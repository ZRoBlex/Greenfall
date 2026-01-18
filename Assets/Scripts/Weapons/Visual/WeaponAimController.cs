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

    [Header("Sensitivity By Zoom")]
    [SerializeField] bool scaleSensitivityByFOV = true;
    [SerializeField] float baseAimSensitivityMultiplier = 0.6f;

    [Header("SPREAD")]
    public WeaponStats weaponStats;
    public bool overrideSpreadOnAim = true;

    private float initialSpread;


    public bool IsAiming => isAiming;


    [SerializeField] MonoBehaviour swayController;



    PlayerWeaponContext context;
    Camera cam;

    Vector3 defaultPos;
    Quaternion defaultRot;
    float defaultFOV;

    bool isAiming;

    //private void Awake()
    //{
    //    initialSpread = weaponStats.spreadAngle;
    //}

    // =========================
    public void InjectContext(PlayerWeaponContext ctx)
    {
        context = ctx;
        cam = ctx.playerCamera;

        // 🔥 ASIGNAR WEAPON ROOT AUTOMÁTICAMENTE
        if (weaponRoot == null)
            weaponRoot = transform.parent; // weaponHolder

        // 🔥 CACHEAR VALORES REALES (YA EN INVENTARIO)
        CacheDefaults();

        // 🔥 CACHEAR SPREAD REAL
        if (weaponStats != null)
            initialSpread = weaponStats.spreadAngle;

        // 🔥 FORZAR ESTADO CORRECTO
        ResetWeapon();
    }


    void CacheDefaults()
    {
        if (!weaponRoot) return;

        defaultPos = weaponRoot.localPosition;
        defaultRot = weaponRoot.localRotation;

        if (cam)
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
        if (holdToAim)
        {
            if (Input.GetMouseButtonDown(1))
                StartAim();

            if (Input.GetMouseButtonUp(1))
                StopAim();
        }
        else
        {
            if (Input.GetMouseButtonDown(1))
            {
                if (!isAiming)
                    StartAim();
                else
                    CycleZoomOrStop();
            }
        }
    }


    // =========================
    void StartAim()
    {
        isAiming = true;
        currentZoomIndex = 0;

        context.playerController.SetAimMoveMultiplier(moveMultiplier);
        context.playerController.SetAimSensitivityMultiplier(
            GetCurrentSensitivityMultiplier()
        );

        if (swayController)
            swayController.enabled = false;

        if (useScopeUI)
            context.scopeUI?.SetActive(true);

        if (hideWeaponModelWhenScoped && weaponModel)
            weaponModel.SetActive(false);

        if(overrideSpreadOnAim)
            weaponStats.spreadAngle = 0;
    }

    void StopAim()
    {
        isAiming = false;
        currentZoomIndex = 0;

        context.playerController.ResetAimModifiers();

        if (swayController)
            swayController.enabled = true;

        if (useScopeUI)
            context.scopeUI?.SetActive(false);

        if (weaponModel)
            weaponModel.SetActive(true);
        weaponStats.spreadAngle = initialSpread;

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
        float target = isAiming
            ? zoomLevels[currentZoomIndex]
            : defaultFOV;

        cam.fieldOfView =
            Mathf.Lerp(cam.fieldOfView, target, Time.deltaTime * 8f);

        // 🔥 ACTUALIZAR SENSIBILIDAD DINÁMICAMENTE
        if (isAiming && context?.playerController)
        {
            context.playerController.SetAimSensitivityMultiplier(
                GetCurrentSensitivityMultiplier()
            );
        }
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

        if (weaponRoot)
        {
            weaponRoot.localPosition = defaultPos;
            weaponRoot.localRotation = defaultRot;
        }

        if (cam)
            cam.fieldOfView = defaultFOV;

        context?.playerController.ResetAimModifiers();

        if (useScopeUI)
            context?.scopeUI?.SetActive(false);

        if (weaponModel)
            weaponModel.SetActive(true);

        if (overrideSpreadOnAim && weaponStats != null)
            weaponStats.spreadAngle = initialSpread;
    }


    void CycleZoomOrStop()
    {
        if (zoomLevels.Count > 1 && currentZoomIndex < zoomLevels.Count - 1)
        {
            currentZoomIndex++;
        }
        else
        {
            StopAim();
        }
    }

    float GetCurrentSensitivityMultiplier()
    {
        if (!scaleSensitivityByFOV || !cam)
            return baseAimSensitivityMultiplier;

        float fovRatio = cam.fieldOfView / defaultFOV;
        return baseAimSensitivityMultiplier * fovRatio;
    }

}
