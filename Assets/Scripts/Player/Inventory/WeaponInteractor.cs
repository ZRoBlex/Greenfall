using UnityEngine;
using TMPro;

public class WeaponInteractor : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] float pickupDistance = 3f;
    [SerializeField] LayerMask weaponLayer;

    [Header("References")]
    [SerializeField] WeaponInventory inventory;
    [SerializeField] TextMeshProUGUI interactText;

    Weapon hoveredWeapon;

    void Update()
    {
        hoveredWeapon = null;

        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance, weaponLayer))
        {
            Weapon w = hit.collider.GetComponentInParent<Weapon>();
            if (w && !inventory.IsFull)
                hoveredWeapon = w;
        }

        interactText.gameObject.SetActive(hoveredWeapon != null);
    }

    public bool CanPickup => hoveredWeapon != null && !inventory.IsFull;

    public void Pickup()
    {
        if (!CanPickup) return;

        Weapon w = hoveredWeapon;
        hoveredWeapon = null;
        interactText.gameObject.SetActive(false);

        inventory.AddWeapon(w);
    }
}
