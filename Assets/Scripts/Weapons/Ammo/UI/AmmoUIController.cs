using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AmmoUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] TextMeshProUGUI ammoText;
    [SerializeField] Image ammoIconImage; // opcional

    private Weapon currentWeapon;

    // 🔔 llamado por WeaponInventory al cambiar de arma
    public void SetCurrentWeapon(Weapon weapon)
    {
        currentWeapon = weapon;
        UpdateUI();
    }

    void Update()
    {
        // refresco barato por frame (seguro y simple)
        if (currentWeapon != null)
            UpdateUI();
    }

    void UpdateUI()
    {
        if (ammoText == null)
            return;

        if (currentWeapon == null || currentWeapon.magazine == null)
        {
            ammoText.text = "-- / --";

            if (ammoIconImage)
                ammoIconImage.enabled = false;

            return;
        }

        int current = currentWeapon.magazine.currentBullets;
        int max = currentWeapon.magazine.maxBullets;

        ammoText.text = $"{current} / {max}";

        // imagen opcional (si luego existe)
        if (ammoIconImage)
        {
            if (currentWeapon.stats != null &&
                currentWeapon.stats.ammoType != null &&
                currentWeapon.stats.ammoType.icon != null)
            {
                ammoIconImage.sprite = currentWeapon.stats.ammoType.icon;
                ammoIconImage.enabled = true;
            }
            else
            {
                ammoIconImage.enabled = false;
            }
        }
    }
}
