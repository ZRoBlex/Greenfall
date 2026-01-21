using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AmmoInventory : MonoBehaviour
{
    [Header("Ammo Slots")]
    public List<AmmoSlot> ammoSlots = new List<AmmoSlot>();

    [Header("UI References")]
    [SerializeField] Image ammoIconImage;
    [SerializeField] TextMeshProUGUI ammoCountText;

    private AmmoTypeSO currentAmmoType;

    // ---------------------------
    public AmmoSlot GetSlot(AmmoTypeSO ammoType)
    {
        return ammoSlots.Find(s => s.ammoType == ammoType);
    }

    public bool ConsumeAmmo(AmmoTypeSO ammoType, int amount)
    {
        AmmoSlot slot = GetSlot(ammoType);
        if (slot == null) return false;

        bool result = slot.Consume(amount);

        if (ammoType == currentAmmoType)
            UpdateUI();

        return result;
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

        if (ammoType == currentAmmoType)
            UpdateUI();
    }

    // ---------------------------
    // 🔔 LLAMADO CUANDO CAMBIA EL ARMA
    // ---------------------------
    public void SetCurrentAmmoType(AmmoTypeSO ammoType)
    {
        currentAmmoType = ammoType;
        UpdateUI();
    }

    // ---------------------------
    void UpdateUI()
    {
        if (ammoCountText == null)
            return;

        if (currentAmmoType == null)
        {
            ammoCountText.text = "--";

            if (ammoIconImage)
                ammoIconImage.enabled = false;

            return;
        }

        AmmoSlot slot = GetSlot(currentAmmoType);

        int current = slot != null ? slot.currentAmount : 0;
        int max = slot != null ? slot.maxAmount : currentAmmoType.defaultMaxStack;

        // 👇 FORMATO: actual / máximo
        ammoCountText.text = $"{current} / {max}";

        // Imagen opcional (puedes ignorarla si quieres)
        if (ammoIconImage)
        {
            if (currentAmmoType.icon != null)
            {
                ammoIconImage.sprite = currentAmmoType.icon;
                ammoIconImage.enabled = true;
            }
            else
            {
                ammoIconImage.enabled = false;
            }
        }
    }

    public int RemoveAmmo(AmmoTypeSO ammoType, int amount)
    {
        AmmoSlot slot = GetSlot(ammoType);
        if (slot == null)
            return 0;

        int removed = Mathf.Min(slot.currentAmount, amount);
        slot.currentAmount -= removed;

        UpdateUI();
        return removed;
    }



}
