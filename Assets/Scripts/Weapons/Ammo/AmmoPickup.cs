using UnityEngine;

public class AmmoPickup : MonoBehaviour, IInteractable
{
    [Header("Ammo Settings")]
    public AmmoTypeSO ammoType;
    public int minAmmo = 5;
    public int maxAmmo = 30;

    private int currentAmount = 0;
    private bool ammoInitialized = false;

    void OnEnable()
    {
        // Generar munición solo al activarse (pool friendly)
        if (!ammoInitialized)
        {
            currentAmount = Random.Range(minAmmo, maxAmmo + 1);
            ammoInitialized = true;
        }
    }

    // Texto que verá el jugador
    public string GetInteractText()
    {
        if (ammoType != null)
            return "PICK UP " + ammoType.name;
        else
            return "PICK UP AMMO";
    }

    // Lógica al interactuar
    public void Interact(GameObject interactor)
    {
        // Buscamos el inventario DENTRO del jugador
        AmmoInventory playerInventory = interactor.GetComponentInChildren<AmmoInventory>();
        if (playerInventory == null || ammoType == null)
            return;

        var slot = playerInventory.GetSlot(ammoType);
        if (slot == null)
        {
            playerInventory.AddAmmo(ammoType, 0);
            slot = playerInventory.GetSlot(ammoType);
        }

        int spaceLeft = slot.maxAmount - slot.currentAmount;
        if (spaceLeft <= 0)
            return;

        int ammoToGive = Mathf.Min(currentAmount, spaceLeft);
        playerInventory.AddAmmo(ammoType, ammoToGive);

        currentAmount -= ammoToGive;

        // Si se acabó, se va al pool
        if (currentAmount <= 0)
        {
            ammoInitialized = false;
            gameObject.SetActive(false);
        }
    }
}
