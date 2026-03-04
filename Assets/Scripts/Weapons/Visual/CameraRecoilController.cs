// ============================================================
// CameraRecoilController.cs — RECOIL REALISTA EN DOS CAPAS
// ============================================================
//
// CAPA 1 — KICK (golpe por disparo):
//   Cada bala empuja la cámara hacia arriba + lateral.
//   Se aplica casi instantáneo. El jugador lo siente como
//   el "punch" del disparo.
//
// CAPA 2 — DRIFT (subida acumulada):
//   Con disparos continuos, la mira sube progresivamente.
//   Simula la fuerza continua del retroceso.
//   Al soltar el gatillo, baja suavemente al origen.
//
// CAPA 3 — ROLL (giro en Z):
//   Pequeña inclinación lateral por cada disparo.
//   Da sensación de impacto físico real.
//
// RESULTADO POR TIPO DE ARMA:
//   Pistola/semi: cada disparo se siente limpio e independiente.
//   Full-auto: primer disparo limpio, luego la mira sube.
//   Sniper: kick alto pero drift mínimo (un disparo, no ráfagas).
//   ADS: todo reducido al porcentaje configurado por arma.
//
// DÓNDE COLOCAR ESTE COMPONENTE:
//   En el GameObject padre de la cámara que rota con el jugador.
//   El mismo que Weapon.cs ya busca con GetComponentInParent.
//
// CÓMO LLAMARLO:
//   Desde Weapon.cs al disparar:    cameraRecoil.AddRecoil(v, h)
//   Desde WeaponAimController.cs:   cameraRecoil.SetAiming(true/false)
// ============================================================

using UnityEngine;

public class CameraRecoilController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // CONFIGURACIÓN GLOBAL (cada arma puede modificar los
    // valores de WeaponStats para mayor o menor retroceso)
    // ─────────────────────────────────────────────────────────

    [Header("Kick — golpe por disparo")]
    [Tooltip("Velocidad con que se aplica el kick. Más alto = más violento e inmediato.")]
    [SerializeField] float kickApplySpeed    = 40f;

    [Tooltip("Velocidad con que el kick vuelve a cero tras el disparo.")]
    [SerializeField] float kickReturnSpeed   = 20f;

    [Header("Drift — subida por disparos continuos")]
    [Tooltip("Cuánto drift acumula cada disparo (se multiplica por el vertical del arma).")]
    [SerializeField] float driftPerShot      = 0.35f;

    [Tooltip("Velocidad de recuperación del drift al soltar el gatillo.")]
    [SerializeField] float driftRecovery     = 5f;

    [Tooltip("Límite máximo de drift acumulado en grados.")]
    [SerializeField] float maxDrift          = 10f;

    [Header("Roll — inclinación lateral")]
    [Tooltip("Magnitud del roll por disparo. Sensación de impacto físico.")]
    [SerializeField] float rollAmount        = 1.2f;

    [Tooltip("Velocidad de retorno del roll.")]
    [SerializeField] float rollReturnSpeed   = 14f;

    [Header("ADS — apuntando")]
    [Tooltip("Multiplicador de todo el recoil cuando se apunta. 0.3 = 30% del normal.")]
    [SerializeField] float adsMultiplier     = 0.3f;

    // ─────────────────────────────────────────────────────────
    // ESTADO INTERNO (no tocar en Inspector)
    // ─────────────────────────────────────────────────────────

    // Kick: target sube al disparar, current lo persigue, target baja solo
    Vector3 _kickTarget;
    Vector3 _kickCurrent;

    // Drift: sube con cada disparo, baja al soltar
    float   _driftX;     // vertical acumulado
    float   _driftY;     // lateral acumulado (menor)

    // Roll
    float   _rollTarget;
    float   _rollCurrent;

    // ¿Apuntando?
    bool    _isAiming;

    // Para detectar cuándo dejó de disparar
    float   _lastShotTime;

    // ─────────────────────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Llamar desde Weapon.cs cada vez que se dispara.
    /// vertical: parámetro recoilKick del WeaponStats.
    /// horizontal: parámetro recoilHorizontal del WeaponStats.
    /// </summary>
    public void AddRecoil(float vertical, float horizontal)
    {
        float mult = _isAiming ? adsMultiplier : 1f;

        // Kick instantáneo: empuja hacia arriba + lateral aleatorio
        // Negativo en X porque en Unity rotar hacia arriba = -X
        _kickTarget.x -= vertical   * mult;
        _kickTarget.y += horizontal * Random.Range(-1f, 1f) * mult;

        // Drift: acumula con cada disparo
        // Vertical acumula más que horizontal
        _driftX = Mathf.Clamp(_driftX - vertical * driftPerShot * mult,
                               -maxDrift, 0f);
        _driftY += horizontal * driftPerShot * 0.3f * mult;

        // Roll: pequeña inclinación lateral
        _rollTarget -= horizontal * rollAmount * mult;

        _lastShotTime = Time.time;
    }

    /// <summary>
    /// Llamar desde WeaponAimController cuando el jugador apunta o deja de apuntar.
    /// </summary>
    public void SetAiming(bool aiming)
    {
        _isAiming = aiming;
    }

    /// <summary>
    /// Llama esto al cambiar de arma para limpiar el estado acumulado.
    /// </summary>
    public void ResetRecoil()
    {
        _kickTarget   = Vector3.zero;
        _kickCurrent  = Vector3.zero;
        _driftX       = 0f;
        _driftY       = 0f;
        _rollTarget   = 0f;
        _rollCurrent  = 0f;
        transform.localRotation = Quaternion.identity;
    }

    // ─────────────────────────────────────────────────────────
    // UPDATE — aplicar todas las capas
    // ─────────────────────────────────────────────────────────

    void Update()
    {
        float dt = Time.deltaTime;

        // ── ¿Soltó el gatillo? ───────────────────────────────
        // Si pasaron más de 0.15s sin disparar, recuperar drift
        bool firing = Time.time - _lastShotTime < 0.15f;

        // ── KICK ─────────────────────────────────────────────
        // Current persigue al target muy rápido
        _kickCurrent = Vector3.Lerp(_kickCurrent, _kickTarget, kickApplySpeed * dt);
        // Target baja a cero más lento (es el retorno del golpe)
        _kickTarget  = Vector3.Lerp(_kickTarget,  Vector3.zero, kickReturnSpeed * dt);

        // ── DRIFT ─────────────────────────────────────────────
        if (!firing)
        {
            // Recuperación: vuelve a cero cuando suelta el gatillo
            _driftX = Mathf.Lerp(_driftX, 0f, driftRecovery * dt);
            _driftY = Mathf.Lerp(_driftY, 0f, driftRecovery * dt);
        }

        // ── ROLL ──────────────────────────────────────────────
        _rollCurrent = Mathf.Lerp(_rollCurrent, _rollTarget,  rollReturnSpeed * dt);
        _rollTarget  = Mathf.Lerp(_rollTarget,  0f,           rollReturnSpeed * dt);

        // ── APLICAR A LA CÁMARA ───────────────────────────────
        // La rotación final = kick (golpe inmediato) + drift (acumulado)
        float totalX = _kickCurrent.x + _driftX;
        float totalY = _kickCurrent.y + _driftY;
        float totalZ = _rollCurrent;

        transform.localRotation = Quaternion.Euler(totalX, totalY, totalZ);
    }
}
