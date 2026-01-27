using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DragItemUI : MonoBehaviour
{
    public static DragItemUI Instance;

    public Image icon;
    public TMP_Text amountText;
    public CanvasGroup canvasGroup;

    InventorySlot carriedSlot = new();
    int carriedFromIndex = -1;

    void Awake()
    {
        Instance = this;
        Hide();
    }

    void Update()
    {
        if (carriedSlot.item != null)
            transform.position = Input.mousePosition;
    }

    public void OnSlotClicked(int index, InventorySlot clickedSlot)
    {
        var inv = GeneralInventory.Instance;

        // 1️⃣ No llevo nada → tomar stack
        if (carriedSlot.item == null)
        {
            if (clickedSlot.IsEmpty) return;

            carriedSlot.item = clickedSlot.item;
            carriedSlot.amount = clickedSlot.amount;
            carriedFromIndex = index;

            clickedSlot.item = null;
            clickedSlot.amount = 0;

            Show();
            UpdateUI();
            return;
        }

        // 2️⃣ Llevo algo → soltar / combinar
        var targetSlot = inv.slots[index];

        // mismo item → combinar
        if (targetSlot.item == carriedSlot.item && targetSlot.item.stackable)
        {
            int remaining = targetSlot.Add(carriedSlot.amount);
            carriedSlot.amount = remaining;

            if (carriedSlot.amount <= 0)
                ClearCarried();

            UpdateUI();
            return;
        }

        // slot vacío → mover
        if (targetSlot.IsEmpty)
        {
            targetSlot.item = carriedSlot.item;
            targetSlot.amount = carriedSlot.amount;

            ClearCarried();
            return;
        }

        // diferente item → swap
        var tmpItem = targetSlot.item;
        var tmpAmount = targetSlot.amount;

        targetSlot.item = carriedSlot.item;
        targetSlot.amount = carriedSlot.amount;

        carriedSlot.item = tmpItem;
        carriedSlot.amount = tmpAmount;

        carriedFromIndex = index;
        UpdateUI();
    }

    void ClearCarried()
    {
        carriedSlot.item = null;
        carriedSlot.amount = 0;
        carriedFromIndex = -1;
        Hide();
    }

    void UpdateUI()
    {
        if (carriedSlot.item == null)
        {
            Hide();
            return;
        }

        icon.sprite = carriedSlot.item.icon;
        icon.enabled = true;

        amountText.text = carriedSlot.item.stackable && carriedSlot.amount > 1
            ? carriedSlot.amount.ToString()
            : "";

        Show();
    }

    void Show()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
    }

    void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }
}
