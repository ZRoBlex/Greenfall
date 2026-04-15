// ============================================================
//  InventorySystem.cs
//  Greenfall: The Last Harvest
//  Carpeta sugerida: Assets/Greenfall/Inventory/
// ============================================================
//
//  QUÉ HACE:
//  El inventario central del jugador. Gestiona:
//    - Hotbar (slots visibles — los que el jugador equipa activamente)
//    - Bolsa (slots del inventario completo)
//    - Apilamiento automático
//    - Drop de items
//    - Eventos para que la UI reaccione
//
//  ARQUITECTURA:
//  Este sistema es la FUENTE DE VERDAD para lo que el jugador tiene.
//  Los demás sistemas (WeaponInventoryBridge, ToolController, etc.)
//  escuchan sus eventos y reaccionan en consecuencia.
//
//  SINGLETON:
//  Se accede desde cualquier script con InventorySystem.Instance.
//
//  CÓMO AGREGAR UN ITEM DESDE CÓDIGO:
//    bool added = InventorySystem.Instance.TryAddItem(itemData, cantidad);
//
//  CÓMO SABER QUÉ ITEM HAY EN UN SLOT:
//    InventorySlot slot = InventorySystem.Instance.GetSlot(globalIndex);
//
//  ÍNDICES DE SLOTS:
//  Los primeros hotbarSize slots son la hotbar (índices 0 a hotbarSize-1).
//  Los siguientes son la bolsa (índices hotbarSize en adelante).
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

// ── Clase de slot runtime ───────────────────────────────────────────────────

/// <summary>
/// Representa el estado actual de UN slot del inventario.
/// Contiene la definición del item (qué es) y la cantidad (cuántos hay).
/// Para armas, también guarda la referencia al componente Weapon en escena.
/// </summary>
[Serializable]
public class InventorySlot
{
    // ── Datos del item ──────────────────────────────────────────────────

    /// <summary>
    /// Definición del item. Null si el slot está vacío.
    /// </summary>
    public InventoryItemData itemData;

    /// <summary>
    /// Cantidad de unidades en este slot.
    /// Para items no-stackeable siempre es 0 o 1.
    /// </summary>
    public int amount;

    // ── Datos de instancia (no serializados — solo en runtime) ──────────

    /// <summary>
    /// Para armas: referencia al componente Weapon del arma que está
    /// en escena (parented bajo el WeaponHolder del jugador).
    /// Para otros items: null.
    /// NO se serializa porque es una referencia a un objeto de escena.
    /// </summary>
    [NonSerialized] public Weapon weaponInstance;

    /// <summary>
    /// Para herramientas: referencia al MonoBehaviour de la herramienta.
    /// Se usa para activarlo/desactivarlo al cambiar de slot.
    /// </summary>
    [NonSerialized] public MonoBehaviour toolInstance;

    // ── Propiedades de conveniencia ─────────────────────────────────────

    /// <summary>True si no hay ningún item en este slot.</summary>
    public bool IsEmpty => itemData == null || amount <= 0;

    /// <summary>True si el slot está lleno (no puede recibir más del mismo item).</summary>
    public bool IsFull => !IsEmpty && itemData.isStackable
        ? amount >= itemData.maxStack
        : !IsEmpty;

    /// <summary>
    /// Vacía el slot completamente.
    /// No destruye el worldPrefab — eso lo maneja InventorySystem.
    /// </summary>
    public void Clear()
    {
        itemData       = null;
        amount         = 0;
        weaponInstance = null;
        toolInstance   = null;
    }

    /// <summary>
    /// Cuántas unidades más de este item caben en el slot.
    /// Retorna 0 si está lleno o es un item no-stackeable.
    /// </summary>
    public int FreeSpace()
    {
        if (IsEmpty) return int.MaxValue;    // Slot vacío → acepta cualquier cantidad (hasta maxStack)
        if (!itemData.isStackable) return 0; // No stackeable → no cabe más
        return itemData.maxStack - amount;
    }
}

// ── Sistema principal ───────────────────────────────────────────────────────

public class InventorySystem : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    //  SINGLETON
    // ─────────────────────────────────────────────────────────────────────

    public static InventorySystem Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────
    //  CONFIGURACIÓN (editable en Inspector)
    // ─────────────────────────────────────────────────────────────────────

    [Header("Tamaño del Inventario")]
    [Tooltip("Número de slots de HOTBAR (los que el jugador equipa directamente). " +
             "Estos son los que aparecen en la barra inferior de la pantalla. " +
             "Rango recomendado: 4-8.")]
    [Range(1, 10)] [SerializeField] private int _hotbarSize = 5;

    [Tooltip("Número de slots de BOLSA (inventario completo). " +
             "Estos aparecen cuando el jugador abre el inventario con Tab. " +
             "Rango recomendado: 16-40.")]
    [Range(4, 60)] [SerializeField] private int _bagSize = 20;

    [Header("Drop")]
    [Tooltip("Fuerza con que el item sale disparado al tirar.")]
    [SerializeField] private float _dropForce = 5f;

    [Tooltip("Offset desde el jugador donde aparece el item al tirarlo. " +
             "Normalmente al frente y un poco arriba.")]
    [SerializeField] private Vector3 _dropOffset = new Vector3(0f, 0.5f, 1.2f);

    [Header("Audio Global")]
    [Tooltip("AudioSource para sonidos de inventario que no vienen del item. " +
             "Por ejemplo, el sonido de 'inventario lleno'.")]
    [SerializeField] private AudioSource _audioSource;

    [Tooltip("Sonido cuando el inventario está lleno y no puede recibir el item.")]
    [SerializeField] private AudioClip _inventoryFullSound;

    // ─────────────────────────────────────────────────────────────────────
    //  ESTADO INTERNO
    // ─────────────────────────────────────────────────────────────────────

    // Array único de todos los slots: [hotbar0, hotbar1, ..., bag0, bag1, ...]
    // Índices 0 a (_hotbarSize-1) = hotbar
    // Índices _hotbarSize en adelante = bolsa
    private InventorySlot[] _slots;

    // Índice global del slot activo en la hotbar
    // -1 significa que ningún slot está activo
    private int _activeHotbarSlot = 0;

    // ─────────────────────────────────────────────────────────────────────
    //  PROPIEDADES PÚBLICAS
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Total de slots (hotbar + bolsa).</summary>
    public int TotalSlots => _hotbarSize + _bagSize;

    /// <summary>Número de slots de hotbar.</summary>
    public int HotbarSize => _hotbarSize;

    /// <summary>Número de slots de bolsa.</summary>
    public int BagSize => _bagSize;

    /// <summary>Índice global del slot activo (-1 si ninguno).</summary>
    public int ActiveSlotIndex => _activeHotbarSlot;

    /// <summary>El slot activo actualmente (puede ser null/empty).</summary>
    public InventorySlot ActiveSlot =>
        (_activeHotbarSlot >= 0 && _activeHotbarSlot < _hotbarSize)
        ? _slots[_activeHotbarSlot]
        : null;

    // ─────────────────────────────────────────────────────────────────────
    //  EVENTOS
    //  La UI escucha estos eventos para saber cuándo actualizarse.
    //  Usar eventos es más eficiente que hacer polling cada frame.
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Se dispara cuando el contenido de UN slot específico cambia.
    /// Parámetro: índice global del slot que cambió.
    /// La UI del slot correspondiente debería refrescarse.
    /// </summary>
    public event Action<int> OnSlotChanged;

    /// <summary>
    /// Se dispara cuando cambia el slot activo de la hotbar.
    /// Parámetro: nuevo índice global activo (-1 si ninguno).
    /// </summary>
    public event Action<int> OnActiveSlotChanged;

    /// <summary>
    /// Se dispara cuando se agrega un item exitosamente.
    /// Parámetros: (itemData, cantidad, índice del slot donde quedó).
    /// </summary>
    public event Action<InventoryItemData, int, int> OnItemAdded;

    /// <summary>
    /// Se dispara cuando se quita un item.
    /// Parámetros: (itemData, cantidad removida, índice del slot).
    /// </summary>
    public event Action<InventoryItemData, int, int> OnItemRemoved;

    /// <summary>
    /// Se dispara cuando el inventario no puede recibir el item
    /// (estaba lleno o no hay espacio de stacking).
    /// </summary>
    public event Action<InventoryItemData> OnInventoryFull;

    // ─────────────────────────────────────────────────────────────────────
    //  AWAKE
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Patrón Singleton con protección contra duplicados
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[InventorySystem] Ya existe una instancia. Destruyendo duplicado.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Inicializar el array de slots
        _slots = new InventorySlot[TotalSlots];
        for (int i = 0; i < TotalSlots; i++)
            _slots[i] = new InventorySlot();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  API PÚBLICA — AGREGAR ITEMS
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Intenta agregar un item al inventario.
    /// Si es stackeable, apila automáticamente con slots existentes.
    /// Si no es stackeable, busca el primer slot vacío.
    ///
    /// PRIORIDAD:
    ///  - Weapons → primero hotbar, luego bolsa
    ///  - Ammo, Seeds, Materials → primero bolsa, luego hotbar
    ///  - Consumables → primero bolsa
    ///
    /// Returns: true si se agregó al menos una unidad.
    /// </summary>
    public bool TryAddItem(InventoryItemData itemData, int amount = 1,
                           Weapon weaponInstance = null)
    {
        if (itemData == null || amount <= 0) return false;

        int remaining = amount;

        // Paso 1: intentar apilar en slots existentes del mismo item
        if (itemData.isStackable)
            remaining = TryStackInExisting(itemData, remaining);

        // Si ya quedó todo apilado, terminamos
        if (remaining <= 0) return true;

        // Paso 2: buscar slots vacíos según la prioridad del tipo de item
        bool found = false;

        if (itemData.type == ItemType.Weapon)
        {
            // Armas van primero a hotbar, luego bolsa
            found = TryPlaceInRange(itemData, remaining, 0, _hotbarSize, weaponInstance)
                 || TryPlaceInRange(itemData, remaining, _hotbarSize, TotalSlots, weaponInstance);
        }
        else
        {
            // El resto va primero a bolsa, luego hotbar (hotbar es para cosas que se usan activamente)
            found = TryPlaceInRange(itemData, remaining, _hotbarSize, TotalSlots, weaponInstance)
                 || TryPlaceInRange(itemData, remaining, 0, _hotbarSize, weaponInstance);
        }

        if (!found)
        {
            PlayInventoryFullSound();
            OnInventoryFull?.Invoke(itemData);
            return false;
        }

        return true;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  API PÚBLICA — REMOVER ITEMS
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Remueve una cantidad de un item específico del inventario.
    /// Busca en todos los slots y remueve comenzando por el primer slot que lo tenga.
    ///
    /// Returns: cuántas unidades se removieron realmente.
    /// </summary>
    public int RemoveItem(InventoryItemData itemData, int amount = 1)
    {
        if (itemData == null || amount <= 0) return 0;

        int remaining = amount;
        for (int i = 0; i < TotalSlots && remaining > 0; i++)
        {
            var slot = _slots[i];
            if (slot.IsEmpty || slot.itemData != itemData) continue;

            int toRemove = Mathf.Min(slot.amount, remaining);
            slot.amount -= toRemove;
            remaining   -= toRemove;

            if (slot.amount <= 0) slot.Clear();

            OnSlotChanged?.Invoke(i);
            OnItemRemoved?.Invoke(itemData, toRemove, i);
        }

        return amount - remaining; // cuánto se removió
    }

    /// <summary>
    /// Remueve el item en un slot específico por su índice global.
    /// Si amount es mayor al contenido, remueve todo lo que hay.
    /// </summary>
    public bool RemoveFromSlot(int globalIndex, int amount = 1)
    {
        if (!IsValidIndex(globalIndex)) return false;
        var slot = _slots[globalIndex];
        if (slot.IsEmpty) return false;

        var itemData = slot.itemData;
        int toRemove = Mathf.Min(slot.amount, amount);
        slot.amount -= toRemove;
        if (slot.amount <= 0) slot.Clear();

        OnSlotChanged?.Invoke(globalIndex);
        OnItemRemoved?.Invoke(itemData, toRemove, globalIndex);
        return true;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  API PÚBLICA — EQUIP / SELECCIÓN DE HOTBAR
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Establece qué slot de hotbar está activo.
    /// Dispara OnActiveSlotChanged para que los sistemas equipen el item.
    ///
    /// hotbarLocalIndex: 0-based index dentro de la hotbar (0 = primer slot).
    /// </summary>
    public void SetActiveHotbarSlot(int hotbarLocalIndex)
    {
        // Convertir índice local de hotbar a índice global
        int globalIndex = Mathf.Clamp(hotbarLocalIndex, 0, _hotbarSize - 1);

        if (_activeHotbarSlot == globalIndex) return; // Ya está activo

        _activeHotbarSlot = globalIndex;
        OnActiveSlotChanged?.Invoke(_activeHotbarSlot);
    }

    /// <summary>
    /// Avanza al siguiente slot de hotbar (con wrap).
    /// Útil para scroll wheel o tecla de cambio.
    /// </summary>
    public void SelectNextHotbarSlot()
    {
        SetActiveHotbarSlot((_activeHotbarSlot + 1) % _hotbarSize);
    }

    /// <summary>
    /// Retrocede al slot de hotbar anterior (con wrap).
    /// </summary>
    public void SelectPreviousHotbarSlot()
    {
        SetActiveHotbarSlot((_activeHotbarSlot - 1 + _hotbarSize) % _hotbarSize);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  API PÚBLICA — MOVER ITEMS (drag & drop)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Mueve o intercambia el contenido de dos slots.
    /// Si son el mismo tipo de item y stackeable, intenta apilar.
    /// De lo contrario, intercambia los contenidos.
    /// </summary>
    public void MoveSlot(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return;
        if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex)) return;

        var from = _slots[fromIndex];
        var to   = _slots[toIndex];

        if (from.IsEmpty) return;

        // Si ambos tienen el mismo item stackeable, intentar apilar
        if (!to.IsEmpty && to.itemData == from.itemData && from.itemData.isStackable)
        {
            int space = to.FreeSpace();
            if (space > 0)
            {
                int transfer = Mathf.Min(from.amount, space);
                to.amount   += transfer;
                from.amount -= transfer;
                if (from.amount <= 0) from.Clear();

                OnSlotChanged?.Invoke(fromIndex);
                OnSlotChanged?.Invoke(toIndex);
                return;
            }
        }

        // Intercambiar slots completamente
        (_slots[fromIndex], _slots[toIndex]) = (_slots[toIndex], _slots[fromIndex]);
        OnSlotChanged?.Invoke(fromIndex);
        OnSlotChanged?.Invoke(toIndex);

        // Si uno de los slots era el activo, notificar el cambio
        if (fromIndex == _activeHotbarSlot || toIndex == _activeHotbarSlot)
            OnActiveSlotChanged?.Invoke(_activeHotbarSlot);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  API PÚBLICA — TIRAR ITEM AL MUNDO (drop)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tira al mundo el item del slot activo.
    /// Si el item no se puede tirar (QuestItem, arma default), no hace nada.
    /// El worldPrefab del InventoryItemData se instancia en el mundo.
    /// </summary>
    public void DropActiveItem()
    {
        if (_activeHotbarSlot < 0) return;
        DropItemFromSlot(_activeHotbarSlot);
    }

    /// <summary>
    /// Tira al mundo el item de un slot específico.
    /// </summary>
    public void DropItemFromSlot(int globalIndex)
    {
        if (!IsValidIndex(globalIndex)) return;
        var slot = _slots[globalIndex];
        if (slot.IsEmpty || !slot.itemData.canBeDropped) return;

        var itemData = slot.itemData;

        // Instanciar el worldPrefab si existe
        if (itemData.worldPrefab != null)
        {
            Vector3 dropPos = transform.position
                + transform.TransformDirection(_dropOffset);

            var dropped = Instantiate(itemData.worldPrefab, dropPos, transform.rotation);

            // Si tiene Rigidbody, aplicar fuerza de drop
            if (dropped.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.AddForce(transform.forward * _dropForce, ForceMode.Impulse);
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * 2f, ForceMode.Impulse);
            }

            // Si tiene InventoryItemController, sincronizar la cantidad dropeada
            if (dropped.TryGetComponent<InventoryItemController>(out var controller))
                controller.SetDroppedAmount(slot.amount);

            // Si tiene WeaponPickup y es un arma con instancia en escena, reusar la instancia
            if (slot.weaponInstance != null)
            {
                // La instancia del arma que ya estaba en la escena se activa como dropped
                slot.weaponInstance.transform.SetParent(null);
                slot.weaponInstance.transform.position = dropPos;
                // Notificar al WeaponInventoryBridge que debe gestionar esto
                // El bridge escucha OnItemRemoved y maneja el arma física
            }

            // Sonido de drop
            if (itemData.dropSound != null)
                PlaySoundAtPoint(itemData.dropSound, dropPos, itemData.pickupVolume);
        }

        // Limpiar el slot
        int amountDropped = slot.amount;
        slot.Clear();
        OnSlotChanged?.Invoke(globalIndex);
        OnItemRemoved?.Invoke(itemData, amountDropped, globalIndex);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  API PÚBLICA — LECTURA
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene el slot en un índice global. Nunca retorna null.
    /// Si el índice es inválido, retorna un slot vacío dummy.
    /// </summary>
    public InventorySlot GetSlot(int globalIndex)
    {
        if (!IsValidIndex(globalIndex)) return new InventorySlot();
        return _slots[globalIndex];
    }

    /// <summary>
    /// Retorna true si el índice es de la hotbar.
    /// </summary>
    public bool IsHotbarSlot(int globalIndex) =>
        globalIndex >= 0 && globalIndex < _hotbarSize;

    /// <summary>
    /// Retorna true si el índice es de la bolsa.
    /// </summary>
    public bool IsBagSlot(int globalIndex) =>
        globalIndex >= _hotbarSize && globalIndex < TotalSlots;

    /// <summary>
    /// Convierte un índice local de hotbar (0, 1, 2...) a global.
    /// </summary>
    public int HotbarToGlobal(int hotbarLocal) => hotbarLocal;

    /// <summary>
    /// Convierte un índice local de bolsa (0, 1, 2...) a global.
    /// </summary>
    public int BagToGlobal(int bagLocal) => _hotbarSize + bagLocal;

    /// <summary>
    /// Devuelve cuántas unidades del item dado hay en TODO el inventario.
    /// </summary>
    public int GetTotalAmount(InventoryItemData itemData)
    {
        int total = 0;
        foreach (var slot in _slots)
            if (!slot.IsEmpty && slot.itemData == itemData)
                total += slot.amount;
        return total;
    }

    /// <summary>
    /// True si el inventario está completamente lleno (ningún slot puede recibir más items).
    /// </summary>
    public bool IsFull()
    {
        foreach (var slot in _slots)
            if (slot.IsEmpty) return false;
        return true;
    }

    /// <summary>
    /// Fuerza el refresco visual de un slot específico.
    /// Útil cuando algo externo modifica el slot (como WeaponInventoryBridge).
    /// </summary>
    public void ForceRefreshSlot(int globalIndex)
    {
        if (IsValidIndex(globalIndex))
            OnSlotChanged?.Invoke(globalIndex);
    }

    /// <summary>
    /// Establece directamente el weaponInstance de un slot.
    /// Llamado por WeaponInventoryBridge cuando el arma se instancia en escena.
    /// </summary>
    public void SetWeaponInstance(int globalIndex, Weapon weapon)
    {
        if (!IsValidIndex(globalIndex)) return;
        _slots[globalIndex].weaponInstance = weapon;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  LÓGICA INTERNA PRIVADA
    // ─────────────────────────────────────────────────────────────────────

    // Intenta apilar en slots existentes que ya tengan el mismo item.
    // Retorna cuántas unidades NO se pudieron apilar (sobra).
    private int TryStackInExisting(InventoryItemData itemData, int amount)
    {
        int remaining = amount;
        for (int i = 0; i < TotalSlots && remaining > 0; i++)
        {
            var slot = _slots[i];
            if (slot.IsEmpty || slot.itemData != itemData) continue;

            int canFit = slot.FreeSpace();
            if (canFit <= 0) continue;

            int adding = Mathf.Min(remaining, canFit);
            slot.amount += adding;
            remaining   -= adding;

            OnSlotChanged?.Invoke(i);
            OnItemAdded?.Invoke(itemData, adding, i);
        }
        return remaining;
    }

    // Intenta colocar el item en el primer slot vacío del rango dado.
    // Retorna true si se colocó exitosamente.
    private bool TryPlaceInRange(InventoryItemData itemData, int amount,
                                  int from, int to, Weapon weaponInstance)
    {
        for (int i = from; i < to; i++)
        {
            if (!_slots[i].IsEmpty) continue;

            _slots[i].itemData       = itemData;
            _slots[i].amount         = Mathf.Min(amount, itemData.isStackable ? itemData.maxStack : 1);
            _slots[i].weaponInstance = weaponInstance;

            OnSlotChanged?.Invoke(i);
            OnItemAdded?.Invoke(itemData, _slots[i].amount, i);
            return true;
        }
        return false;
    }

    private bool IsValidIndex(int index) => index >= 0 && index < TotalSlots;

    private void PlayInventoryFullSound()
    {
        if (_audioSource != null && _inventoryFullSound != null)
            _audioSource.PlayOneShot(_inventoryFullSound);
    }

    private static void PlaySoundAtPoint(AudioClip clip, Vector3 pos, float volume)
    {
        if (clip == null) return;
        var go  = new GameObject("TempAudio");
        go.transform.position = pos;
        var src  = go.AddComponent<AudioSource>();
        src.clip         = clip;
        src.spatialBlend = 0.7f;
        src.volume       = volume;
        src.Play();
        Destroy(go, clip.length + 0.1f);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  DEBUG (solo en editor)
    // ─────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    [ContextMenu("Debug: Imprimir Inventario")]
    private void DebugPrintInventory()
    {
        Debug.Log("=== INVENTARIO ===");
        for (int i = 0; i < TotalSlots; i++)
        {
            string zone  = IsHotbarSlot(i) ? $"HOTBAR[{i}]" : $"BOLSA [{i - _hotbarSize}]";
            string mark  = i == _activeHotbarSlot ? " ◄ ACTIVO" : "";
            string item  = _slots[i].IsEmpty
                ? "(vacío)"
                : $"{_slots[i].itemData.displayName} x{_slots[i].amount}";
            Debug.Log($"  {zone}: {item}{mark}");
        }
    }
#endif
}