using UnityEngine;

public class WeaponAimController : MonoBehaviour
{
    [Header("Aim Mode")]
    [SerializeField] bool holdToAim = true;

    [Header("Aim Method")]
    [SerializeField] bool usePositionAim = true;
    [SerializeField] bool useAnimationAim = false;

    [Header("Position Aim")]
    [SerializeField] Transform weaponRoot;
    [SerializeField] Vector3 aimLocalPosition;
    [SerializeField] Vector3 aimLocalRotation;
    [SerializeField] float aimSmoothSpeed = 10f;

    [Header("Animation Aim")]
    [SerializeField] Animator animator;
    [SerializeField] string aimBoolName = "IsAiming";

    [Header("Camera FOV")]
    [SerializeField] bool changeFOV = true;
    [SerializeField] float aimedFOV = 40f;
    [SerializeField] float fovSmoothSpeed = 8f;

    [Header("Sniper / Scope")]
    [SerializeField] bool useScopeUI = false;
    [SerializeField] bool hideWeaponModelWhenScoped = false;
    [SerializeField] GameObject weaponModel;

    // -------------------------
    PlayerWeaponContext context;
    Camera targetCamera;

    Vector3 defaultLocalPos;
    Quaternion defaultLocalRot;
    float defaultFOV;

    bool isAiming;
    bool toggleState;

    public bool IsAiming => isAiming;

    // =========================
    // INJECTION
    // =========================
    public void InjectContext(PlayerWeaponContext ctx)
    {
        context = ctx;
        targetCamera = ctx.playerCamera;

        CacheDefaults();

        if (useScopeUI && context.scopeUI)
            context.scopeUI.SetActive(false);
    }

    void CacheDefaults()
    {
        if (weaponRoot)
        {
            defaultLocalPos = weaponRoot.localPosition;
            defaultLocalRot = weaponRoot.localRotation;
        }

        if (targetCamera)
            defaultFOV = targetCamera.fieldOfView;
    }

    // =========================
    void Update()
    {
        HandleInput();
        HandlePositionAim();
        HandleFOV();
    }

    // =========================
    void HandleInput()
    {
        if (holdToAim)
        {
            if (Input.GetMouseButtonDown(1))
                SetAim(true);

            if (Input.GetMouseButtonUp(1))
                SetAim(false);
        }
        else
        {
            if (Input.GetMouseButtonDown(1))
            {
                toggleState = !toggleState;
                SetAim(toggleState);
            }
        }
    }

    // =========================
    void SetAim(bool state)
    {
        isAiming = state;

        if (useAnimationAim && animator)
            animator.SetBool(aimBoolName, isAiming);

        if (useScopeUI && context && context.scopeUI)
            context.scopeUI.SetActive(isAiming);

        if (weaponModel && hideWeaponModelWhenScoped)
            weaponModel.SetActive(!isAiming);
    }

    // =========================
    void HandlePositionAim()
    {
        if (!usePositionAim || !weaponRoot) return;

        Vector3 targetPos = isAiming ? aimLocalPosition : defaultLocalPos;
        Quaternion targetRot = isAiming
            ? Quaternion.Euler(aimLocalRotation)
            : defaultLocalRot;

        weaponRoot.localPosition = Vector3.Lerp(
            weaponRoot.localPosition,
            targetPos,
            Time.deltaTime * aimSmoothSpeed
        );

        weaponRoot.localRotation = Quaternion.Slerp(
            weaponRoot.localRotation,
            targetRot,
            Time.deltaTime * aimSmoothSpeed
        );
    }

    // =========================
    void HandleFOV()
    {
        if (!changeFOV || !targetCamera) return;

        float target = isAiming ? aimedFOV : defaultFOV;

        targetCamera.fieldOfView = Mathf.Lerp(
            targetCamera.fieldOfView,
            target,
            Time.deltaTime * fovSmoothSpeed
        );
    }

    // =========================
    public void ForceStopAim()
    {
        toggleState = false;
        SetAim(false);
    }
}
