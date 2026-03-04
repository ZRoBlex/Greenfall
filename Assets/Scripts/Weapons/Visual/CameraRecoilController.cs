// ============================================================
// CameraRecoilController.cs — RECOIL PROCEDURAL
// ============================================================
// SISTEMA DE RETROCESO PROCEDURAL:
//
// Cada disparo empuja la cámara en una DIRECCIÓN ALEATORIA
// usando ruido Perlin para que el movimiento sea orgánico y
// fluido, no entrecortado.
//
// TRES CAPAS:
//
//   1. KICK (impacto por disparo):
//      Rotación instantánea hacia arriba + lateral aleatorio.
//      Se aplica rápido, se recupera solo.
//      Simula el "golpe" de cada bala.
//
//   2. DRIFT (acumulación por ráfaga):
//      Con disparos continuos, la mira deriva gradualmente.
//      La dirección cambia con ruido Perlin = movimiento orgánico.
//      Al soltar el gatillo, vuelve suavemente al centro.
//
//   3. ROLL (inclinación en Z):
//      Pequeño giro lateral por impacto. Muy sutil.
//
// VALORES POR DEFECTO = suave y controlable.
// Cada arma puede multiplicar con sus propios recoilKick /
// recoilHorizontal en WeaponStats.
//
// CÓMO CONFIGURAR EN INSPECTOR:
//   kickStrength    → 0.5–2.0   (suave a fuerte)
//   driftStrength   → 0.3–1.5   (cuánto deriva en ráfaga)
//   maxDrift        → 4–10      (límite de subida)
//   driftRecovery   → 3–8       (qué tan rápido baja)
//   rollStrength    → 0–1       (inclinación lateral, 0 = off)
// ============================================================

using UnityEngine;

public class CameraRecoilController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // CONFIGURACIÓN
    // ─────────────────────────────────────────────────────────

    [Header("Kick — golpe por disparo")]
    [Tooltip("Fuerza del impulso vertical por disparo. 1.0 = valor base del arma.")]
    [SerializeField] float kickStrength     = 1.0f;

    [Tooltip("Velocidad con que se aplica el kick (alto = más snap).")]
    [SerializeField] float kickApplySpeed   = 35f;

    [Tooltip("Velocidad de retorno del kick.")]
    [SerializeField] float kickReturnSpeed  = 16f;

    [Header("Drift — movimiento procedural en ráfaga")]
    [Tooltip("Cuánto derive la mira con disparos continuos.")]
    [SerializeField] float driftStrength    = 0.6f;

    [Tooltip("Velocidad del ruido Perlin (velocidad del drift orgánico).")]
    [SerializeField] float noiseSpeed       = 1.8f;

    [Tooltip("Límite máximo de drift acumulado en grados.")]
    [SerializeField] float maxDrift         = 6f;

    [Tooltip("Velocidad de recuperación del drift al soltar el gatillo.")]
    [SerializeField] float driftRecovery    = 5f;

    [Header("Roll — inclinación lateral")]
    [Tooltip("Fuerza del giro en Z. 0 = desactivado.")]
    [SerializeField] float rollStrength     = 0.5f;

    [Tooltip("Velocidad de retorno del roll.")]
    [SerializeField] float rollReturnSpeed  = 12f;

    [Header("ADS — reducción al apuntar")]
    [Tooltip("Multiplicador al apuntar. 0.3 = 30% del recoil normal.")]
    [SerializeField] float adsMultiplier    = 0.3f;

    // ─────────────────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────────────────

    // Kick: target sube al disparar, current lo persigue
    float _kickXTarget,  _kickXCurrent;
    float _kickYTarget,  _kickYCurrent;

    // Drift: posición orgánica acumulada
    float _driftX, _driftY;

    // Roll
    float _rollTarget, _rollCurrent;

    // Perlin noise offset (único por instancia para que varie)
    float _noiseOffsetX;
    float _noiseOffsetY;
    float _noiseTime;

    // Control de disparo
    float _lastShotTime;
    bool  _isFiring;
    bool  _isAiming;

    // ─────────────────────────────────────────────────────────
    // INIT
    // ─────────────────────────────────────────────────────────

    void Awake()
    {
        // Offsets únicos para que dos armas iguales no tengan
        // el mismo patrón de drift
        _noiseOffsetX = Random.Range(0f, 100f);
        _noiseOffsetY = Random.Range(0f, 100f);
    }

    // ─────────────────────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Llamar desde Weapon.cs cada vez que se dispara.
    /// vertical   → stats.cameraRecoilVertical
    /// horizontal → stats.cameraRecoilHorizontal
    /// </summary>
    public void AddRecoil(float vertical, float horizontal)
    {
        float mult = _isAiming ? adsMultiplier : 1f;

        // ── Kick ─────────────────────────────────────────────
        // Vertical: siempre hacia arriba (negativo = sube en Unity)
        _kickXTarget -= vertical * kickStrength * mult;

        // Horizontal: pequeño aleatorio izquierda/derecha
        // Usamos un valor suave, no totalmente aleatorio frame a frame
        float lateralKick = Random.Range(-horizontal, horizontal) * kickStrength * mult * 0.4f;
        _kickYTarget += lateralKick;

        // ── Drift acumulado ───────────────────────────────────
        // La dirección la dicta el ruido Perlin, no un random puro.
        // Esto hace que el drift "fluya" en lugar de saltar.
        float noiseX = (Mathf.PerlinNoise(_noiseTime + _noiseOffsetX, 0f) - 0.5f) * 2f;
        float noiseY = (Mathf.PerlinNoise(0f, _noiseTime + _noiseOffsetY) - 0.5f) * 2f;

        // Vertical siempre sube (con variación), horizontal es libre
        _driftX = Mathf.Clamp(
            _driftX + (-vertical * driftStrength * mult * 0.3f) + (noiseX * driftStrength * mult * 0.1f),
            -maxDrift, 0f);

        _driftY = Mathf.Clamp(
            _driftY + (noiseY * horizontal * driftStrength * mult * 0.15f),
            -maxDrift * 0.5f, maxDrift * 0.5f);

        // ── Roll ─────────────────────────────────────────────
        float rollDir = Random.Range(-1f, 1f);
        _rollTarget += rollDir * horizontal * rollStrength * mult * 0.3f;
        _rollTarget  = Mathf.Clamp(_rollTarget, -2f, 2f);

        _lastShotTime = Time.time;
        _isFiring     = true;
    }

    /// <summary>Llamar desde WeaponAimController al entrar/salir de ADS.</summary>
    public void SetAiming(bool aiming) => _isAiming = aiming;

    /// <summary>Limpia todo el recoil acumulado. Llamar al cambiar de arma.</summary>
    public void ResetRecoil()
    {
        _kickXTarget  = _kickXCurrent  = 0f;
        _kickYTarget  = _kickYCurrent  = 0f;
        _driftX       = _driftY        = 0f;
        _rollTarget   = _rollCurrent   = 0f;
        _noiseTime    = 0f;
        transform.localRotation = Quaternion.identity;
    }

    // ─────────────────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────────────────

    void Update()
    {
        float dt = Time.deltaTime;

        // Detectar si dejó de disparar (0.12s sin disparo = soltó el gatillo)
        if (_isFiring && Time.time - _lastShotTime > 0.12f)
            _isFiring = false;

        // Avanzar el tiempo del ruido (solo mientras dispara)
        if (_isFiring)
            _noiseTime += dt * noiseSpeed;

        // ── Kick ─────────────────────────────────────────────
        // Current persigue al target rápido
        _kickXCurrent = Mathf.Lerp(_kickXCurrent, _kickXTarget, kickApplySpeed  * dt);
        _kickYCurrent = Mathf.Lerp(_kickYCurrent, _kickYTarget, kickApplySpeed  * dt);
        // Target vuelve a cero (es el retorno del golpe)
        _kickXTarget  = Mathf.Lerp(_kickXTarget,  0f,           kickReturnSpeed * dt);
        _kickYTarget  = Mathf.Lerp(_kickYTarget,  0f,           kickReturnSpeed * dt);

        // ── Drift ─────────────────────────────────────────────
        if (!_isFiring)
        {
            // Recuperación suave al centro cuando suelta el gatillo
            _driftX = Mathf.Lerp(_driftX, 0f, driftRecovery * dt);
            _driftY = Mathf.Lerp(_driftY, 0f, driftRecovery * dt);
        }

        // ── Roll ─────────────────────────────────────────────
        _rollCurrent = Mathf.Lerp(_rollCurrent, _rollTarget,  rollReturnSpeed * dt);
        _rollTarget  = Mathf.Lerp(_rollTarget,  0f,           rollReturnSpeed * dt);

        // ── Aplicar a la cámara ───────────────────────────────
        float totalX = _kickXCurrent + _driftX;
        float totalY = _kickYCurrent + _driftY;
        float totalZ = _rollCurrent;

        transform.localRotation = Quaternion.Euler(totalX, totalY, totalZ);
    }
}
