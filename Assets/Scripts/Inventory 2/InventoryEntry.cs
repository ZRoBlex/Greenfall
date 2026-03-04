// ============================================================
// InventoryEntry.cs
// Carpeta: Scripts/Inventory/
// ------------------------------------------------------------
// Contenedor universal de un item en runtime.
// El inventario SOLO trabaja con InventoryEntry.
// No sabe nada de Weapon ni SeedItem directamente.
//
// Para agregar un nuevo tipo de item al futuro (por ejemplo
// materiales de construcción), solo:
//   1. Agrega ItemKind.Material
//   2. Crea un InventoryEntry con esa kind
//   3. El inventario lo maneja igual que cualquier otro
// ============================================================

using UnityEngine;

namespace Greenfall.Inventory
{
    public enum ItemKind
    {
        Weapon,
        Seed,
        Consumable,
        Material,
        Ammo,
        Misc
    }

    /// <summary>
    /// Representa UN item (o stack de items) en un slot del inventario.
    /// Es solo datos: no tiene Update, no tiene lógica de juego.
    /// </summary>
    public class InventoryEntry
    {
        // ── Datos comunes a todos los items ──────────────────────
        public string   id;           // ID único del tipo de item (ej: "seed_wheat", "weapon_rifle")
        public string   displayName;  // Nombre visible en la UI
        public Sprite   icon;         // Ícono para la UI
        public ItemKind kind;         // Qué tipo de item es

        // ── Stacking ─────────────────────────────────────────────
        public int amount;            // Cuántos hay en este slot
        public int maxStack;          // Máximo apilable (1 = no apila)

        // ── Referencias específicas de tipo ──────────────────────
        // Solo UNA de estas estará asignada según el ItemKind.
        // El inventario no las lee; los sistemas externos (WeaponInventory,
        // FarmingSystem) las leen cuando necesitan actuar.

        /// <summary>Si es arma: la instancia del componente Weapon en escena.</summary>
        public Weapon    weaponInstance;

        /// <summary>Si es semilla: el ScriptableObject con los datos.</summary>
        public SeedItem  seedData;

        // ─────────────────────────────────────────────────────────

        public bool IsEmpty    => id == null;
        public bool IsStackable => maxStack > 1;
        public bool IsFull     => amount >= maxStack;

        /// <summary>
        /// Crea un InventoryEntry para un ARMA.
        /// La instancia del Weapon ya existe en escena (pickup la trae).
        /// </summary>
        public static InventoryEntry FromWeapon(Weapon weapon)
        {
            return new InventoryEntry
            {
                id             = "weapon_" + weapon.weaponName,
                displayName    = weapon.weaponName,
                icon           = weapon.icon,
                kind           = ItemKind.Weapon,
                amount         = 1,
                maxStack       = 1,
                weaponInstance = weapon
            };
        }

        /// <summary>
        /// Crea un InventoryEntry para una SEMILLA.
        /// </summary>
        public static InventoryEntry FromSeed(SeedItem seed, int amount)
        {
            return new InventoryEntry
            {
                id          = "seed_" + seed.seedId,
                displayName = seed.seedId,   // SeedItem no tiene displayName, usa seedId
                icon        = seed.icon,     // asumimos que SeedItem tiene .icon
                kind        = ItemKind.Seed,
                amount      = amount,
                maxStack    = 99,
                seedData    = seed
            };
        }

        /// <summary>Copia superficial de esta entrada (para drag & drop).</summary>
        public InventoryEntry Clone()
        {
            return new InventoryEntry
            {
                id             = id,
                displayName    = displayName,
                icon           = icon,
                kind           = kind,
                amount         = amount,
                maxStack       = maxStack,
                weaponInstance = weaponInstance,
                seedData       = seedData
            };
        }

        public void Clear()
        {
            id             = null;
            displayName    = null;
            icon           = null;
            amount         = 0;
            weaponInstance = null;
            seedData       = null;
        }
    }
}
