using UnityEngine;
using System.Collections.Generic;

public class GeneralInventory : MonoBehaviour
{
    public static GeneralInventory Instance;

    [Header("Settings")]
    public int maxSlots = 30;

    [Header("Runtime")]
    public List<InventorySlot> slots = new();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Inicializar slots vacíos
        for (int i = 0; i < maxSlots; i++)
            slots.Add(new InventorySlot());
    }

    // =========================
    // ADD
    // =========================
    public bool AddItem(ItemSO item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        int remaining = amount;

        // 1) Rellenar stacks existentes
        if (item.stackable)
        {
            foreach (var slot in slots)
            {
                if (slot.item == item && !slot.IsFull)
                {
                    remaining = slot.Add(remaining);
                    if (remaining <= 0)
                        return true;
                }
            }
        }

        // 2) Usar slots vacíos
        foreach (var slot in slots)
        {
            if (slot.IsEmpty)
            {
                slot.item = item;
                slot.amount = 0;
                remaining = slot.Add(remaining);

                if (remaining <= 0)
                    return true;
            }
        }

        // No cupo todo
        return false;
    }

    // =========================
    // REMOVE
    // =========================
    public bool RemoveItem(ItemSO item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        int remaining = amount;

        foreach (var slot in slots)
        {
            if (slot.item == item)
            {
                int removed = slot.Remove(remaining);
                remaining -= removed;

                if (remaining <= 0)
                    return true;
            }
        }

        return false; // no había suficientes
    }

    // =========================
    // QUERY
    // =========================
    public int GetItemCount(ItemSO item)
    {
        int total = 0;

        foreach (var slot in slots)
        {
            if (slot.item == item)
                total += slot.amount;
        }

        return total;
    }

    public bool HasItem(ItemSO item, int amount = 1)
    {
        return GetItemCount(item) >= amount;
    }

    // =========================
    // DEBUG
    // =========================
    [ContextMenu("Print Inventory")]
    public void PrintInventory()
    {
        Debug.Log("==== GENERAL INVENTORY ====");
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty)
                Debug.Log($"{slot.item.displayName} x{slot.amount}");
        }
    }
}
