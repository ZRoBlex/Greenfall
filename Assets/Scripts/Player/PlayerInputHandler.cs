using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Actions Asset")]
    [SerializeField] InputActionAsset playerControls;

    [Header("Action Map Reference")]
    [SerializeField] string actionMapName = "Player";

    [Header("Action Name Reference")]
    [SerializeField] string movement = "Movement";
    [SerializeField] string rotation = "Rotation";
    [SerializeField] string Jump = "Jump";
    [SerializeField] string Sprint = "Sprint";
    [SerializeField] string Crouch = "Crouch"; // 👈 NUEVO

    private InputAction movementAction;
    private InputAction rotationAction;
    private InputAction JumpAction;
    private InputAction SprintAction;
    private InputAction CrouchAction; // 👈 NUEVO

    public Vector2 MovementInput { get; private set; }
    public Vector2 RotationInput { get; private set; }
    public bool JumpTrigger { get; private set; }
    public bool SprintTrigger { get; private set; }
    public bool CrouchTrigger { get; private set; } // 👈 NUEVO


    private void Awake()
    {
        InputActionMap mapReference = playerControls.FindActionMap(actionMapName);

        movementAction = mapReference.FindAction(movement);
        rotationAction = mapReference.FindAction(rotation);
        JumpAction = mapReference.FindAction(Jump);
        SprintAction = mapReference.FindAction(Sprint);
        CrouchAction = mapReference.FindAction(Crouch); // 👈 NUEVO

        SuscribeActionValuesToInputEvents();
    }

    private void SuscribeActionValuesToInputEvents()
    {
        movementAction.performed += inputInfo => MovementInput = inputInfo.ReadValue<Vector2>();
        movementAction.canceled += inputInfo => MovementInput = Vector2.zero;

        rotationAction.performed += inputInfo => RotationInput = inputInfo.ReadValue<Vector2>();
        rotationAction.canceled += inputInfo => RotationInput = Vector2.zero;

        JumpAction.performed += inputInfo => JumpTrigger = true;
        JumpAction.canceled += inputInfo => JumpTrigger = false;

        SprintAction.performed += inputInfo => SprintTrigger = true;
        SprintAction.canceled += inputInfo => SprintTrigger = false;

        // 👇 CROUCH
        CrouchAction.performed += _ =>
            CrouchTrigger = true;
        CrouchAction.canceled += _ =>
            CrouchTrigger = false;
    }

    private void OnEnable()
    {
        playerControls.FindActionMap(actionMapName).Enable();
    }

    private void OnDisable()
    {
        playerControls.FindActionMap(actionMapName).Disable();
    }
}
