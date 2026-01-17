using UnityEngine;

public class WeaponSwayBinder : MonoBehaviour
{
    [SerializeField] WeaponInventory inventory;
    [SerializeField] SwayController sway;

    Weapon lastWeapon;

    void Update()
    {
        Weapon current = inventory.CurrentWeapon;

        if (current == lastWeapon) return;

        if (lastWeapon && lastWeapon.TryGetComponent(out SwayController oldSway))
            oldSway.enabled = false;

        lastWeapon = current;

        if (!current) return;

        SwayController newSway = current.GetComponent<SwayController>();
        if (!newSway)
            newSway = current.gameObject.AddComponent<SwayController>();

        sway = newSway;
        sway.enabled = true;
    }

    public void SetInputs(Vector2 move, Vector2 look)
    {
        if (!sway) return;
        sway.SetMovementInput(move);
        sway.SetLookInput(look);
    }
}
