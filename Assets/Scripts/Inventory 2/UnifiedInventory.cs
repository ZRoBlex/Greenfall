// ============================================================
// UnifiedInventory.cs
// Carpeta: Scripts/Inventory/
// ------------------------------------------------------------
// RESPONSABILIDAD: solo maneja los DATOS.
// Responde preguntas como:
//   "¿Qué hay en el slot 3?"
//   "¿Puedo agregar esta semilla?"
//   "Mueve el item del slot 2 al slot 7"
//
// NO sabe nada de:
//   - UI (no toca Image, Canvas ni RectTransform)
//   - Input (no llama Input.GetKey)
//   - Weapon behavior (no llama Equip ni Reload)
//
// Comunica sus cambios mediante EVENTOS.
// WeaponInventory y la UI se suscriben a esos eventos.
//
// LAYOUT DE SLOTS:
//   [0 .. hotbarSize-1]         → Hotbar (siempre visible)
//   [hotbarSize .. total-1]     → Bolsa / Inventario general
//
// USO DESDE OTROS SISTEMAS:
//   // Agregar un arma recogida:
//   UnifiedInventory.Instance.TryAdd(InventoryEntry.FromWeapon(weapon));
//
//   // Agregar semillas:
//   UnifiedInventory.Instance.TryAdd(InventoryEntry.FromSeed(seed, 3));
// ============================================================

using System;
using UnityEngine;

namespace Greenfall.Inventory
{
    public class UnifiedInventory : MonoBehaviour
    {
        public static UnifiedInventory Instance { get; private set; }

        // ─────────────────────────────────────────────────────────
        // CONFIGURACIÓN (Inspector)
        // ─────────────────────────────────────────────────────────

        [Header("Tamaños")]
        [Tooltip("Slots de hotbar. Las armas van aquí primero.")]
        [SerializeField] private int _hotbarSize    = 5;

        [Tooltip("Slots del inventario general (bolsa). Semillas, materiales, etc.")]
        [SerializeField] private int _bagSize       = 20;

        [Header("Bridge")]
        [Tooltip("El WeaponInventory que ya tienes. Lo usamos para Equip/Drop.")]
        [SerializeField] private WeaponInventory _weaponInventory;

        // ─────────────────────────────────────────────────────────
        // DATOS
        // ─────────────────────────────────────────────────────────

        private InventoryEntry[] _slots;
        public int TotalSlots  => _hotbarSize + _bagSize;
        public int HotbarSize  => _hotbarSize;
        public int BagSize     => _bagSize;

        // Slot activo de la hotbar (el arma equipada)
        private int _activeHotbarSlot = -1;
        public  int ActiveHotbarSlot  => _activeHotbarSlot;

        // ─────────────────────────────────────────────────────────
        // EVENTOS
        // La UI y WeaponInventory se suscriben aquí.
        // ─────────────────────────────────────────────────────────

        /// <summary>Cambió el contenido de un slot. param: índice global.</summary>
        public event Action<int> OnSlotChanged;

        /// <summary>Cambió el slot activo de la hotbar. param: nuevo índice global.</summary>
        public event Action<int> OnActiveSlotChanged;

        // ─────────────────────────────────────────────────────────
        // HELPERS DE ÍNDICE
        // ─────────────────────────────────────────────────────────

        /// <summary>Índice local hotbar → índice global</summary>
        public int HotbarIndex(int local) => local;

        /// <summary>Índice local bolsa → índice global</summary>
        public int BagIndex(int local) => _hotbarSize + local;

        /// <summary>¿Este índice global es un slot de hotbar?</summary>
        public bool IsHotbar(int global) => global >= 0 && global < _hotbarSize;

        /// <summary>¿Este índice global es un slot de bolsa?</summary>
        public bool IsBag(int global) => global >= _hotbarSize && global < TotalSlots;

        // ─────────────────────────────────────────────────────────
        // INICIALIZACIÓN
        // ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _slots = new InventoryEntry[TotalSlots];
            for (int i = 0; i < TotalSlots; i++)
                _slots[i] = new InventoryEntry(); // slot vacío
        }

        // ─────────────────────────────────────────────────────────
        // LECTURA
        // ─────────────────────────────────────────────────────────

        /// <summary>Retorna la entrada en el slot global. Nunca null (puede ser IsEmpty).</summary>
        public InventoryEntry GetSlot(int globalIndex)
        {
            if (globalIndex < 0 || globalIndex >= TotalSlots)
                return new InventoryEntry(); // entrada vacía de seguridad
            return _slots[globalIndex];
        }

        // ─────────────────────────────────────────────────────────
        // AGREGAR ITEM
        // Llamado por sistemas externos (pickup, drops del mundo).
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Intenta agregar un item.
        /// Prioridad: hotbar si es arma y hay espacio → luego bolsa.
        /// Semillas apilan si ya existe el mismo seed.
        /// </summary>
        /// <returns>true si se añadió completamente.</returns>
        public bool TryAdd(InventoryEntry entry)
        {
            if (entry == null || entry.IsEmpty) return false;

            // ── Intentar apilar en slot existente (seeds, materiales) ──
            if (entry.IsStackable)
            {
                int remaining = TryStack(entry);
                if (remaining <= 0) return true;
                entry.amount = remaining; // seguir con lo que sobra
            }

            // ── Buscar slot vacío ──
            // Las armas van a hotbar primero; el resto va a bolsa primero.
            if (entry.kind == ItemKind.Weapon)
            {
                // Intentar hotbar primero, luego bolsa
                if (TryPlaceInRange(entry, 0, _hotbarSize))            return true;
                if (TryPlaceInRange(entry, _hotbarSize, TotalSlots))   return true;
            }
            else
            {
                // Semillas/materiales: bolsa primero, hotbar solo si bolsa llena
                if (TryPlaceInRange(entry, _hotbarSize, TotalSlots))   return true;
                if (TryPlaceInRange(entry, 0, _hotbarSize))            return true;
            }

            return false; // Sin espacio
        }

        // Apila en slots que ya tienen el mismo item. Retorna la cantidad que sobró.
        private int TryStack(InventoryEntry entry)
        {
            int remaining = entry.amount;
            for (int i = 0; i < TotalSlots && remaining > 0; i++)
            {
                var slot = _slots[i];
                if (slot.IsEmpty || slot.id != entry.id) continue;
                if (slot.IsFull) continue;

                int space   = slot.maxStack - slot.amount;
                int add     = Mathf.Min(remaining, space);
                slot.amount += add;
                remaining   -= add;
                OnSlotChanged?.Invoke(i);
            }
            return remaining;
        }

        // Pone el entry en el primer slot vacío dentro de [from, to).
        private bool TryPlaceInRange(InventoryEntry entry, int from, int to)
        {
            for (int i = from; i < to; i++)
            {
                if (!_slots[i].IsEmpty) continue;

                _slots[i] = entry;
                OnSlotChanged?.Invoke(i);

                // Si es arma y cayó en hotbar → notificar al primer equip
                if (entry.kind == ItemKind.Weapon && IsHotbar(i))
                    HandleWeaponAddedToHotbar(entry, i);

                return true;
            }
            return false;
        }

        // ─────────────────────────────────────────────────────────
        // MOVER (drag & drop)
        // La UI llama esto cuando el jugador suelta un item.
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Mueve o intercambia dos slots.
        /// Puede apilarse si son el mismo item stackable.
        /// </summary>
        public void Move(int fromGlobal, int toGlobal)
        {
            if (fromGlobal == toGlobal) return;
            if (fromGlobal < 0 || fromGlobal >= TotalSlots) return;
            if (toGlobal   < 0 || toGlobal   >= TotalSlots) return;

            var from = _slots[fromGlobal];
            var to   = _slots[toGlobal];

            if (from.IsEmpty) return;

            // Mismo item stackable → intentar apilar
            if (!to.IsEmpty && to.id == from.id && from.IsStackable)
            {
                int space = from.maxStack - to.amount;
                if (space > 0)
                {
                    int transfer  = Mathf.Min(from.amount, space);
                    to.amount    += transfer;
                    from.amount  -= transfer;
                    if (from.amount <= 0) from.Clear();
                    OnSlotChanged?.Invoke(fromGlobal);
                    OnSlotChanged?.Invoke(toGlobal);
                    return;
                }
            }

            // Notificar al WeaponInventory ANTES de mover
            NotifyWeaponMove(fromGlobal, toGlobal);

            // Intercambiar
            _slots[fromGlobal] = to;
            _slots[toGlobal]   = from;
            OnSlotChanged?.Invoke(fromGlobal);
            OnSlotChanged?.Invoke(toGlobal);
        }

        // ─────────────────────────────────────────────────────────
        // QUITAR ITEM
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Quita una cantidad del slot. Si llega a 0, limpia el slot.
        /// </summary>
        public bool Remove(int globalIndex, int amount = 1)
        {
            if (globalIndex < 0 || globalIndex >= TotalSlots) return false;
            var slot = _slots[globalIndex];
            if (slot.IsEmpty) return false;

            if (slot.kind == ItemKind.Weapon && IsHotbar(globalIndex))
                HandleWeaponRemovedFromHotbar(globalIndex);

            slot.amount -= amount;
            if (slot.amount <= 0) slot.Clear();
            OnSlotChanged?.Invoke(globalIndex);
            return true;
        }

        // ─────────────────────────────────────────────────────────
        // SELECCIÓN DE HOTBAR (cambio de arma activa)
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Cambia el slot activo de la hotbar.
        /// Le dice al WeaponInventory qué arma equipar.
        /// </summary>
        public void SetActiveHotbar(int hotbarLocal)
        {
            int global = HotbarIndex(hotbarLocal);
            if (global == _activeHotbarSlot) return;
            if (!IsHotbar(global)) return;

            _activeHotbarSlot = global;
            OnActiveSlotChanged?.Invoke(global);

            // Bridge con WeaponInventory
            if (_weaponInventory != null)
                _weaponInventory.Equip(hotbarLocal);
        }

        // ─────────────────────────────────────────────────────────
        // BRIDGE CON WEAPONINVENTORY
        // Coordina sin acoplar: UnifiedInventory no sabe de balas,
        // recoil ni animaciones. Solo dice "equipar índice X".
        // ─────────────────────────────────────────────────────────

        private void HandleWeaponAddedToHotbar(InventoryEntry entry, int globalSlot)
        {
            if (_weaponInventory == null || entry.weaponInstance == null) return;

            // Solo añadir si WeaponInventory aún no lo tiene
            _weaponInventory.AddWeapon(entry.weaponInstance);

            // Si no había ningún arma activa → equipar esta
            if (_activeHotbarSlot < 0)
                SetActiveHotbar(globalSlot);
        }

        private void HandleWeaponRemovedFromHotbar(int globalSlot)
        {
            if (_weaponInventory == null) return;
            // WeaponInventory.DropCurrent quita el arma activa.
            // Solo lo llamamos si este slot era el activo.
            if (globalSlot == _activeHotbarSlot)
                _weaponInventory.DropCurrent();
        }

        private void NotifyWeaponMove(int from, int to)
        {
            if (_weaponInventory == null) return;

            bool fromHotbar = IsHotbar(from);
            bool toHotbar   = IsHotbar(to);
            var  fromEntry  = _slots[from];
            var  toEntry    = _slots[to];

            // Arma sale de hotbar activo → drop
            if (fromHotbar && from == _activeHotbarSlot && fromEntry.kind == ItemKind.Weapon)
                _weaponInventory.DropCurrent();

            // Arma entra a hotbar → add + equip
            if (toHotbar && fromEntry.kind == ItemKind.Weapon && fromEntry.weaponInstance != null)
            {
                _weaponInventory.AddWeapon(fromEntry.weaponInstance);
                _activeHotbarSlot = to;
                OnActiveSlotChanged?.Invoke(to);
            }
        }

        // ─────────────────────────────────────────────────────────
        // DEBUG
        // ─────────────────────────────────────────────────────────
#if UNITY_EDITOR
        [ContextMenu("Print Inventory State")]
        private void DebugPrint()
        {
            for (int i = 0; i < TotalSlots; i++)
            {
                string zone = IsHotbar(i) ? $"HOTBAR[{i}]" : $"  BAG[{i - _hotbarSize}]";
                string item = _slots[i].IsEmpty ? "(vacío)" : $"{_slots[i].displayName} x{_slots[i].amount}";
                Debug.Log($"{zone}: {item}");
            }
        }
#endif
    }
}
