// ============================================================
// UnifiedInventory.cs — FIX: RebuildHotbarWeapons()
// ============================================================
// FIX PRINCIPAL:
//   HandleWeaponsRebuilt() ahora hace un rebuild completo del
//   hotbar en lugar de ignorar el evento.
//
// LÓGICA DEL REBUILD:
//   1. Recorre los slots del hotbar.
//   2. Limpia todos los slots que contengan armas.
//   3. Lee las armas actuales de WeaponInventory en orden.
//   4. Las coloca en los primeros slots disponibles del hotbar.
//   5. Conserva semillas y otros items no-arma en sus slots.
//   6. Actualiza el slot activo según el índice equipado.
//
// Esto funciona aunque WeaponInventory haya comprimido su lista
// (al soltar la arma del slot 1, el slot 2 pasa a ser el 1,
//  y el rebuild lo refleja correctamente en la UI).
// ============================================================

using System;
using UnityEngine;

namespace Greenfall.Inventory
{
    public class UnifiedInventory : MonoBehaviour
    {
        public static UnifiedInventory Instance { get; private set; }

        [Header("Tamaños")]
        [SerializeField] private int _hotbarSize = 5;
        [SerializeField] private int _bagSize    = 20;

        [Header("WeaponInventory (fuente de verdad para armas)")]
        [SerializeField] private WeaponInventory _weaponInventory;

        private InventoryEntry[] _slots;

        public int TotalSlots => _hotbarSize + _bagSize;
        public int HotbarSize => _hotbarSize;
        public int BagSize    => _bagSize;

        private int _activeHotbarSlot = -1;
        public  int ActiveHotbarSlot  => _activeHotbarSlot;

        public event Action<int> OnSlotChanged;
        public event Action<int> OnActiveSlotChanged;

        public int  HotbarIndex(int local) => local;
        public int  BagIndex(int local)    => _hotbarSize + local;
        public bool IsHotbar(int global)   => global >= 0 && global < _hotbarSize;
        public bool IsBag(int global)      => global >= _hotbarSize && global < TotalSlots;

        // ── Awake ─────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _slots = new InventoryEntry[TotalSlots];
            for (int i = 0; i < TotalSlots; i++)
                _slots[i] = new InventoryEntry();
        }

        private void Start()
        {
            if (_weaponInventory == null)
            {
                Debug.LogWarning("[UnifiedInventory] WeaponInventory no asignado.");
                return;
            }

            _weaponInventory.OnWeaponAdded    += HandleWeaponAdded;
            _weaponInventory.OnWeaponEquipped += HandleWeaponEquipped;
            _weaponInventory.OnWeaponsRebuilt += HandleWeaponsRebuilt;

            InitializeFromWeaponInventory();
        }

        // ── Leer estado inicial de WeaponInventory ─────────────

        private void InitializeFromWeaponInventory()
        {
            for (int i = 0; i < _hotbarSize; i++)
            {
                Weapon w = _weaponInventory.GetWeaponAtUI(i);
                if (w == null) continue;
                _slots[i] = InventoryEntry.FromWeapon(w);
                OnSlotChanged?.Invoke(i);
            }

            int active = _weaponInventory.CurrentIndex;
            if (active >= 0 && active < _hotbarSize)
            {
                _activeHotbarSlot = active;
                OnActiveSlotChanged?.Invoke(active);
            }
        }

        // ── Lectura ───────────────────────────────────────────

        public InventoryEntry GetSlot(int globalIndex)
        {
            if (globalIndex < 0 || globalIndex >= TotalSlots)
                return new InventoryEntry();
            return _slots[globalIndex];
        }

        // ── Agregar item ──────────────────────────────────────

        public bool TryAdd(InventoryEntry entry)
        {
            if (entry == null || entry.IsEmpty) return false;

            if (entry.IsStackable)
            {
                int rem = TryStack(entry);
                if (rem <= 0) return true;
                entry.amount = rem;
            }

            if (entry.kind == ItemKind.Weapon)
            {
                if (TryPlaceInRange(entry, 0, _hotbarSize))          return true;
                if (TryPlaceInRange(entry, _hotbarSize, TotalSlots)) return true;
            }
            else
            {
                if (TryPlaceInRange(entry, _hotbarSize, TotalSlots)) return true;
                if (TryPlaceInRange(entry, 0, _hotbarSize))          return true;
            }

            return false;
        }

        private int TryStack(InventoryEntry entry)
        {
            int rem = entry.amount;
            for (int i = 0; i < TotalSlots && rem > 0; i++)
            {
                var s = _slots[i];
                if (s.IsEmpty || s.id != entry.id || s.IsFull) continue;
                int add = Mathf.Min(rem, s.maxStack - s.amount);
                s.amount += add;
                rem      -= add;
                OnSlotChanged?.Invoke(i);
            }
            return rem;
        }

        private bool TryPlaceInRange(InventoryEntry entry, int from, int to)
        {
            for (int i = from; i < to; i++)
            {
                if (!_slots[i].IsEmpty) continue;

                _slots[i] = entry;
                OnSlotChanged?.Invoke(i);

                if (entry.kind == ItemKind.Weapon && IsHotbar(i) && entry.weaponInstance != null)
                    if (!WeaponInventoryHasWeapon(entry.weaponInstance))
                        _weaponInventory?.AddWeapon(entry.weaponInstance);

                return true;
            }
            return false;
        }

        private bool WeaponInventoryHasWeapon(Weapon w)
        {
            if (_weaponInventory == null) return false;
            for (int i = 0; i < _weaponInventory.MaxSlots; i++)
                if (_weaponInventory.GetWeaponAtUI(i) == w) return true;
            return false;
        }

        // ── Mover item (drag & drop) ──────────────────────────

        public void Move(int fromGlobal, int toGlobal)
        {
            if (fromGlobal == toGlobal) return;
            if (fromGlobal < 0 || fromGlobal >= TotalSlots) return;
            if (toGlobal   < 0 || toGlobal   >= TotalSlots) return;

            var from = _slots[fromGlobal];
            var to   = _slots[toGlobal];

            if (from.IsEmpty) return;

            // Apilar si mismo item stackable
            if (!to.IsEmpty && to.id == from.id && from.IsStackable)
            {
                int space = from.maxStack - to.amount;
                if (space > 0)
                {
                    int t       = Mathf.Min(from.amount, space);
                    to.amount  += t;
                    from.amount -= t;
                    if (from.amount <= 0) from.Clear();
                    OnSlotChanged?.Invoke(fromGlobal);
                    OnSlotChanged?.Invoke(toGlobal);
                    return;
                }
            }

            _slots[fromGlobal] = to;
            _slots[toGlobal]   = from;

            OnSlotChanged?.Invoke(fromGlobal);
            OnSlotChanged?.Invoke(toGlobal);

            SyncWeaponInventory();
        }

        // ── Sincronizar WeaponInventory tras drag ─────────────

        private void SyncWeaponInventory()
        {
            if (_weaponInventory == null) return;

            var weapons = new Weapon[_hotbarSize];
            int count   = 0;
            for (int i = 0; i < _hotbarSize; i++)
            {
                var e = _slots[i];
                if (!e.IsEmpty && e.kind == ItemKind.Weapon && e.weaponInstance != null)
                    weapons[count++] = e.weaponInstance;
            }

            int activeIdx = 0, counted = 0;
            for (int i = 0; i < _hotbarSize; i++)
            {
                var e = _slots[i];
                if (!e.IsEmpty && e.kind == ItemKind.Weapon)
                {
                    if (i == _activeHotbarSlot) activeIdx = counted;
                    counted++;
                }
            }

            _weaponInventory.SetWeaponsFromInventory(weapons, activeIdx);
        }

        // ── Quitar item ───────────────────────────────────────

        public bool Remove(int globalIndex, int amount = 1)
        {
            if (globalIndex < 0 || globalIndex >= TotalSlots) return false;
            var slot = _slots[globalIndex];
            if (slot.IsEmpty) return false;

            slot.amount -= amount;
            if (slot.amount <= 0) slot.Clear();

            OnSlotChanged?.Invoke(globalIndex);
            if (IsHotbar(globalIndex)) SyncWeaponInventory();
            return true;
        }

        // ── Selección de hotbar ───────────────────────────────

        public void SetActiveHotbar(int hotbarLocal)
        {
            int global = HotbarIndex(hotbarLocal);
            if (!IsHotbar(global)) return;

            _activeHotbarSlot = global;
            OnActiveSlotChanged?.Invoke(global);

            if (_weaponInventory != null)
            {
                var entry = _slots[global];
                if (!entry.IsEmpty && entry.kind == ItemKind.Weapon)
                {
                    int weaponIdx = 0;
                    for (int i = 0; i < global; i++)
                    {
                        var e = _slots[i];
                        if (!e.IsEmpty && e.kind == ItemKind.Weapon) weaponIdx++;
                    }
                    _weaponInventory.Equip(weaponIdx);
                }
            }
        }

        // ── Handlers de eventos de WeaponInventory ────────────

        private void HandleWeaponAdded(int slotIndex, Weapon weapon)
        {
            if (slotIndex < 0 || slotIndex >= _hotbarSize) return;
            int global = HotbarIndex(slotIndex);
            if (!_slots[global].IsEmpty) return; // ya está reflejado

            _slots[global] = InventoryEntry.FromWeapon(weapon);
            OnSlotChanged?.Invoke(global);
        }

        private void HandleWeaponEquipped(int weaponInventoryIndex)
        {
            if (weaponInventoryIndex < 0)
            {
                // Sin arma equipada
                if (_activeHotbarSlot != -1)
                {
                    _activeHotbarSlot = -1;
                    OnActiveSlotChanged?.Invoke(-1);
                }
                return;
            }

            int counted = 0;
            for (int i = 0; i < _hotbarSize; i++)
            {
                var e = _slots[i];
                if (!e.IsEmpty && e.kind == ItemKind.Weapon)
                {
                    if (counted == weaponInventoryIndex)
                    {
                        if (_activeHotbarSlot != i)
                        {
                            _activeHotbarSlot = i;
                            OnActiveSlotChanged?.Invoke(i);
                        }
                        return;
                    }
                    counted++;
                }
            }
        }

        /// <summary>
        /// FIX PRINCIPAL: rebuild completo del hotbar desde WeaponInventory.
        /// Se llama después de cualquier drop o reordenamiento.
        ///
        /// PROCESO:
        ///   1. Limpiar todos los slots de hotbar que tengan armas.
        ///   2. Colocar las armas de WeaponInventory en los primeros
        ///      slots vacíos de hotbar (en orden).
        ///   3. Los items no-arma (semillas, etc.) se conservan.
        ///   4. Actualizar el slot activo.
        /// </summary>
        private void HandleWeaponsRebuilt()
        {
            // ── Paso 1: limpiar slots de armas en hotbar ──────────
            for (int i = 0; i < _hotbarSize; i++)
            {
                if (!_slots[i].IsEmpty && _slots[i].kind == ItemKind.Weapon)
                {
                    _slots[i].Clear();
                    OnSlotChanged?.Invoke(i);
                }
            }

            // ── Paso 2: recolocar armas de WeaponInventory ────────
            int slotIdx = 0;
            for (int w = 0; w < _weaponInventory.SlotCount; w++)
            {
                Weapon weapon = _weaponInventory.GetWeaponAtUI(w);
                if (weapon == null) continue;

                // Buscar el próximo slot de hotbar vacío
                while (slotIdx < _hotbarSize && !_slots[slotIdx].IsEmpty)
                    slotIdx++;

                if (slotIdx >= _hotbarSize) break; // hotbar llena

                _slots[slotIdx] = InventoryEntry.FromWeapon(weapon);
                OnSlotChanged?.Invoke(slotIdx);
                slotIdx++;
            }

            // ── Paso 3: actualizar slot activo ────────────────────
            int weaponActive = _weaponInventory.CurrentIndex;

            if (weaponActive < 0)
            {
                _activeHotbarSlot = -1;
                OnActiveSlotChanged?.Invoke(-1);
                return;
            }

            // El slot activo = el slot donde quedó el arma en índice weaponActive
            int weaponCounter = 0;
            for (int i = 0; i < _hotbarSize; i++)
            {
                var e = _slots[i];
                if (!e.IsEmpty && e.kind == ItemKind.Weapon)
                {
                    if (weaponCounter == weaponActive)
                    {
                        _activeHotbarSlot = i;
                        OnActiveSlotChanged?.Invoke(i);
                        return;
                    }
                    weaponCounter++;
                }
            }
        }

        // ── Limpieza ──────────────────────────────────────────

        private void OnDestroy()
        {
            if (_weaponInventory == null) return;
            _weaponInventory.OnWeaponAdded    -= HandleWeaponAdded;
            _weaponInventory.OnWeaponEquipped -= HandleWeaponEquipped;
            _weaponInventory.OnWeaponsRebuilt -= HandleWeaponsRebuilt;
        }

#if UNITY_EDITOR
        [ContextMenu("Debug: Print Inventory")]
        private void DebugPrint()
        {
            for (int i = 0; i < TotalSlots; i++)
            {
                string zone = IsHotbar(i) ? $"HOTBAR[{i}]" : $"  BAG[{i - _hotbarSize}]";
                string mark = i == _activeHotbarSlot ? " ◄ ACTIVO" : "";
                string item = _slots[i].IsEmpty
                    ? "(vacío)"
                    : $"{_slots[i].displayName} x{_slots[i].amount} [{_slots[i].kind}]";
                Debug.Log($"{zone}: {item}{mark}");
            }
        }
#endif
    }
}
