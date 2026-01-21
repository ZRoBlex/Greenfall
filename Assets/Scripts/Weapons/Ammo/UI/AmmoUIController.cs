using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AmmoUIController : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] Image weaponImage;
    [SerializeField] TMP_Text ammoText;

    AmmoTypeSO currentAmmoType;
    AmmoInventory ammoInventory;

    // ------------------------------------------------
    public void Bind(AmmoInventory inventory)
    {
        ammoInventory = inventory;
        Refresh();
    }

    // ------------------------------------------------
    public void SetCurrentWeapon(Weapon weapon)
    {
        if (weapon == null || weapon.stats == null)
        {
            currentAmmoType = null;
            Clear();
            return;
        }

        currentAmmoType = weapon.stats.ammoType;

        // Imagen del arma (si existe)
        if (weaponImage)
        {
            if (weapon.stats.weaponIcon != null)
            {
                weaponImage.sprite = weapon.stats.weaponIcon;
                weaponImage.enabled = true;
            }
            else
            {
                weaponImage.enabled = false;
            }
        }


        Refresh();
    }

    // ------------------------------------------------
    public void Refresh()
    {
        if (ammoText == null) return;

        if (ammoInventory == null || currentAmmoType == null)
        {
            ammoText.text = "--";
            return;
        }

        AmmoSlot slot = ammoInventory.GetSlot(currentAmmoType);
        if (slot == null)
        {
            ammoText.text = "0";
            return;
        }

        ammoText.text = $"{slot.currentAmount}";
    }

    // ------------------------------------------------
    void Clear()
    {
        if (weaponImage)
            weaponImage.enabled = false;

        if (ammoText)
            ammoText.text = "--";
    }
}
