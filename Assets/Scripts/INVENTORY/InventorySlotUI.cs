using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text amountText;
    public Button button;

    InventoryUI inventoryUI;
    int index;

    public void Init(InventoryUI ui, int index)
    {
        this.inventoryUI = ui;
        this.index = index;

        button.onClick.AddListener(OnClick);
    }

    public void Set(InventorySlot slot)
    {
        if (slot == null || slot.IsEmpty)
        {
            icon.enabled = false;
            amountText.text = "";
        }
        else
        {
            icon.enabled = true;
            icon.sprite = slot.item.icon;

            amountText.text = slot.item.stackable && slot.amount > 1
                ? slot.amount.ToString()
                : "";
        }
    }

    void OnClick()
    {
        inventoryUI.OnSlotClicked(index);
    }
}
