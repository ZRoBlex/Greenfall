using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Speed")]
    [SerializeField] float walkSpeed = 3.0f;
    [SerializeField] float sprintMultiplier = 2.0f;

    [Header("Jump Parameters")]
    [SerializeField] float jumpForce = 5.0f;
    [SerializeField] float gravityMultiplier = 1.0f;

    [Header("Look Parameters")]
    [SerializeField] float mouseSensitivity = 0.1f;
    [SerializeField] float upDownLookRange = 80.0f;

    [Header("References")]
    [SerializeField] CharacterController characterController;
    [SerializeField] Camera mainCamera;
    [SerializeField] PlayerInputHandler playerInputHandler;
    [SerializeField] Animator playerAnimator;

    [Header("Sway")]
    [SerializeField] WeaponSwayBinder weaponSwayBinder;
    //[SerializeField] SwayController cameraVisualSway;
    [SerializeField] CameraBobController cameraVisualSway;

    [Header("Aim Modifiers")]
    [SerializeField] float aimMoveMultiplier = 0.4f;
    [SerializeField] float aimSensitivityMultiplier = 0.5f;

    [SerializeField] DynamicCrosshair crosshair;

    [Header("Crouch")]
    [SerializeField] float crouchHeight = 1.0f;
    [SerializeField] float standingHeight = 1.8f;
    [SerializeField] float crouchSpeedMultiplier = 0.5f;
    [SerializeField] float crouchTransitionSpeed = 8f;

    //[SerializeField] LayerMask ceilingMask;
    //[SerializeField] float ceilingCheckRadius = 0.25f;
    [Header("Crouch Check")]
    [SerializeField] LayerMask ceilingMask;
    [SerializeField] float ceilingRayOffset = 0.05f;


    bool isCrouching;
    bool wantsToStand;


    [Header("Camera Crouch")]
    [SerializeField] float cameraStandingHeight = 1.6f;
    [SerializeField] float cameraCrouchHeight = 1.0f;
    [SerializeField] float cameraCrouchSpeed = 8f;

    [Header("Default Crosshair")]
    [SerializeField] CrosshairProfile defaultCrosshairProfile;

    float cameraTargetHeight;


    float targetHeight;




    float currentMoveMultiplier = 1f;
    float currentSensitivityMultiplier = 1f;


    public void SetAimSensitivityMultiplier(float value)
    {
        currentSensitivityMultiplier = value;
    }

    public void SetAimMoveMultiplier(float value)
    {
        currentMoveMultiplier = value;
    }


    public void ResetAimModifiers()
    {
        currentMoveMultiplier = 1f;
        currentSensitivityMultiplier = 1f;
    }



    private Vector3 currentMovment;
    private float verticalRotation;
    private float currentSpeed =>
    walkSpeed *
    (isCrouching ? crouchSpeedMultiplier : 1f) *
    (playerInputHandler.SprintTrigger && !isCrouching ? sprintMultiplier : 1f) *
    currentMoveMultiplier;


    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        targetHeight = standingHeight;
        characterController.height = standingHeight;

        cameraTargetHeight = cameraStandingHeight;
        mainCamera.transform.localPosition = new Vector3(
            mainCamera.transform.localPosition.x,
            cameraStandingHeight,
            mainCamera.transform.localPosition.z
        );

        playerInputHandler.enabled = true;

        // 🔹 ASIGNAR CROSSHAIR POR DEFECTO SI NO HAY PROFILE
        if (DynamicCrosshair.Instance != null)
        {
            if (DynamicCrosshair.Instance.Profile == null && defaultCrosshairProfile != null)
            {
                DynamicCrosshair.Instance.SetProfile(defaultCrosshairProfile);
            }
        }
    }

    void Update()
    {

        HandleMovement();
        HandleRotation();
        //HandleCrouch();

        Vector2 moveInput = playerInputHandler.MovementInput;
        Vector2 lookInput = playerInputHandler.RotationInput;

        Vector2 move = playerInputHandler.MovementInput;
        Vector2 look = playerInputHandler.RotationInput;

        //cameraVisualSway?.SetMovementInput(move);
        //cameraVisualSway?.SetLookInput(look);
        cameraVisualSway?.SetMovementInput(playerInputHandler.MovementInput);
        cameraVisualSway?.SetSprint(playerInputHandler.SprintTrigger);


        weaponSwayBinder?.SetInputs(move, look);


        float movmentMagnitude = new Vector3(moveInput.x, 0, moveInput.y).magnitude;
        movmentMagnitude /= 2;
        if (playerInputHandler.SprintTrigger)
            movmentMagnitude *= 2;

        playerAnimator.SetFloat("Speed", movmentMagnitude);

        UpdateCrosshairMovement();

        Debug.DrawRay(
    transform.position + Vector3.up * ceilingRayOffset,
    Vector3.up * (standingHeight - ceilingRayOffset),
    CanStandUp() ? Color.green : Color.red
);


    }


    private Vector3 CalculateWorldDirection()
    {
        Vector3 inputDirection = new Vector3(playerInputHandler.MovementInput.x, 0, playerInputHandler.MovementInput.y);
        Vector3 worldDirection = transform.TransformDirection(inputDirection);
        return worldDirection.normalized;
    }

    private void HandleJumping()
    {
        if (characterController.isGrounded)
        {
            playerAnimator.SetBool("isGrounded", true);
            currentMovment.y = -0.5f;

            crosshair?.SetAirborne(false);

            if (playerInputHandler.JumpTrigger)
            {
                currentMovment.y = jumpForce;
            }
        }
        else
        {
            currentMovment.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
            playerAnimator.SetBool("isGrounded", false);

            crosshair?.SetAirborne(true);
        }
    }

    private void HandleMovement()
    {
        Vector3 worldDirection = CalculateWorldDirection();
        currentMovment.x = worldDirection.x * currentSpeed;
        currentMovment.z = worldDirection.z * currentSpeed;

        HandleJumping();
        HandleCrouch();
        characterController.Move(currentMovment * Time.deltaTime);
    }

    private void ApplyHorizontalRotation(float rotationAmmount)
    {
        transform.Rotate(0, rotationAmmount, 0);
    }

    private void ApplyVerticalRotation(float rotationAmmount)
    {
        verticalRotation = Mathf.Clamp(verticalRotation - rotationAmmount, -upDownLookRange, upDownLookRange);
        mainCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }

    private void HandleRotation()
    {
        float mouseXRotation =
    playerInputHandler.RotationInput.x *
    mouseSensitivity *
    currentSensitivityMultiplier;

        float mouseYRotation =
            playerInputHandler.RotationInput.y *
            mouseSensitivity *
            currentSensitivityMultiplier;


        ApplyHorizontalRotation(mouseXRotation);
        ApplyVerticalRotation(mouseYRotation);
    }

    public void SetAimModifiers(bool aiming)
    {
        if (isCrouching) return;

        currentMoveMultiplier = aiming ? aimMoveMultiplier : 1f;
        currentSensitivityMultiplier = aiming ? aimSensitivityMultiplier : 1f;
    }


    void UpdateCrosshairMovement()
    {
        if (crosshair == null) return;

        Vector2 moveInput = playerInputHandler.MovementInput;

        // Magnitud del input (0 quieto, 1 máximo)
        float inputMagnitude = Mathf.Clamp01(moveInput.magnitude);

        // Sprint aumenta spread
        if (playerInputHandler.SprintTrigger)
            inputMagnitude *= 1.5f;

        // Apuntar reduce spread
        inputMagnitude *= currentMoveMultiplier;

        crosshair.SetMovementSpread(inputMagnitude);
    }

    void HandleCrouch()
    {
        bool crouchInput = playerInputHandler.CrouchTrigger;

        if (crouchInput)
        {
            isCrouching = true;
            targetHeight = crouchHeight;
            cameraTargetHeight = cameraCrouchHeight;

            crosshair?.SetCrouch(true);
        }
        else
        {
            if (isCrouching && !CanStandUp())
            {
                // ❌ Hay techo → no puede levantarse
                isCrouching = true;
                targetHeight = crouchHeight;
                cameraTargetHeight = cameraCrouchHeight;
            }
            else
            {
                if (!CanStandUp())
                {
                    // ❌ Hay techo → mantenerse agachado
                    isCrouching = true;
                    targetHeight = crouchHeight;
                    cameraTargetHeight = cameraCrouchHeight;
                }
                else
                {
                    // ✅ Espacio libre
                    isCrouching = false;
                    targetHeight = standingHeight;
                    cameraTargetHeight = cameraStandingHeight;
                    crosshair?.SetCrouch(false);
                }
            }

        }

        // 🔽 Collider suave
        characterController.height = Mathf.Lerp(
            characterController.height,
            targetHeight,
            Time.deltaTime * crouchTransitionSpeed
        );

        characterController.center = new Vector3(
            0,
            characterController.height / 2f,
            0
        );

        // 🎥 Cámara sincronizada
        Vector3 camPos = mainCamera.transform.localPosition;
        camPos.y = Mathf.Lerp(
            camPos.y,
            cameraTargetHeight,
            Time.deltaTime * cameraCrouchSpeed
        );
        mainCamera.transform.localPosition = camPos;
    }


    bool CanStandUp()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * ceilingRayOffset;

        float rayLength = standingHeight - ceilingRayOffset;

        return !Physics.Raycast(
            rayOrigin,
            Vector3.up,
            rayLength,
            ceilingMask
        );
    }



}
