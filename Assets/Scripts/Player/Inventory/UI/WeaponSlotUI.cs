using UnityEngine;
using UnityEngine.UI;

public class WeaponSlotUI : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] GameObject selectedFrame;

    // 🔹 Método que usa el inventario UI
    public void Set(Weapon weapon, bool selected)
    {
        SetWeapon(weapon, selected);
    }

    // 🔹 Método para limpiar el slot
    public void Clear()
    {
        if (icon != null)
            icon.enabled = false;

        if (selectedFrame != null)
            selectedFrame.SetActive(false);
    }

    // 🔹 Tu método original (lo dejamos)
    public void SetWeapon(Weapon weapon, bool selected)
    {
        if (weapon == null || weapon.icon == null)
        {
            if (icon != null)
                icon.enabled = false;
        }
        else
        {
            icon.enabled = true;
            icon.sprite = weapon.icon;
        }

        if (selectedFrame != null)
            selectedFrame.SetActive(selected);
    }
}
