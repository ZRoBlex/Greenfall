using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponInteractor : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] float pickupDistance = 3f;
    [SerializeField] LayerMask weaponLayer;

    [Header("References")]
    [SerializeField] WeaponInventory inventory;
    [SerializeField] TextMeshProUGUI interactText;

    [SerializeField] PlayerInput playerInput;
    [SerializeField] string interactActionName = "Interact";
    InputAction interactAction;


    Weapon hoveredWeapon;

    void Awake()
    {
        if (playerInput != null)
        {
            interactAction = playerInput.actions[interactActionName];
        }
    }


    void Update()
{
    hoveredWeapon = null;

    Ray ray = new Ray(transform.position, transform.forward);
    if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance, weaponLayer))
    {
        Weapon w = hit.collider.GetComponentInParent<Weapon>();
        if (w)
            hoveredWeapon = w;
    }

    if (hoveredWeapon != null)
    {
        UpdateInteractText();
        interactText.gameObject.SetActive(true);
    }
    else
    {
        interactText.gameObject.SetActive(false);
    }
}


    public bool CanPickup => hoveredWeapon != null;

    public void Pickup()
    {
        if (!CanPickup) return;

        Weapon w = hoveredWeapon;
        hoveredWeapon = null;
        interactText.gameObject.SetActive(false);

        //inventory.AddWeapon(w);
        inventory.PickupWeapon(w);

    }

    string GetInteractKey()
    {
        if (interactAction == null)
            return "?";

        // Obtener el binding efectivo (soporta rebinds)
        var binding = interactAction.bindings[0];
        return InputControlPath.ToHumanReadableString(
            binding.effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice
        );
    }

    void UpdateInteractText()
    {
        string key = GetInteractKey();

        bool willSwap = inventory.IsFull;

        string actionText = willSwap ? "Swap Weapon" : "Interact";

        interactText.alignment = TextAlignmentOptions.Center;
        interactText.text = $"{actionText}\n<size=70%>({key})</size>";
    }


}
