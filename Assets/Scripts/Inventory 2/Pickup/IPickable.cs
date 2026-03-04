// ============================================================
// IPickable.cs
// Carpeta: Scripts/Pickup/
// ------------------------------------------------------------
// Contrato que implementa CUALQUIER objeto recogible del mundo.
// WeaponPickup, SeedPickup, MaterialPickup, etc.
//
// El jugador (PlayerInteractor) solo llama TryPickup().
// No sabe si es un arma, semilla o material.
// Cada pickup sabe QUÉ hacer cuando se lo recogen.
//
// PARA AGREGAR UN NUEVO TIPO DE OBJETO RECOGIBLE:
//   1. Crea "MaterialPickup.cs"
//   2. Implementa IPickable
//   3. Implementa TryPickup() → llama UnifiedInventory.Instance.TryAdd(...)
//   4. El PlayerInteractor lo recogerá automáticamente
// ============================================================

using UnityEngine;

namespace Greenfall.Inventory
{
    /// <summary>
    /// Cualquier objeto en el mundo que el jugador pueda recoger.
    /// </summary>
    public interface IPickable
    {
        /// <summary>
        /// Texto que aparece en la UI de interacción. Ej: "Recoger AK-47 [E]"
        /// </summary>
        string GetPickupLabel();

        /// <summary>
        /// Intenta agregar este objeto al inventario del jugador.
        /// Si hay espacio → se recoge (el objeto se desactiva/destruye).
        /// Si no hay espacio → retorna false y el objeto permanece en el mundo.
        /// </summary>
        bool TryPickup();

        /// <summary>
        /// El Transform del objeto en el mundo (para mostrar outlines, efectos, etc.)
        /// </summary>
        Transform WorldTransform { get; }
    }
}
