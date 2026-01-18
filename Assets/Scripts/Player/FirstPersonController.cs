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
    (playerInputHandler.SprintTrigger ? sprintMultiplier : 1f) *
    currentMoveMultiplier;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {

        HandleMovement();
        HandleRotation();

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

            if (playerInputHandler.JumpTrigger)
            {
                currentMovment.y = jumpForce;
            }
        }
        else
        {
            currentMovment.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
            playerAnimator.SetBool("isGrounded", false);
        }
    }

    private void HandleMovement()
    {
        Vector3 worldDirection = CalculateWorldDirection();
        currentMovment.x = worldDirection.x * currentSpeed;
        currentMovment.z = worldDirection.z * currentSpeed;

        HandleJumping();
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
        currentMoveMultiplier = aiming ? aimMoveMultiplier : 1f;
        currentSensitivityMultiplier = aiming ? aimSensitivityMultiplier : 1f;
    }


}
