// ============================================================
//  GF_Inventory.cs
//  Greenfall — Sistema de Inventario Universal
//  Carpeta: Assets/Greenfall/Inventory/
// ============================================================
//  Singleton que gestiona TODOS los items del jugador.
//  Reemplaza WeaponInventory + AmmoInventory + UnifiedInventory.
//
//  ACCESO DESDE CÓDIGO:
//    GF_Inventory.Instance.TryAdd(definition, amount)
//    GF_Inventory.Instance.GetSlot(index)
//    GF_Inventory.Instance.SetActive(slotIndex)
//
//  EVENTOS que otros sistemas escuchan:
//    OnSlotChanged(index)
//    OnActiveSlotChanged(newIndex, oldIndex)
//    OnItemAdded(definition, amount, slotIndex)
//    OnItemRemoved(definition, amount, slotIndex)
//    OnInventoryFull(definition)
//    OnGameStateChanged(GF_GameState)
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

// ── Estado del juego ───────────────────────────────────────────────────────
public enum GF_GameState
{
    Playing,        // Normal — armas activas
    InventoryOpen,  // Inventario abierto — armas desactivadas
    Paused          // Pausado — todo desactivado
}

// ── Clase de Slot ──────────────────────────────────────────────────────────
[Serializable]
public class GF_InventorySlot
{
    public GF_ItemDefinition definition;  // Qué item es (null = vacío)
    public int               amount;      // Cuántos hay

    // Para armas: referencia al Weapon en escena (gestionada por GF_WeaponAdapter)
    [NonSerialized] public Weapon weaponInstance;

    // Propiedades
    public bool IsEmpty  => definition == null || amount <= 0;
    public bool IsFull   => !IsEmpty && (!definition.isStackable || amount >= definition.maxStack);
    public int  FreeSpace =>
        IsEmpty         ? int.MaxValue :
        !definition.isStackable ? 0 :
        definition.maxStack - amount;

    public void Clear()
    {
        definition   = null;
        amount       = 0;
        weaponInstance = null;
    }
}

// ── Sistema de Inventario ──────────────────────────────────────────────────
public class GF_Inventory : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    //  SINGLETON
    // ─────────────────────────────────────────────────────────────────────

    public static GF_Inventory Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────
    //  CONFIG (asignar en Inspector)
    // ─────────────────────────────────────────────────────────────────────

    [Header("Configuración")]
    [Tooltip("ScriptableObject que controla layout, colores y teclas.")]
    public GF_InventoryConfig config;

    [Header("Slots")]
    [Tooltip("Hotbar = slots 0 a (hotbarSize-1). Bolsa = el resto.")]
    [Range(1,10)]  public int hotbarSize = 5;
    [Range(4,60)]  public int bagSize    = 20;

    [Header("Drop")]
    public float   dropForce  = 5f;
    public Vector3 dropOffset = new Vector3(0f, 0.5f, 1.2f);

    [Header("Audio Global")]
    public AudioSource globalAudio;
    public AudioClip   inventoryFullSound;

    // ─────────────────────────────────────────────────────────────────────
    //  ESTADO
    // ─────────────────────────────────────────────────────────────────────

    private GF_InventorySlot[] _slots;
    private int  _activeIndex  = 0;     // Siempre válido (0-based, hotbar only)
    private GF_GameState _gameState = GF_GameState.Playing;

    // ─────────────────────────────────────────────────────────────────────
    //  PROPIEDADES
    // ─────────────────────────────────────────────────────────────────────

    public int          TotalSlots    => hotbarSize + bagSize;
    public int          ActiveIndex   => _activeIndex;
    public GF_InventorySlot ActiveSlot => _slots[_activeIndex];
    public GF_GameState GameState     => _gameState;

    // ─────────────────────────────────────────────────────────────────────
    //  EVENTOS
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>El contenido de un slot cambió. Parámetro: índice global.</summary>
    public event Action<int>                         OnSlotChanged;

    /// <summary>El slot activo cambió. Parámetros: nuevo índice, índice anterior.</summary>
    public event Action<int, int>                    OnActiveSlotChanged;

    /// <summary>Item agregado. Parámetros: definition, cantidad, índice del slot.</summary>
    public event Action<GF_ItemDefinition, int, int> OnItemAdded;

    /// <summary>Item removido. Parámetros: definition, cantidad, índice del slot.</summary>
    public event Action<GF_ItemDefinition, int, int> OnItemRemoved;

    /// <summary>No había espacio para el item.</summary>
    public event Action<GF_ItemDefinition>           OnInventoryFull;

    /// <summary>El estado del juego cambió (jugar / inventario / pausa).</summary>
    public event Action<GF_GameState>                OnGameStateChanged;

    // ─────────────────────────────────────────────────────────────────────
    //  AWAKE
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _slots = new GF_InventorySlot[TotalSlots];
        for (int i = 0; i < TotalSlots; i++)
            _slots[i] = new GF_InventorySlot();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  API — AGREGAR ITEMS
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Intenta agregar un item al inventario. Apila automáticamente si es stackeable.
    /// weaponInstance: para armas, pasa la instancia del componente Weapon.
    /// Retorna true si al menos una unidad fue agregada.
    /// </summary>
    public bool TryAdd(GF_ItemDefinition def, int amount = 1, Weapon weaponInstance = null)
    {
        if (def == null || amount <= 0) return false;

        int remaining = amount;

        // Paso 1: intentar apilar en slots existentes
        if (def.isStackable)
            remaining = StackInExisting(def, remaining);

        if (remaining <= 0) return true;

        // Paso 2: buscar slot vacío según tipo
        // Armas → hotbar primero; todo lo demás → bolsa primero
        bool placed = def.type == GF_ItemType.Weapon
            ? TryPlaceInRange(def, remaining, 0, hotbarSize, weaponInstance)
           || TryPlaceInRange(def, remaining, hotbarSize, TotalSlots, weaponInstance)
            : TryPlaceInRange(def, remaining, hotbarSize, TotalSlots, weaponInstance)
           || TryPlaceInRange(def, remaining, 0, hotbarSize, weaponInstance);

        if (!placed)
        {
            PlaySound(inventoryFullSound);
            OnInventoryFull?.Invoke(def);
            return false;
        }

        return true;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  API — REMOVER ITEMS
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Remueve items de este tipo del inventario (de todos los slots que lo contengan).
    /// Retorna cuántas unidades se removieron realmente.
    /// </summary>
    public int RemoveItems(GF_ItemDefinition def, int amount = 1)
    {
        if (def == null || amount <= 0) return 0;

        int remaining = amount;
        for (int i = 0; i < TotalSlots && remaining > 0; i++)
        {
            var slot = _slots[i];
            if (slot.IsEmpty || slot.definition != def) continue;

            int toRemove = Mathf.Min(slot.amount, remaining);
            slot.amount -= toRemove;
            remaining   -= toRemove;

            var removedDef = slot.definition;
            if (slot.amount <= 0) slot.Clear();

            OnSlotChanged?.Invoke(i);
            OnItemRemoved?.Invoke(removedDef, toRemove, i);
        }

        return amount - remaining;
    }

    /// <summary>Remueve el contenido completo de un slot por índice.</summary>
    public void ClearSlot(int globalIndex)
    {
        if (!IsValid(globalIndex) || _slots[globalIndex].IsEmpty) return;

        var def    = _slots[globalIndex].definition;
        var amount = _slots[globalIndex].amount;
        _slots[globalIndex].Clear();

        OnSlotChanged?.Invoke(globalIndex);
        OnItemRemoved?.Invoke(def, amount, globalIndex);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  API — MOVER (drag & drop)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Mueve o intercambia dos slots.
    /// Si tienen el mismo item stackeable, apila.
    /// </summary>
    public void MoveSlot(int from, int to)
    {
        if (from == to || !IsValid(from) || !IsValid(to)) return;

        var f = _slots[from];
        var t = _slots[to];

        if (f.IsEmpty) return;

        // Apilar mismo tipo
        if (!t.IsEmpty && t.definition == f.definition && f.definition.isStackable)
        {
            int space = t.FreeSpace;
            if (space > 0)
            {
                int transfer = Mathf.Min(f.amount, space);
                t.amount += transfer;
                f.amount -= transfer;
                if (f.amount <= 0) f.Clear();

                OnSlotChanged?.Invoke(from);
                OnSlotChanged?.Invoke(to);
                return;
            }
        }

        // Intercambiar
        (_slots[from], _slots[to]) = (_slots[to], _slots[from]);
        OnSlotChanged?.Invoke(from);
        OnSlotChanged?.Invoke(to);

        // Si uno de los slots era el activo, notificar
        if (from == _activeIndex || to == _activeIndex)
            OnActiveSlotChanged?.Invoke(_activeIndex, _activeIndex);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  API — SELECCIÓN DE HOTBAR
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Selecciona un slot de hotbar por índice local (0-based).</summary>
    public void SetActive(int hotbarLocalIndex)
    {
        int clamped = Mathf.Clamp(hotbarLocalIndex, 0, hotbarSize - 1);
        if (clamped == _activeIndex) return;

        int prev = _activeIndex;
        _activeIndex = clamped;
        OnActiveSlotChanged?.Invoke(_activeIndex, prev);
    }

    public void SelectNext() => SetActive((_activeIndex + 1) % hotbarSize);
    public void SelectPrev() => SetActive((_activeIndex - 1 + hotbarSize) % hotbarSize);

    // ─────────────────────────────────────────────────────────────────────
    //  API — DROP
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Tira al mundo el item del slot activo.</summary>
    public void DropActive()
    {
        DropFromSlot(_activeIndex);
    }

    /// <summary>Tira al mundo el item de un slot específico.</summary>
    public void DropFromSlot(int globalIndex)
    {
        if (!IsValid(globalIndex)) return;
        var slot = _slots[globalIndex];
        if (slot.IsEmpty || !slot.definition.canDrop) return;

        var def = slot.definition;
        int amt = slot.amount;

        // Instanciar worldPrefab si existe y no es una instancia de arma
        if (def.worldPrefab != null && slot.weaponInstance == null)
        {
            Vector3 pos = transform.position + transform.TransformDirection(dropOffset);
            var go = Instantiate(def.worldPrefab, pos, transform.rotation);

            if (go.TryGetComponent<Rigidbody>(out var rb))
                rb.AddForce(transform.forward * dropForce, ForceMode.Impulse);

            if (go.TryGetComponent<GF_WorldPickup>(out var pickup))
                pickup.SetAmount(amt);

            PlaySoundAt(def.dropSound, pos, def.pickupVolume);
        }

        ClearSlot(globalIndex);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  API — ESTADO DEL JUEGO
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Cambia el estado del juego.
    /// GF_WeaponAdapter escucha este evento para activar/desactivar armas.
    /// </summary>
    public void SetGameState(GF_GameState state)
    {
        if (_gameState == state) return;
        _gameState = state;
        OnGameStateChanged?.Invoke(state);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  API — LECTURA
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Lee un slot por índice global. Nunca retorna null.</summary>
    public GF_InventorySlot GetSlot(int globalIndex)
    {
        if (!IsValid(globalIndex)) return new GF_InventorySlot();
        return _slots[globalIndex];
    }

    public bool IsHotbarSlot(int i) => i >= 0 && i < hotbarSize;
    public bool IsBagSlot(int i)    => i >= hotbarSize && i < TotalSlots;
    public int  GlobalIndex(int hotbarLocal) => hotbarLocal;
    public int  BagGlobalIndex(int bagLocal) => hotbarSize + bagLocal;

    /// <summary>Total de un tipo de item en todo el inventario.</summary>
    public int TotalOf(GF_ItemDefinition def)
    {
        int total = 0;
        foreach (var s in _slots)
            if (!s.IsEmpty && s.definition == def)
                total += s.amount;
        return total;
    }

    /// <summary>Registra la instancia de Weapon en el slot correcto.</summary>
    public void SetWeaponInstance(int globalIndex, Weapon w)
    {
        if (IsValid(globalIndex)) _slots[globalIndex].weaponInstance = w;
    }

    /// <summary>Fuerza el refresco visual de un slot.</summary>
    public void ForceRefresh(int globalIndex)
    {
        if (IsValid(globalIndex)) OnSlotChanged?.Invoke(globalIndex);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  PRIVADOS
    // ─────────────────────────────────────────────────────────────────────

    private int StackInExisting(GF_ItemDefinition def, int amount)
    {
        int rem = amount;
        for (int i = 0; i < TotalSlots && rem > 0; i++)
        {
            var s = _slots[i];
            if (s.IsEmpty || s.definition != def || s.IsFull) continue;

            int add = Mathf.Min(rem, s.FreeSpace);
            s.amount += add;
            rem      -= add;

            OnSlotChanged?.Invoke(i);
            OnItemAdded?.Invoke(def, add, i);
        }
        return rem;
    }

    private bool TryPlaceInRange(GF_ItemDefinition def, int amount,
                                  int from, int to, Weapon weaponInstance)
    {
        for (int i = from; i < to; i++)
        {
            if (!_slots[i].IsEmpty) continue;

            int placing = Mathf.Min(amount, def.isStackable ? def.maxStack : 1);
            _slots[i].definition   = def;
            _slots[i].amount       = placing;
            _slots[i].weaponInstance = weaponInstance;

            OnSlotChanged?.Invoke(i);
            OnItemAdded?.Invoke(def, placing, i);
            return true;
        }
        return false;
    }

    private bool IsValid(int i) => i >= 0 && i < TotalSlots;

    private void PlaySound(AudioClip clip)
    {
        if (globalAudio != null && clip != null)
            globalAudio.PlayOneShot(clip);
    }

    private static void PlaySoundAt(AudioClip clip, Vector3 pos, float vol)
    {
        if (clip == null) return;
        var go  = new GameObject("GF_DropSound");
        go.transform.position = pos;
        var src = go.AddComponent<AudioSource>();
        src.clip = clip; src.volume = vol; src.spatialBlend = 0.5f;
        src.Play();
        Destroy(go, clip.length + 0.1f);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  DEBUG
    // ─────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    [ContextMenu("Debug: Imprimir Inventario")]
    private void DebugPrint()
    {
        Debug.Log("=== GF_Inventory ===");
        for (int i = 0; i < TotalSlots; i++)
        {
            string zone = IsHotbarSlot(i) ? $"HOTBAR[{i}]" : $"BOLSA [{i-hotbarSize}]";
            string mark = i == _activeIndex ? " ◄ ACTIVO" : "";
            string item = _slots[i].IsEmpty
                ? "(vacío)"
                : $"{_slots[i].definition.displayName} x{_slots[i].amount}";
            Debug.Log($"  {zone}: {item}{mark}");
        }
    }
#endif
}