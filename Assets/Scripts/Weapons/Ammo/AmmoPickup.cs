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
        if (!ammoInitialized)
        {
            currentAmount = Random.Range(minAmmo, maxAmmo + 1);
            ammoInitialized = true;
        }
    }

    public string GetInteractText()
    {
        if (ammoType != null)
            return "PICK UP " + ammoType.name;
        return "PICK UP AMMO";
    }

    public void Interact(GameObject interactor)
    {
        AmmoInventory inventory = interactor.GetComponentInChildren<AmmoInventory>();
        if (inventory == null || ammoType == null)
            return;

        var slot = inventory.GetSlot(ammoType);
        if (slot == null)
        {
            inventory.AddAmmo(ammoType, 0);
            slot = inventory.GetSlot(ammoType);
        }

        int spaceLeft = slot.maxAmount - slot.currentAmount;
        if (spaceLeft <= 0)
            return;

        int ammoToGive = Mathf.Min(currentAmount, spaceLeft);
        inventory.AddAmmo(ammoType, ammoToGive);

        currentAmount -= ammoToGive;

        if (currentAmount <= 0)
        {
            ammoInitialized = false;
            gameObject.SetActive(false);
        }
    }
}
