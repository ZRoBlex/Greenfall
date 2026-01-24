using UnityEngine;

public class AmmoBox : MonoBehaviour
{
    [Header("UI")]
    public string displayName = "Ammo Box"; // 👈 editable en Inspector

    [Header("Ammo Type")]
    public AmmoTypeSO ammoType;   // Tipo de bala que da esta caja

    [Header("Random Ammo")]
    public int minAmmo = 5;
    public int maxAmmo = 30;

    private int currentAmount;
    private bool ammoInitialized = false;

    void OnEnable()
    {
        // 🔹 Generar SOLO cuando se activa por primera vez
        if (!ammoInitialized)
        {
            currentAmount = Random.Range(minAmmo, maxAmmo + 1);
            ammoInitialized = true;

            Debug.Log($"[AmmoBox] Generada con {currentAmount} balas de {ammoType?.name}");
        }
    }

    // Esto lo llama el jugador al interactuar
    public void Interact(GameObject interactor)
    {
        if (ammoType == null)
        {
            Debug.LogWarning("[AmmoBox] No tiene AmmoType asignado");
            return;
        }

        // 🔍 Buscar inventario en el jugador que interactúa
        AmmoInventory inventory = interactor.GetComponentInChildren<AmmoInventory>();
        if (inventory == null)
        {
            Debug.LogWarning("[AmmoBox] El jugador no tiene AmmoInventory");
            return;
        }

        // 🔹 Asegurar que exista el slot
        AmmoSlot slot = inventory.GetSlot(ammoType);
        if (slot == null)
        {
            inventory.AddAmmo(ammoType, 0);
            slot = inventory.GetSlot(ammoType);
        }

        if (slot == null)
        {
            Debug.LogWarning("[AmmoBox] No se pudo crear el slot de munición");
            return;
        }

        int spaceLeft = slot.maxAmount - slot.currentAmount;

        // ❌ Inventario lleno → no pasa nada
        if (spaceLeft <= 0)
        {
            Debug.Log($"[AmmoBox] Inventario lleno para {ammoType.name}, quedan {currentAmount} en la caja");
            return;
        }

        // 🔹 Dar solo lo que cabe
        int ammoToGive = Mathf.Min(currentAmount, spaceLeft);

        inventory.AddAmmo(ammoType, ammoToGive);
        currentAmount -= ammoToGive;

        Debug.Log($"[AmmoBox] Jugador recogió {ammoToGive} balas de {ammoType.name}. Quedan {currentAmount}");

        // ✅ Solo se desactiva cuando queda vacía
        if (currentAmount <= 0)
        {
            ammoInitialized = false;   // listo para pooling
            gameObject.SetActive(false);

            Debug.Log("[AmmoBox] Caja vacía → desactivada");
        }
    }

    // 🔹 Para UI futura: "PICK UP 15x 7.62mm"
    public string GetInteractText()
    {
        if (!string.IsNullOrEmpty(displayName))
            return $"Grab ({displayName})";

        if (ammoType != null)
            return $"Grab ({ammoType.name})";

        return "Grab (Ammo)";
    }

}
