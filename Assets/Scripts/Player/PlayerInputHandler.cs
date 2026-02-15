using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sistema de input CONSOLIDADO y optimizado
/// - Cache de actions
/// - API limpia
/// - Sin buscar strings cada frame
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    [Header("═══════ INPUT ASSET ═══════")]
    [SerializeField] InputActionAsset playerControls;

    [Header("═══════ ACTION MAP ═══════")]
    [SerializeField] string actionMapName = "Player";

    [Header("═══════ ACTION NAMES ═══════")]
    [SerializeField] string movementActionName = "Movement";
    [SerializeField] string rotationActionName = "Rotation";
    [SerializeField] string jumpActionName = "Jump";
    [SerializeField] string sprintActionName = "Sprint";
    [SerializeField] string interactActionName = "Interact";
    [SerializeField] string crouchActionName = "Crouch";

    // ═══════ CACHED ACTIONS ═══════

    InputAction movementAction;
    InputAction rotationAction;
    InputAction jumpAction;
    InputAction sprintAction;
    InputAction interactAction;
    InputAction crouchAction;

    // ═══════ INPUT STATE ═══════

    public Vector2 MovementInput { get; private set; }
    public Vector2 RotationInput { get; private set; }
    public bool JumpTrigger { get; private set; }
    public bool SprintTrigger { get; private set; }
    public bool InteractTrigger { get; private set; }
    public bool CrouchTrigger { get; private set; }

    // ═══════ LIFECYCLE ═══════

    void Awake()
    {
        if (playerControls == null)
        {
            Debug.LogError("❌ PlayerInputHandler: playerControls no asignado");
            enabled = false;
            return;
        }

        InitializeActions();
        SubscribeToEvents();
    }

    void InitializeActions()
    {
        InputActionMap map = playerControls.FindActionMap(actionMapName);

        if (map == null)
        {
            Debug.LogError($"❌ No se encontró el Action Map: {actionMapName}");
            enabled = false;
            return;
        }

        movementAction = map.FindAction(movementActionName);
        rotationAction = map.FindAction(rotationActionName);
        jumpAction = map.FindAction(jumpActionName);
        sprintAction = map.FindAction(sprintActionName);
        interactAction = map.FindAction(interactActionName);
        crouchAction = map.FindAction(crouchActionName);
    }

    void SubscribeToEvents()
    {
        // Movement
        if (movementAction != null)
        {
            movementAction.performed += ctx => MovementInput = ctx.ReadValue<Vector2>();
            movementAction.canceled += ctx => MovementInput = Vector2.zero;
        }

        // Rotation
        if (rotationAction != null)
        {
            rotationAction.performed += ctx => RotationInput = ctx.ReadValue<Vector2>();
            rotationAction.canceled += ctx => RotationInput = Vector2.zero;
        }

        // Jump
        if (jumpAction != null)
        {
            jumpAction.performed += ctx => JumpTrigger = true;
            jumpAction.canceled += ctx => JumpTrigger = false;
        }

        // Sprint
        if (sprintAction != null)
        {
            sprintAction.performed += ctx => SprintTrigger = true;
            sprintAction.canceled += ctx => SprintTrigger = false;
        }

        // Interact
        if (interactAction != null)
        {
            interactAction.performed += ctx => InteractTrigger = true;
        }

        // Crouch
        if (crouchAction != null)
        {
            crouchAction.performed += ctx => CrouchTrigger = true;
            crouchAction.canceled += ctx => CrouchTrigger = false;
        }
    }

    void OnEnable()
    {
        playerControls?.FindActionMap(actionMapName)?.Enable();
    }

    void OnDisable()
    {
        playerControls?.FindActionMap(actionMapName)?.Disable();
    }

    // ═══════ API PÚBLICA ═══════

    public void ResetInteractTrigger()
    {
        InteractTrigger = false;
    }

    public void ResetAllTriggers()
    {
        JumpTrigger = false;
        InteractTrigger = false;
    }

    public bool IsMoving()
    {
        return MovementInput.sqrMagnitude > 0.01f;
    }

    public float GetMovementMagnitude()
    {
        return MovementInput.magnitude;
    }
}