// ============================================================
// WeaponAimController.cs — CONECTA ADS CON BOB Y SWAY
// ============================================================
// CAMBIOS RESPECTO A LA VERSIÓN ANTERIOR:
//
// 1. NOTIFICA A CameraBobController:
//    Al entrar/salir de ADS llama bob.SetAiming(bool).
//    El bob hace fade out suavemente mientras se apunta.
//
// 2. NOTIFICA A SwayController:
//    Al entrar/salir de ADS llama sway.SetAiming(bool).
//    El sway se suprime casi por completo en ADS.
//    Ya no se desactiva el componente (enabled=false) porque
//    eso causaba un salto brusco. Ahora es una transición suave.
//
// 3. AUTO-DETECTA BOB Y SWAY:
//    Busca en el parent del jugador. Si los tienes en otro GO
//    puedes asignarlos manualmente en el Inspector.
//
// NOTA: swayController ya no se usa como MonoBehaviour genérico.
// Ahora es un campo tipado SwayController. Si tu Inspector
// tenía el campo antiguo, reasígnalo.
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
    int _currentZoomIndex;

    [Header("Aim Modifiers")]
    [SerializeField] float moveMultiplier = 0.4f;

    [Header("Sniper")]
    [SerializeField] bool       useScopeUI                = false;
    [SerializeField] bool       hideWeaponModelWhenScoped = false;
    [SerializeField] GameObject weaponModel;

    [Header("Sensitivity By Zoom")]
    [SerializeField] bool  scaleSensitivityByFOV        = true;
    [SerializeField] float baseAimSensitivityMultiplier = 0.6f;

    [Header("Spread")]
    public  WeaponStats weaponStats;
    public  bool        overrideSpreadOnAim = true;

    [Header("Referencias (auto-detectadas si están vacías)")]
    [Tooltip("SwayController del arma. Si está vacío se busca en el mismo GO.")]
    [SerializeField] SwayController       swayController;

    [Tooltip("CameraBobController. Si está vacío se busca en la cámara.")]
    [SerializeField] CameraBobController  bobController;

    // ─────────────────────────────────────────────────────────
    // REFERENCIAS INTERNAS
    // ─────────────────────────────────────────────────────────

    public bool IsAiming => _isAiming;

    PlayerWeaponContext    _context;
    Camera                 _cam;
    CameraRecoilController _recoil;

    Vector3    _defaultPos;
    Quaternion _defaultRot;
    float      _defaultFOV;
    float      _initialSpread;
    bool       _isAiming;
    bool       _contextReady;

    // ─────────────────────────────────────────────────────────
    // INJECT CONTEXT
    // ─────────────────────────────────────────────────────────

    public void InjectContext(PlayerWeaponContext ctx)
    {
        _context = ctx;
        _cam     = ctx.playerCamera;

        if (weaponRoot == null)
            weaponRoot = transform.parent;

        // Auto-detectar recoil
        if (_cam != null)
            _recoil = _cam.GetComponentInParent<CameraRecoilController>();

        // Auto-detectar sway (mismo GO que el arma)
        if (swayController == null)
            swayController = GetComponentInParent<SwayController>();

        // Auto-detectar bob (en la cámara o su parent)
        if (bobController == null && _cam != null)
            bobController = _cam.GetComponentInParent<CameraBobController>();

        // FIX zoom: restaurar FOV antes de cachearlo
        if (_cam != null)
            _cam.fieldOfView = GetSafeFOV();

        CacheDefaults();

        if (weaponStats != null)
            _initialSpread = weaponStats.spreadAngle;

        _contextReady = true;
        ResetWeapon();
    }

    // Devuelve el FOV "real" (no reducido por zoom anterior)
    float GetSafeFOV()
    {
        if (_cam == null) return 60f;
        float cur = _cam.fieldOfView;
        foreach (float z in zoomLevels)
            if (Mathf.Abs(cur - z) < 1f)
                return _defaultFOV > 0f ? _defaultFOV : 60f;
        return cur;
    }

    void CacheDefaults()
    {
        if (weaponRoot)
        {
            _defaultPos = weaponRoot.localPosition;
            _defaultRot = weaponRoot.localRotation;
        }
        if (_cam) _defaultFOV = _cam.fieldOfView;
    }

    // ─────────────────────────────────────────────────────────
    // ON DISABLE — restaurar FOV al cambiar de arma
    // ─────────────────────────────────────────────────────────

    void OnDisable()
    {
        if (!_contextReady) return;
        ForceRestoreFOV();
        if (_isAiming) InternalStopAim();
    }

    // ─────────────────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────────────────

    void Update()
    {
        if (!_contextReady) return;
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
                if (!_isAiming) StartAim();
                else            CycleZoomOrStop();
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    // AIM ON / OFF
    // ─────────────────────────────────────────────────────────

    void StartAim()
    {
        _isAiming        = true;
        _currentZoomIndex = 0;

        _context.playerController.SetAimMoveMultiplier(moveMultiplier);
        _context.playerController.SetAimSensitivityMultiplier(GetSensMult());

        if (useScopeUI)                                 _context.scopeUI?.SetActive(true);
        if (hideWeaponModelWhenScoped && weaponModel)   weaponModel.SetActive(false);
        if (overrideSpreadOnAim && weaponStats != null) weaponStats.spreadAngle = 0f;

        // Notificar a los controllers de feeling
        _recoil?.SetAiming(true);
        swayController?.SetAiming(true);   // sway se suprime suavemente
        bobController?.SetAiming(true);    // bob desaparece suavemente
    }

    void StopAim()
    {
        InternalStopAim();
        ForceRestoreFOV();
    }

    void InternalStopAim()
    {
        _isAiming        = false;
        _currentZoomIndex = 0;

        _context?.playerController.ResetAimModifiers();
        if (useScopeUI)                                 _context?.scopeUI?.SetActive(false);
        if (weaponModel)                                weaponModel.SetActive(true);
        if (overrideSpreadOnAim && weaponStats != null) weaponStats.spreadAngle = _initialSpread;

        _recoil?.SetAiming(false);
        swayController?.SetAiming(false);
        bobController?.SetAiming(false);
    }

    void ForceRestoreFOV()
    {
        if (_cam != null && _defaultFOV > 0f)
            _cam.fieldOfView = _defaultFOV;
    }

    void CycleZoomOrStop()
    {
        if (zoomLevels.Count > 1 && _currentZoomIndex < zoomLevels.Count - 1)
            _currentZoomIndex++;
        else
            StopAim();
    }

    // ─────────────────────────────────────────────────────────
    // TRANSFORM / FOV
    // ─────────────────────────────────────────────────────────

    void UpdateAimTransform()
    {
        if (!weaponRoot) return;
        Vector3    pos = _isAiming ? aimLocalPosition                  : _defaultPos;
        Quaternion rot = _isAiming ? Quaternion.Euler(aimLocalRotation) : _defaultRot;

        weaponRoot.localPosition = Vector3.Lerp(
            weaponRoot.localPosition, pos, Time.deltaTime * aimSmoothSpeed);
        weaponRoot.localRotation = Quaternion.Slerp(
            weaponRoot.localRotation, rot, Time.deltaTime * aimSmoothSpeed);
    }

    void UpdateFOV()
    {
        if (!_cam) return;
        float target = _isAiming ? zoomLevels[_currentZoomIndex] : _defaultFOV;
        _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, target, Time.deltaTime * 8f);

        if (_isAiming && _context?.playerController != null)
            _context.playerController.SetAimSensitivityMultiplier(GetSensMult());
    }

    // ─────────────────────────────────────────────────────────
    // FORCE STOP / RESET
    // ─────────────────────────────────────────────────────────

    public void ForceStopAim()
    {
        if (!_contextReady) return;
        InternalStopAim();
        ResetWeapon();
    }

    void ResetWeapon()
    {
        _isAiming        = false;
        _currentZoomIndex = 0;

        if (weaponRoot)
        {
            weaponRoot.localPosition = _defaultPos;
            weaponRoot.localRotation = _defaultRot;
        }

        ForceRestoreFOV();
        _context?.playerController.ResetAimModifiers();
        _context?.scopeUI?.SetActive(false);
        if (weaponModel) weaponModel.SetActive(true);
        if (overrideSpreadOnAim && weaponStats != null)
            weaponStats.spreadAngle = _initialSpread;

        _recoil?.SetAiming(false);
        swayController?.SetAiming(false);
        bobController?.SetAiming(false);
    }

    float GetSensMult()
    {
        if (!scaleSensitivityByFOV || !_cam || _defaultFOV <= 0f)
            return baseAimSensitivityMultiplier;
        return baseAimSensitivityMultiplier * (_cam.fieldOfView / _defaultFOV);
    }
}
