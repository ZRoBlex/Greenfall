// ============================================================
// CameraBobController.cs — CON SUPRESIÓN EN ADS Y TILT DE SPRINT
// ============================================================
// CAMBIOS RESPECTO A TU VERSIÓN:
//
// 1. SUPRESIÓN EN ADS:
//    Cuando el jugador apunta, el bob se reduce gradualmente
//    a cero. Al dejar de apuntar, vuelve suavemente.
//    Llama SetAiming(true/false) desde WeaponAimController.
//
// 2. TILT DE SPRINT (inclinación en Z):
//    Al correr, la cámara se inclina levemente hacia el lado
//    del movimiento lateral. Da sensación de velocidad e inercia.
//    Configurable con sprintTiltAmount y sprintTiltSpeed.
//
// 3. BOB EN SALTO (fade out al aire):
//    Si pasas un bool isGrounded, el bob hace fade out en el
//    aire para no verse raro al saltar.
//    Llamar SetGrounded(bool) desde el PlayerController.
//
// INTEGRACIÓN:
//    Desde PlayerController.Update():
//      bob.SetMovementInput(moveInput);
//      bob.SetSprint(isSprinting);
//      bob.SetGrounded(isGrounded);
//    Desde WeaponAimController.StartAim() / StopAim():
//      bob.SetAiming(true / false);
// ============================================================

using UnityEngine;

public class CameraBobController : MonoBehaviour
{
    [Header("General")]
    [SerializeField] bool enableBob = true;

    [Header("Idle")]
    [SerializeField] float idleAmplitude = 0.008f;
    [SerializeField] float idleSpeed     = 0.6f;

    [Header("Walk")]
    [SerializeField] float walkAmplitude = 0.04f;
    [SerializeField] float walkSpeed     = 1.4f;

    [Header("Sprint")]
    [SerializeField] float sprintMultiplier = 1.5f;

    [Tooltip("Inclinación en Z al correr. 0 = desactivado.")]
    [SerializeField] float sprintTiltAmount = 2.5f;
    [SerializeField] float sprintTiltSpeed  = 6f;

    [Header("ADS — suprimir bob al apuntar")]
    [Tooltip("Qué tan rápido desaparece el bob al apuntar.")]
    [SerializeField] float adsSuppressSpeed = 8f;

    [Header("Smoothing")]
    [SerializeField] float smoothSpeed = 9f;

    // ─────────────────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────────────────

    Vector2 _movementInput;
    bool    _isSprinting;
    bool    _isAiming;
    bool    _isGrounded = true;

    Vector3 _defaultLocalPos;
    Quaternion _defaultLocalRot;
    float   _noiseTime;

    // Multiplicador de intensidad actual (se reduce en ADS)
    float _intensityMult = 1f;

    // Tilt actual en Z
    float _currentTilt;
    float _targetTilt;

    void Awake()
    {
        _defaultLocalPos = transform.localPosition;
        _defaultLocalRot = transform.localRotation;
        _noiseTime       = Random.Range(0f, 100f);
    }

    void Update()
    {
        UpdateIntensityMult();
        UpdateTilt();

        if (!enableBob || _intensityMult < 0.01f)
        {
            // Volver suavemente a la posición base cuando el bob está suprimido
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                _defaultLocalPos,
                Time.deltaTime * smoothSpeed);
            ApplyTilt();
            return;
        }

        ApplyBob();
        ApplyTilt();
    }

    // ─────────────────────────────────────────────────────────
    // BOB
    // ─────────────────────────────────────────────────────────

    void ApplyBob()
    {
        float intensity;
        float speed;

        bool isMoving = _movementInput.sqrMagnitude > 0.01f;

        if (!isMoving)
        {
            intensity = idleAmplitude;
            speed     = idleSpeed;
        }
        else
        {
            intensity = walkAmplitude;
            speed     = walkSpeed;
            if (_isSprinting) intensity *= sprintMultiplier;
        }

        // El bob se suprime al no estar en el suelo
        if (!_isGrounded) intensity *= 0.1f;

        intensity *= _intensityMult;

        _noiseTime += Time.deltaTime * speed;

        float x = (Mathf.PerlinNoise(_noiseTime,        0f) - 0.5f) * intensity;
        float y = (Mathf.PerlinNoise(0f, _noiseTime         ) - 0.5f) * intensity * 1.5f;

        Vector3 target = _defaultLocalPos + new Vector3(x, y, 0f);

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            target,
            Time.deltaTime * smoothSpeed);
    }

    // ─────────────────────────────────────────────────────────
    // TILT DE SPRINT
    // ─────────────────────────────────────────────────────────

    void UpdateTilt()
    {
        // Tilt basado en el input lateral (x) mientras corre
        if (_isSprinting && !_isAiming)
            _targetTilt = -_movementInput.x * sprintTiltAmount;
        else
            _targetTilt = 0f;

        _currentTilt = Mathf.Lerp(_currentTilt, _targetTilt, Time.deltaTime * sprintTiltSpeed);
    }

    void ApplyTilt()
    {
        // Aplicar el tilt en Z sobre la rotación base
        Quaternion tilt    = Quaternion.Euler(0f, 0f, _currentTilt);
        transform.localRotation = Quaternion.Lerp(
            transform.localRotation,
            _defaultLocalRot * tilt,
            Time.deltaTime * sprintTiltSpeed);
    }

    // ─────────────────────────────────────────────────────────
    // INTENSIDAD (supresión en ADS)
    // ─────────────────────────────────────────────────────────

    void UpdateIntensityMult()
    {
        float target = _isAiming ? 0f : 1f;
        _intensityMult = Mathf.Lerp(_intensityMult, target, Time.deltaTime * adsSuppressSpeed);
    }

    // ─────────────────────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────────────────────

    public void SetMovementInput(Vector2 input)   => _movementInput = input;
    public void SetSprint(bool sprint)             => _isSprinting   = sprint;
    public void SetAiming(bool aiming)             => _isAiming      = aiming;
    public void SetGrounded(bool grounded)         => _isGrounded    = grounded;
}
