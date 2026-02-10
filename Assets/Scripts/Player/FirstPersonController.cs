using UnityEngine;

/// <summary>
/// Controlador de movimiento ULTRA optimizado
/// - Cache agresivo
/// - Sin allocations
/// - Cálculos mínimos
/// </summary>
public class FirstPersonController : MonoBehaviour
{
    [Header("═══════ MOVIMIENTO ═══════")]
    [Range(1f, 10f)]
    [SerializeField] float walkSpeed = 3f;

    [Range(1.5f, 3f)]
    [SerializeField] float sprintMultiplier = 2f;

    [Header("═══════ SALTO ═══════")]
    [Range(3f, 10f)]
    [SerializeField] float jumpForce = 5f;

    [Range(0.5f, 3f)]
    [SerializeField] float gravityMultiplier = 1f;

    [Header("═══════ CÁMARA ═══════")]
    [Range(0.05f, 0.5f)]
    [SerializeField] float mouseSensitivity = 0.1f;

    [Range(60f, 90f)]
    [SerializeField] float upDownLookRange = 80f;

    [Header("═══════ AGACHARSE ═══════")]
    [Range(0.5f, 1.5f)]
    [SerializeField] float crouchHeight = 1f;

    [Range(1.5f, 2.5f)]
    [SerializeField] float standingHeight = 1.8f;

    [Range(0.3f, 0.8f)]
    [SerializeField] float crouchSpeedMultiplier = 0.5f;

    [Range(3f, 15f)]
    [SerializeField] float crouchTransitionSpeed = 8f;

    [Header("═══════ CÁMARA CROUCH ═══════")]
    [Range(0.5f, 1.8f)]
    [SerializeField] float cameraStandingHeight = 1.6f;

    [Range(0.3f, 1.2f)]
    [SerializeField] float cameraCrouchHeight = 1f;

    [Header("═══════ AIM ═══════")]
    [Range(0.1f, 0.8f)]
    [SerializeField] float aimMoveMultiplier = 0.4f;

    [Range(0.1f, 0.8f)]
    [SerializeField] float aimSensitivityMultiplier = 0.5f;

    [Header("═══════ TECHO ═══════")]
    [SerializeField] LayerMask ceilingMask;
    [SerializeField] float ceilingRayOffset = 0.05f;

    [Header("═══════ REFERENCIAS ═══════")]
    [SerializeField] CharacterController characterController;
    [SerializeField] Camera mainCamera;
    [SerializeField] PlayerInputHandler playerInputHandler;
    [SerializeField] Animator playerAnimator;
    [SerializeField] PlayerStats stats;

    [Header("═══════ SWAY ═══════")]
    [SerializeField] WeaponSwayBinder weaponSwayBinder;
    [SerializeField] CameraBobController cameraVisualSway;

    [Header("═══════ CROSSHAIR ═══════")]
    [SerializeField] DynamicCrosshair crosshair;
    [SerializeField] CrosshairProfile defaultCrosshairProfile;

    // ═══════ ESTADO ═══════

    bool isCrouching;
    float targetHeight;
    float cameraTargetHeight;

    float currentMoveMultiplier = 1f;
    float currentSensitivityMultiplier = 1f;

    // ═══════ MOVIMIENTO ═══════

    Vector3 currentMovement;
    float verticalRotation;

    // ═══════ CACHE ═══════

    Transform cachedTransform;
    Transform cameraTransform;
    Vector3 cameraLocalPos;

    // ═══════ PROPIEDADES CALCULADAS ═══════

    float CurrentSpeed =>
        walkSpeed *
        (isCrouching ? crouchSpeedMultiplier : 1f) *
        (ShouldSprint() ? sprintMultiplier : 1f) *
        currentMoveMultiplier;

    bool ShouldSprint() =>
        playerInputHandler.SprintTrigger &&
        !isCrouching &&
        stats != null &&
        stats.CanSprint();

    // ═══════ LIFECYCLE ═══════

    void Start()
    {
        cachedTransform = transform;
        cameraTransform = mainCamera.transform;
        cameraLocalPos = cameraTransform.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        targetHeight = standingHeight;
        characterController.height = standingHeight;

        cameraTargetHeight = cameraStandingHeight;
        cameraLocalPos.y = cameraStandingHeight;
        cameraTransform.localPosition = cameraLocalPos;

        if (crosshair != null && crosshair.Profile == null && defaultCrosshairProfile != null)
            crosshair.SetProfile(defaultCrosshairProfile);
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleCrouch();
        UpdateVisuals();
    }

    // ═══════ MOVIMIENTO ═══════

    void HandleMovement()
    {
        Vector3 worldDirection = CalculateWorldDirection();

        currentMovement.x = worldDirection.x * CurrentSpeed;
        currentMovement.z = worldDirection.z * CurrentSpeed;

        HandleJumping();

        characterController.Move(currentMovement * Time.deltaTime);
    }

    Vector3 CalculateWorldDirection()
    {
        Vector2 input = playerInputHandler.MovementInput;
        Vector3 inputDirection = new Vector3(input.x, 0f, input.y);
        return cachedTransform.TransformDirection(inputDirection).normalized;
    }

    void HandleJumping()
    {
        if (characterController.isGrounded)
        {
            currentMovement.y = -0.5f;

            if (crosshair != null)
                crosshair.SetAirborne(false);

            if (playerAnimator != null)
                playerAnimator.SetBool("isGrounded", true);

            if (playerInputHandler.JumpTrigger)
                currentMovement.y = jumpForce;
        }
        else
        {
            currentMovement.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;

            if (crosshair != null)
                crosshair.SetAirborne(true);

            if (playerAnimator != null)
                playerAnimator.SetBool("isGrounded", false);
        }
    }

    // ═══════ ROTACIÓN ═══════

    void HandleRotation()
    {
        Vector2 rotInput = playerInputHandler.RotationInput;

        float mouseX = rotInput.x * mouseSensitivity * currentSensitivityMultiplier;
        float mouseY = rotInput.y * mouseSensitivity * currentSensitivityMultiplier;

        cachedTransform.Rotate(0f, mouseX, 0f);

        verticalRotation = Mathf.Clamp(verticalRotation - mouseY, -upDownLookRange, upDownLookRange);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    // ═══════ CROUCH ═══════

    void HandleCrouch()
    {
        bool crouchInput = playerInputHandler.CrouchTrigger;

        if (crouchInput)
        {
            isCrouching = true;
            targetHeight = crouchHeight;
            cameraTargetHeight = cameraCrouchHeight;

            if (crosshair != null)
                crosshair.SetCrouch(true);
        }
        else
        {
            if (isCrouching && CanStandUp())
            {
                isCrouching = false;
                targetHeight = standingHeight;
                cameraTargetHeight = cameraStandingHeight;

                if (crosshair != null)
                    crosshair.SetCrouch(false);
            }
        }

        // Transición suave de collider
        characterController.height = Mathf.Lerp(
            characterController.height,
            targetHeight,
            Time.deltaTime * crouchTransitionSpeed
        );

        characterController.center = new Vector3(0f, characterController.height * 0.5f, 0f);

        // Transición suave de cámara
        cameraLocalPos.y = Mathf.Lerp(
            cameraLocalPos.y,
            cameraTargetHeight,
            Time.deltaTime * crouchTransitionSpeed
        );

        cameraTransform.localPosition = cameraLocalPos;
    }

    bool CanStandUp()
    {
        Vector3 rayOrigin = cachedTransform.position + Vector3.up * ceilingRayOffset;
        float rayLength = standingHeight - ceilingRayOffset;

        return !Physics.Raycast(rayOrigin, Vector3.up, rayLength, ceilingMask, QueryTriggerInteraction.Ignore);
    }

    // ═══════ VISUALS ═══════

    void UpdateVisuals()
    {
        Vector2 moveInput = playerInputHandler.MovementInput;
        Vector2 lookInput = playerInputHandler.RotationInput;

        // Animator
        if (playerAnimator != null)
        {
            float moveMagnitude = new Vector3(moveInput.x, 0f, moveInput.y).magnitude * 0.5f;
            if (ShouldSprint())
                moveMagnitude *= 2f;

            playerAnimator.SetFloat("Speed", moveMagnitude);
        }

        // Camera bob
        if (cameraVisualSway != null)
        {
            cameraVisualSway.SetMovementInput(moveInput);
            cameraVisualSway.SetSprint(playerInputHandler.SprintTrigger);
        }

        // Weapon sway
        if (weaponSwayBinder != null)
            weaponSwayBinder.SetInputs(moveInput, lookInput);

        // Crosshair
        UpdateCrosshair(moveInput);
    }

    void UpdateCrosshair(Vector2 moveInput)
    {
        if (crosshair == null)
            return;

        float inputMagnitude = Mathf.Clamp01(moveInput.magnitude);

        if (ShouldSprint())
            inputMagnitude *= 1.5f;

        inputMagnitude *= currentMoveMultiplier;

        crosshair.SetMovementSpread(inputMagnitude);
    }

    // ═══════ API PÚBLICA ═══════

    public void SetAimModifiers(bool aiming)
    {
        if (isCrouching) return;

        currentMoveMultiplier = aiming ? aimMoveMultiplier : 1f;
        currentSensitivityMultiplier = aiming ? aimSensitivityMultiplier : 1f;
    }

    public void SetAimMoveMultiplier(float value)
    {
        currentMoveMultiplier = value;
    }

    public void SetAimSensitivityMultiplier(float value)
    {
        currentSensitivityMultiplier = value;
    }

    public void ResetAimModifiers()
    {
        currentMoveMultiplier = 1f;
        currentSensitivityMultiplier = 1f;
    }

    public Vector2 GetMovementInput() => playerInputHandler.MovementInput;

    public bool IsSprinting() => ShouldSprint();
}