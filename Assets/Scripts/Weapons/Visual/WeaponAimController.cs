using UnityEngine;
using System.Collections.Generic;

public class WeaponAimController : MonoBehaviour
{
    [Header("Aim Mode")]
    [SerializeField] bool holdToAim = true;

    [Header("Position Aim")]
    [SerializeField] Transform weaponRoot;
    [SerializeField] Vector3 aimLocalPosition;
    [SerializeField] Vector3 aimLocalRotation;
    [SerializeField] float aimSmoothSpeed = 10f;

    [Header("Camera Zoom Levels")]
    [SerializeField] List<float> zoomLevels = new() { 40f, 20f };
    int currentZoomIndex = 0;

    [Header("Aim Modifiers")]
    [SerializeField] float moveMultiplier = 0.4f;
    [SerializeField] float sensitivityMultiplier = 0.5f;

    [Header("Sniper")]
    [SerializeField] bool useScopeUI = false;
    [SerializeField] bool hideWeaponModelWhenScoped = false;
    [SerializeField] GameObject weaponModel;

    PlayerWeaponContext context;
    Camera cam;

    Vector3 defaultPos;
    Quaternion defaultRot;
    float defaultFOV;

    bool isAiming;

    // =========================
    public void InjectContext(PlayerWeaponContext ctx)
    {
        context = ctx;
        cam = ctx.playerCamera;

        CacheDefaults();
        ResetWeapon();
    }

    void CacheDefaults()
    {
        defaultPos = weaponRoot.localPosition;
        defaultRot = weaponRoot.localRotation;
        defaultFOV = cam.fieldOfView;
    }

    // =========================
    void Update()
    {
        HandleInput();
        UpdateAimTransform();
        UpdateFOV();
    }

    // =========================
    void HandleInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (!isAiming)
                StartAim();
            else if (!holdToAim)
                CycleZoom();
        }

        if (holdToAim && Input.GetMouseButtonUp(1))
            StopAim();
    }

    // =========================
    void StartAim()
    {
        isAiming = true;
        currentZoomIndex = 0;

        context.playerController.SetAimModifiers(true);

        if (useScopeUI)
            context.scopeUI?.SetActive(true);

        if (hideWeaponModelWhenScoped && weaponModel)
            weaponModel.SetActive(false);
    }

    void StopAim()
    {
        isAiming = false;
        currentZoomIndex = 0;

        context.playerController.SetAimModifiers(false);

        if (useScopeUI)
            context.scopeUI?.SetActive(false);

        if (weaponModel)
            weaponModel.SetActive(true);
    }

    void CycleZoom()
    {
        if (zoomLevels.Count <= 1) return;
        currentZoomIndex = (currentZoomIndex + 1) % zoomLevels.Count;
    }

    // =========================
    void UpdateAimTransform()
    {
        Vector3 pos = isAiming ? aimLocalPosition : defaultPos;
        Quaternion rot = isAiming
            ? Quaternion.Euler(aimLocalRotation)
            : defaultRot;

        weaponRoot.localPosition =
            Vector3.Lerp(weaponRoot.localPosition, pos, Time.deltaTime * aimSmoothSpeed);

        weaponRoot.localRotation =
            Quaternion.Slerp(weaponRoot.localRotation, rot, Time.deltaTime * aimSmoothSpeed);
    }

    void UpdateFOV()
    {
        float target =
            isAiming ? zoomLevels[currentZoomIndex] : defaultFOV;

        cam.fieldOfView =
            Mathf.Lerp(cam.fieldOfView, target, Time.deltaTime * 8f);
    }

    // =========================
    public void ForceStopAim()
    {
        StopAim();
        ResetWeapon();
    }

    void ResetWeapon()
    {
        isAiming = false;
        currentZoomIndex = 0;

        weaponRoot.localPosition = defaultPos;
        weaponRoot.localRotation = defaultRot;
        cam.fieldOfView = defaultFOV;

        context?.playerController.SetAimModifiers(false);

        if (useScopeUI)
            context?.scopeUI?.SetActive(false);

        if (weaponModel)
            weaponModel.SetActive(true);
    }
}
