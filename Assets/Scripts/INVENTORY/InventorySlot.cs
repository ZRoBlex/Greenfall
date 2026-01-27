using UnityEngine;

[System.Serializable]
public class InventorySlot
{
    public ItemSO item;
    public int amount;

    public bool IsEmpty => item == null || amount <= 0;
    public bool IsFull => item != null && amount >= item.maxStack;

    public InventorySlot() { }

    public InventorySlot(ItemSO item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }

    public int Add(int value)
    {
        if (item == null) return value;

        int space = item.maxStack - amount;
        int toAdd = Mathf.Min(space, value);
        amount += toAdd;
        return value - toAdd; // lo que sobró
    }

    public int Remove(int value)
    {
        int toRemove = Mathf.Min(amount, value);
        amount -= toRemove;
        if (amount <= 0)
        {
            item = null;
            amount = 0;
        }
        return toRemove;
    }
}
