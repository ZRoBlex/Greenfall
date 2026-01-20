using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AmmoSlot
{
    public AmmoTypeSO ammoType;
    public int currentAmount;
    public int maxAmount;

    public AmmoSlot(AmmoTypeSO type, int max)
    {
        ammoType = type;
        maxAmount = max;
        currentAmount = max;
    }

    public bool Consume(int amount)
    {
        if (currentAmount >= amount)
        {
            currentAmount -= amount;
            return true;
        }
        return false;
    }

    public void AddAmmo(int amount)
    {
        currentAmount = Mathf.Min(currentAmount + amount, maxAmount);
    }
}