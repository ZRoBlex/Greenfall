// ============================================================
// WeaponAimController.cs — FIX: zoom se quedaba al cambiar arma
// ============================================================
// BUG CORREGIDO:
//   defaultFOV se cacheaba en InjectContext() con el FOV actual
//   de la cámara. Si el jugador estaba apuntando (FOV reducido)
//   cuando cambiaba de arma, el nuevo arma cacheaba el FOV
//   reducido como su "normal". Resultado: zoom permanente.
//
//   FIX: ForceStopAim() ahora restaura el FOV ANTES de que
//   InjectContext() del nuevo arma lo lea. Además se agrega
//   OnDisable() que hace lo mismo si el GO se desactiva.
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

    [Header("Camera Zoom")]
    [SerializeField] List<float> zoomLevels = new() { 40f, 20f };
    int currentZoomIndex;

    [Header("Aim Modifiers")]
    [SerializeField] float moveMultiplier      = 0.4f;

    [Header("Sniper")]
    [SerializeField] bool       useScopeUI                = false;
    [SerializeField] bool       hideWeaponModelWhenScoped = false;
    [SerializeField] GameObject weaponModel;

    [Header("Sensitivity By Zoom")]
    [SerializeField] bool  scaleSensitivityByFOV        = true;
    [SerializeField] float baseAimSensitivityMultiplier = 0.6f;

    [Header("Spread")]
    public WeaponStats weaponStats;
    public bool overrideSpreadOnAim = true;

    [SerializeField] MonoBehaviour swayController;

    public bool IsAiming => isAiming;

    PlayerWeaponContext    context;
    Camera                 cam;
    CameraRecoilController recoil;

    Vector3    defaultPos;
    Quaternion defaultRot;
    float      defaultFOV;
    float      initialSpread;
    bool       isAiming;
    bool       contextReady;

    // ── InjectContext ──────────────────────────────────────────
    // Llamado por WeaponInventory.Equip() cada vez que se equipa
    // este arma.

    public void InjectContext(PlayerWeaponContext ctx)
    {
        context = ctx;
        cam     = ctx.playerCamera;

        if (weaponRoot == null)
            weaponRoot = transform.parent;

        if (cam != null)
            recoil = cam.GetComponentInParent<CameraRecoilController>();

        // FIX: cachear DESPUÉS de que la cámara esté en FOV normal.
        // Si el arma anterior no llamó ForceStopAim antes de desactivarse,
        // restauramos el FOV ahora para evitar que se cachee un valor reducido.
        if (cam != null)
            cam.fieldOfView = GetSafeFOV();

        CacheDefaults();

        if (weaponStats != null)
            initialSpread = weaponStats.spreadAngle;

        contextReady = true;
        ResetWeapon();
    }

    // Intenta leer el FOV "verdadero" desde la lista de zoom.
    // Si el FOV actual coincide con un nivel de zoom, usamos el
    // mayor (el FOV sin zoom). Si no, usamos el actual.
    float GetSafeFOV()
    {
        if (cam == null) return 60f;
        float current = cam.fieldOfView;

        // ¿El FOV actual es uno de los niveles de zoom?
        foreach (float z in zoomLevels)
            if (Mathf.Abs(current - z) < 1f)
                return defaultFOV > 0f ? defaultFOV : 60f; // usar el guardado o 60

        // No es un nivel de zoom, es el FOV real
        return current;
    }

    void CacheDefaults()
    {
        if (weaponRoot)
        {
            defaultPos = weaponRoot.localPosition;
            defaultRot = weaponRoot.localRotation;
        }
        if (cam) defaultFOV = cam.fieldOfView;
    }

    // ── OnDisable ──────────────────────────────────────────────
    // Se llama cuando WeaponInventory desactiva el GO del arma
    // al cambiar de arma (slots[old].gameObject.SetActive(false)).
    // Restauramos FOV aquí para que el próximo arma lo cachee limpio.

    void OnDisable()
    {
        if (!contextReady) return;
        ForceRestoreFOV();
        if (isAiming) InternalStopAim();
    }

    // ── Update ─────────────────────────────────────────────────

    void Update()
    {
        if (!contextReady) return;
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

    // ── Start / Stop Aim ───────────────────────────────────────

    void StartAim()
    {
        isAiming         = true;
        currentZoomIndex = 0;

        context.playerController.SetAimMoveMultiplier(moveMultiplier);
        context.playerController.SetAimSensitivityMultiplier(GetSensMult());

        if (swayController)                               swayController.enabled = false;
        if (useScopeUI)                                   context.scopeUI?.SetActive(true);
        if (hideWeaponModelWhenScoped && weaponModel)     weaponModel.SetActive(false);
        if (overrideSpreadOnAim && weaponStats != null)   weaponStats.spreadAngle = 0f;
        recoil?.SetAiming(true);
    }

    void StopAim()
    {
        InternalStopAim();
        ForceRestoreFOV();
    }

    // Lógica interna de stop (sin restaurar FOV directamente,
    // para que OnDisable también pueda llamarla)
    void InternalStopAim()
    {
        isAiming         = false;
        currentZoomIndex = 0;

        context?.playerController.ResetAimModifiers();
        if (swayController)                               swayController.enabled = true;
        if (useScopeUI)                                   context?.scopeUI?.SetActive(false);
        if (weaponModel)                                  weaponModel.SetActive(true);
        if (overrideSpreadOnAim && weaponStats != null)   weaponStats.spreadAngle = initialSpread;
        recoil?.SetAiming(false);
    }

    // Restaura el FOV al valor cacheado de forma inmediata (sin lerp)
    void ForceRestoreFOV()
    {
        if (cam != null && defaultFOV > 0f)
            cam.fieldOfView = defaultFOV;
    }

    void CycleZoomOrStop()
    {
        if (zoomLevels.Count > 1 && currentZoomIndex < zoomLevels.Count - 1)
            currentZoomIndex++;
        else
            StopAim();
    }

    // ── Transform / FOV ────────────────────────────────────────

    void UpdateAimTransform()
    {
        if (!weaponRoot) return;
        Vector3    pos = isAiming ? aimLocalPosition               : defaultPos;
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
            context.playerController.SetAimSensitivityMultiplier(GetSensMult());
    }

    // ── Force Stop / Reset ─────────────────────────────────────

    public void ForceStopAim()
    {
        if (!contextReady) return;
        InternalStopAim();
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

        ForceRestoreFOV();
        context?.playerController.ResetAimModifiers();
        context?.scopeUI?.SetActive(false);
        if (weaponModel) weaponModel.SetActive(true);
        if (overrideSpreadOnAim && weaponStats != null)
            weaponStats.spreadAngle = initialSpread;
        recoil?.SetAiming(false);
    }

    float GetSensMult()
    {
        if (!scaleSensitivityByFOV || !cam || defaultFOV <= 0f)
            return baseAimSensitivityMultiplier;
        return baseAimSensitivityMultiplier * (cam.fieldOfView / defaultFOV);
    }
}
