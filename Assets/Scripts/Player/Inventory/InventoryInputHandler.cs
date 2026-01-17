using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryInputHandler : MonoBehaviour
{
    PlayerControls input;

    [Header("References")]
    [SerializeField] WeaponInventory inventory;
    [SerializeField] WeaponInteractor interactor;

    void Awake()
    {
        input = new PlayerControls();
    }

    void OnEnable()
    {
        input.Inventory.Enable();

        input.Inventory.Interact.performed += OnInteract;
        input.Inventory.Drop.performed += OnDrop;
        input.Inventory.NextSlot.performed += OnNextSlot;
        input.Inventory.PrevSlot.performed += OnPrevSlot;
    }

    void OnDisable()
    {
        input.Inventory.Interact.performed -= OnInteract;
        input.Inventory.Drop.performed -= OnDrop;
        input.Inventory.NextSlot.performed -= OnNextSlot;
        input.Inventory.PrevSlot.performed -= OnPrevSlot;

        input.Inventory.Disable();
    }

    // -----------------------
    void OnInteract(InputAction.CallbackContext ctx)
    {
        if (interactor.CanPickup)
            interactor.Pickup();
    }

    void OnDrop(InputAction.CallbackContext ctx)
    {
        inventory.DropCurrent();
    }

    void OnNextSlot(InputAction.CallbackContext ctx)
    {
        float value = ctx.ReadValue<float>();
        if (value > 0.1f)
            inventory.EquipNext();
    }

    void OnPrevSlot(InputAction.CallbackContext ctx)
    {
        float value = ctx.ReadValue<float>();
        if (value < -0.1f)
            inventory.EquipPrevious();
    }
}
