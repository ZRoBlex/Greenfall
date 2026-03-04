// ============================================================
// WeaponAimController.cs — NOTIFICA AL RECOIL CUANDO ADS CAMBIA
// ============================================================
// CAMBIO RESPECTO A TU VERSIÓN ORIGINAL:
//   Al entrar/salir de ADS, se notifica al CameraRecoilController
//   para que reduzca el recoil automáticamente.
//   El campo cameraRecoil se autodetecta desde la cámara.
//   Todo lo demás está intacto.
// ============================================================

using UnityEngine;
using System.Collections.Generic;

public class WeaponAimController : MonoBehaviour
{
    [Header("Aim Mode")]
    [SerializeField] bool holdToAim = true;

    [Header("Position Aim")]
    [SerializeField] Transform weaponRoot;
    [SerializeField] Vector3   aimLocalPosition;
    [SerializeField] Vector3   aimLocalRotation;
    [SerializeField] float     aimSmoothSpeed = 10f;

    [Header("Camera Zoom Levels")]
    [SerializeField] List<float> zoomLevels = new() { 40f, 20f };
    int currentZoomIndex = 0;

    [Header("Aim Modifiers")]
    [SerializeField] float moveMultiplier        = 0.4f;
    [SerializeField] float sensitivityMultiplier = 0.5f;

    [Header("Sniper")]
    [SerializeField] bool       useScopeUI                = false;
    [SerializeField] bool       hideWeaponModelWhenScoped = false;
    [SerializeField] GameObject weaponModel;

    [Header("Sensitivity By Zoom")]
    [SerializeField] bool  scaleSensitivityByFOV          = true;
    [SerializeField] float baseAimSensitivityMultiplier   = 0.6f;

    [Header("Spread")]
    public WeaponStats weaponStats;
    public bool overrideSpreadOnAim = true;

    [SerializeField] MonoBehaviour swayController;

    public bool IsAiming => isAiming;

    PlayerWeaponContext    context;
    Camera                 cam;
    CameraRecoilController recoil;  // NUEVO: para notificar ADS

    Vector3    defaultPos;
    Quaternion defaultRot;
    float      defaultFOV;
    float      initialSpread;
    bool       isAiming;

    // ─────────────────────────────────────────────────────────
    // INJECT CONTEXT
    // ─────────────────────────────────────────────────────────

    public void InjectContext(PlayerWeaponContext ctx)
    {
        context = ctx;
        cam     = ctx.playerCamera;

        if (weaponRoot == null)
            weaponRoot = transform.parent;

        // Autodetectar el recoil controller desde la cámara
        if (cam != null)
            recoil = cam.GetComponentInParent<CameraRecoilController>();

        CacheDefaults();

        if (weaponStats != null)
            initialSpread = weaponStats.spreadAngle;

        ResetWeapon();
    }

    void CacheDefaults()
    {
        if (!weaponRoot) return;
        defaultPos = weaponRoot.localPosition;
        defaultRot = weaponRoot.localRotation;
        if (cam) defaultFOV = cam.fieldOfView;
    }

    // ─────────────────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────────────────

    void Update()
    {
        HandleInput();
        UpdateAimTransform();
        UpdateFOV();
    }

    void HandleInput()
    {
        if (holdToAim)
        {
            if (Input.GetMouseButtonDown(1)) StartAim();
            if (Input.GetMouseButtonUp(1))   StopAim();
        }
        else
        {
            if (Input.GetMouseButtonDown(1))
            {
                if (!isAiming) StartAim();
                else           CycleZoomOrStop();
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    // START / STOP AIM
    // ─────────────────────────────────────────────────────────

    void StartAim()
    {
        isAiming         = true;
        currentZoomIndex = 0;

        context.playerController.SetAimMoveMultiplier(moveMultiplier);
        context.playerController.SetAimSensitivityMultiplier(GetCurrentSensitivityMultiplier());

        if (swayController)   swayController.enabled = false;
        if (useScopeUI)       context.scopeUI?.SetActive(true);
        if (hideWeaponModelWhenScoped && weaponModel) weaponModel.SetActive(false);
        if (overrideSpreadOnAim && weaponStats != null) weaponStats.spreadAngle = 0f;

        // NUEVO: avisar al recoil que estamos apuntando
        recoil?.SetAiming(true);
    }

    void StopAim()
    {
        isAiming         = false;
        currentZoomIndex = 0;

        context.playerController.ResetAimModifiers();

        if (swayController) swayController.enabled = true;
        if (useScopeUI)     context.scopeUI?.SetActive(false);
        if (weaponModel)    weaponModel.SetActive(true);
        if (weaponStats != null) weaponStats.spreadAngle = initialSpread;

        // NUEVO: avisar al recoil que ya no apuntamos
        recoil?.SetAiming(false);
    }

    void CycleZoomOrStop()
    {
        if (zoomLevels.Count > 1 && currentZoomIndex < zoomLevels.Count - 1)
            currentZoomIndex++;
        else
            StopAim();
    }

    // ─────────────────────────────────────────────────────────
    // TRANSFORM / FOV
    // ─────────────────────────────────────────────────────────

    void UpdateAimTransform()
    {
        if (!weaponRoot) return;

        Vector3    pos = isAiming ? aimLocalPosition         : defaultPos;
        Quaternion rot = isAiming ? Quaternion.Euler(aimLocalRotation) : defaultRot;

        weaponRoot.localPosition = Vector3.Lerp(
            weaponRoot.localPosition, pos, Time.deltaTime * aimSmoothSpeed);

        weaponRoot.localRotation = Quaternion.Slerp(
            weaponRoot.localRotation, rot, Time.deltaTime * aimSmoothSpeed);
    }

    void UpdateFOV()
    {
        if (!cam) return;

        float target = isAiming ? zoomLevels[currentZoomIndex] : defaultFOV;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, target, Time.deltaTime * 8f);

        if (isAiming && context?.playerController)
            context.playerController.SetAimSensitivityMultiplier(
                GetCurrentSensitivityMultiplier());
    }

    // ─────────────────────────────────────────────────────────
    // RESET / FORCE STOP
    // ─────────────────────────────────────────────────────────

    public void ForceStopAim()
    {
        StopAim();
        ResetWeapon();
    }

    void ResetWeapon()
    {
        isAiming         = false;
        currentZoomIndex = 0;

        if (weaponRoot)
        {
            weaponRoot.localPosition = defaultPos;
            weaponRoot.localRotation = defaultRot;
        }

        if (cam)    cam.fieldOfView = defaultFOV;

        context?.playerController.ResetAimModifiers();
        context?.scopeUI?.SetActive(false);
        if (weaponModel) weaponModel.SetActive(true);
        if (overrideSpreadOnAim && weaponStats != null)
            weaponStats.spreadAngle = initialSpread;

        recoil?.SetAiming(false);
    }

    float GetCurrentSensitivityMultiplier()
    {
        if (!scaleSensitivityByFOV || !cam) return baseAimSensitivityMultiplier;
        return baseAimSensitivityMultiplier * (cam.fieldOfView / defaultFOV);
    }
}
