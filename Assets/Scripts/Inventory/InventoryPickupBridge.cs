using UnityEngine;

public class InventoryPickupBridge : MonoBehaviour
{
    public InventoryItemData inventoryItem;
    public int amount = 1;

    public bool TryPickup()
    {
        if (inventoryItem == null)
            return false;

        return InventoryManager.Instance.AddItem(inventoryItem);
    }
}
