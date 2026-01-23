using UnityEngine;

public class WeaponInventoryUI : MonoBehaviour
{
    [SerializeField] WeaponInventory inventory;
    [SerializeField] WeaponSlotUI[] slots;

    void Awake()
    {
        Refresh();
    }

    public void Refresh()
    {
        Debug.Log("🔄 Refresh Inventory UI");

        for (int i = 0; i < slots.Length; i++)
        {
            Weapon w = inventory.GetWeaponAtUI(i);

            if (w == null)
            {
                Debug.Log($"Slot {i}: vacío");
                slots[i].Clear();
            }
            else
            {
                Debug.Log($"Slot {i}: {w.name} | icon = {w.icon}");
                bool selected = i == inventory.CurrentIndex;
                slots[i].Set(w, selected);
            }
        }
    }

}
