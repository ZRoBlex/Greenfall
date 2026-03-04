// ============================================================
// SwayController.cs — SUPRESIÓN EN ADS Y DURANTE CAMBIO DE ARMA
// ============================================================
// CAMBIOS RESPECTO A TU VERSIÓN:
//
// 1. SUPRESIÓN EN ADS:
//    Al apuntar, el sway se reduce a casi cero de forma suave.
//    El arma se siente más "fija" y precisa.
//
// 2. SUPRESIÓN DURANTE SWITCH:
//    Cuando se está cambiando de arma, el sway se apaga
//    para que no interfiera con la animación de holster/draw.
//    Llamar SetSwitching(true/false) desde WeaponSwitchAnimator.
//
// 3. INTEGRACIÓN:
//    Desde WeaponAimController.StartAim(): sway.SetAiming(true)
//    Desde WeaponAimController.StopAim():  sway.SetAiming(false)
//    Desde WeaponSwitchAnimator:           sway.SetSwitching(bool)
// ============================================================

using UnityEngine;

public class SwayController : MonoBehaviour
{
    [Header("Enable States")]
    public bool enableIdleSway = true;
    public bool enableMoveSway = true;
    public bool enableLookSway = true;

    [Header("Idle Sway")]
    [SerializeField] Vector2 idleAmplitude = new Vector2(0.2f, 0.2f);
    [SerializeField] float   idleSpeed     = 1.5f;

    [Header("Move Sway")]
    [SerializeField] Vector2 moveAmplitude = new Vector2(0.6f, 0.6f);
    [SerializeField] float   moveSpeed     = 6f;

    [Header("Look Sway")]
    [SerializeField] float lookAmount = 1.5f;
    [SerializeField] float lookSmooth = 8f;

    [Header("ADS — suprimir sway al apuntar")]
    [Tooltip("Multiplicador de sway en ADS. 0.05 = casi nulo.")]
    [SerializeField] float adsMult          = 0.05f;
    [SerializeField] float adsSuppressSpeed = 10f;

    // ─────────────────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────────────────

    Quaternion _baseRotation;
    Vector2    _movementInput;
    Vector2    _lookInput;
    bool       _isMoving;

    float      _idleTimer;
    Quaternion _currentSway;

    // Multiplicadores de supresión
    float _intensityMult = 1f;   // baja en ADS y durante switch
    bool  _isAiming;
    bool  _isSwitching;

    void Awake()
    {
        _baseRotation = transform.localRotation;
    }

    void Update()
    {
        UpdateIntensityMult();

        Quaternion sway = Quaternion.identity;

        if (enableIdleSway && !_isMoving)
            sway *= GetIdleSway();

        if (enableMoveSway && _isMoving)
            sway *= GetMoveSway();

        if (enableLookSway)
            sway *= GetLookSway();

        // Aplicar multiplicador de supresión
        sway = Quaternion.Slerp(Quaternion.identity, sway, _intensityMult);

        _currentSway = Quaternion.Lerp(
            _currentSway,
            sway,
            Time.deltaTime * lookSmooth);

        transform.localRotation = _baseRotation * _currentSway;
    }

    // ─────────────────────────────────────────────────────────
    // SWAYS
    // ─────────────────────────────────────────────────────────

    Quaternion GetIdleSway()
    {
        _idleTimer += Time.deltaTime * idleSpeed;
        float x = Mathf.Sin(_idleTimer)         * idleAmplitude.x;
        float y = Mathf.Cos(_idleTimer * 0.8f)  * idleAmplitude.y;
        return Quaternion.Euler(x, y, 0f);
    }

    Quaternion GetMoveSway()
    {
        float x = -_movementInput.y * moveAmplitude.x;
        float y =  _movementInput.x * moveAmplitude.y;
        return Quaternion.Euler(x, y, 0f);
    }

    Quaternion GetLookSway()
    {
        return Quaternion.Euler(
            -_lookInput.y * lookAmount,
             _lookInput.x * lookAmount,
            0f);
    }

    // ─────────────────────────────────────────────────────────
    // INTENSIDAD
    // ─────────────────────────────────────────────────────────

    void UpdateIntensityMult()
    {
        // Si está apuntando o cambiando arma, reducir a casi cero
        float target = (_isAiming || _isSwitching) ? adsMult : 1f;
        _intensityMult = Mathf.Lerp(_intensityMult, target, Time.deltaTime * adsSuppressSpeed);
    }

    // ─────────────────────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────────────────────

    public void SetMovementInput(Vector2 input)
    {
        _movementInput = input;
        _isMoving      = input.sqrMagnitude > 0.01f;
    }

    public void SetLookInput(Vector2 input)  => _lookInput   = input;
    public void SetAiming(bool aiming)        => _isAiming    = aiming;

    /// <summary>
    /// Llamar desde WeaponSwitchAnimator al iniciar/terminar
    /// la animación de cambio de arma.
    /// </summary>
    public void SetSwitching(bool switching)  => _isSwitching = switching;
}
