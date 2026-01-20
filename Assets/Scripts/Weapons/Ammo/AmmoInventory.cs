using System.Collections.Generic;
using UnityEngine;

public class AmmoInventory : MonoBehaviour
{
    public List<AmmoSlot> ammoSlots = new List<AmmoSlot>();

    public AmmoSlot GetSlot(AmmoTypeSO ammoType)
    {
        return ammoSlots.Find(s => s.ammoType == ammoType);
    }

    public bool ConsumeAmmo(AmmoTypeSO ammoType, int amount)
    {
        AmmoSlot slot = GetSlot(ammoType);
        if (slot == null) return false;
        return slot.Consume(amount);
    }

    public void AddAmmo(AmmoTypeSO ammoType, int amount)
    {
        AmmoSlot slot = GetSlot(ammoType);
        if (slot == null)
        {
            slot = new AmmoSlot(ammoType, ammoType.defaultMaxStack);
            ammoSlots.Add(slot);
        }
        slot.AddAmmo(amount);
    }
}