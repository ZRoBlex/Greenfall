// ============================================================
// WeaponRecoilAnimator.cs — VALORES SUAVIZADOS
// ============================================================
// El recoil visual del modelo del arma ahora tiene valores
// mucho más suaves por defecto. El kick es solo un pequeño
// retroceso, no una sacudida violenta.
//
// VALORES RECOMENDADOS POR TIPO DE ARMA:
//   Pistola:  kickBackAmount=0.015, kickUpAmount=0.004, rotKickX=-1.5
//   Rifle:    kickBackAmount=0.025, kickUpAmount=0.006, rotKickX=-2.0
//   Shotgun:  kickBackAmount=0.04,  kickUpAmount=0.01,  rotKickX=-3.5
//   Sniper:   kickBackAmount=0.05,  kickUpAmount=0.012, rotKickX=-4.0
//
// Si sigue sintiéndose fuerte, bajar kickBackAmount y rotKickX.
// ============================================================

using UnityEngine;

public class WeaponRecoilAnimator : MonoBehaviour
{
    [Header("Kick de posición")]
    [Tooltip("Cuánto retrocede el arma en Z. 0.02 = muy sutil, 0.06 = notorio.")]
    [SerializeField] float kickBackAmount = 0.02f;

    [Tooltip("Cuánto sube el arma en Y por disparo.")]
    [SerializeField] float kickUpAmount   = 0.005f;

    [Tooltip("Velocidad de aplicación del kick. No subir de 40.")]
    [SerializeField] float kickApplySpeed = 28f;

    [Tooltip("Velocidad de retorno a posición base.")]
    [SerializeField] float returnSpeed    = 10f;

    [Header("Kick de rotación")]
    [Tooltip("Giro en X (cañón sube). Negativo. -1 a -4 es rango normal.")]
    [SerializeField] float rotKickX       = -2.0f;

    [Tooltip("Oscilación lateral máxima en Y. 0.5 a 1.5 es sutil.")]
    [SerializeField] float rotKickY       = 0.8f;

    [Tooltip("Velocidad de retorno de la rotación.")]
    [SerializeField] float rotReturnSpeed = 12f;

    // ─────────────────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────────────────

    Vector3    _basePosLocal;
    Quaternion _baseRotLocal;
    bool       _baseInitialized;

    Vector3 _posTarget,  _posCurrent;
    Vector3 _rotTarget,  _rotCurrent;

    // ─────────────────────────────────────────────────────────
    // INIT
    // ─────────────────────────────────────────────────────────

    void OnEnable()
    {
        // Resetear targets al equipar. Base se cachea en el primer Update
        // para asegurar que WeaponInventory ya aplicó el offset.
        _baseInitialized = false;
        _posTarget = _posCurrent = Vector3.zero;
        _rotTarget = _rotCurrent = Vector3.zero;
    }

    // Cachear la posición base DESPUÉS de que WeaponInventory haya
    // aplicado ApplyWeaponOffset(). Se llama en el primer LateUpdate.
    void LateUpdate()
    {
        if (!_baseInitialized)
        {
            _basePosLocal    = transform.localPosition;
            _baseRotLocal    = transform.localRotation;
            _baseInitialized = true;
        }

        float dt = Time.deltaTime;

        // Posición: current persigue target, target vuelve a cero
        _posCurrent = Vector3.Lerp(_posCurrent, _posTarget,  kickApplySpeed * dt);
        _posTarget  = Vector3.Lerp(_posTarget,  Vector3.zero, returnSpeed   * dt);

        // Rotación
        _rotCurrent = Vector3.Lerp(_rotCurrent, _rotTarget,  kickApplySpeed  * dt);
        _rotTarget  = Vector3.Lerp(_rotTarget,  Vector3.zero, rotReturnSpeed * dt);

        transform.localPosition = _basePosLocal + _posCurrent;
        transform.localRotation = _baseRotLocal * Quaternion.Euler(_rotCurrent);
    }

    // ─────────────────────────────────────────────────────────
    // API
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Llamar desde Weapon.cs al disparar.
    /// weaponKick:   stats.recoilKick
    /// weaponRandom: stats.recoilRandom
    /// </summary>
    public void AddKick(float weaponKick, float weaponRandom)
    {
        if (!_baseInitialized) return;

        float rand = Random.Range(-weaponRandom, weaponRandom) * 0.5f;

        // Retroceso en Z y pequeña subida en Y
        // Se SUMA al target actual (disparos continuos acumulan un poco)
        _posTarget += new Vector3(
            0f,
            kickUpAmount   * weaponKick * 0.5f,
           -kickBackAmount * weaponKick);

        // Clampear para que no sea excesivo en full-auto
        _posTarget.z = Mathf.Clamp(_posTarget.z, -kickBackAmount * 3f, 0f);
        _posTarget.y = Mathf.Clamp(_posTarget.y,  0f, kickUpAmount * 3f);

        _rotTarget += new Vector3(
            rotKickX * weaponKick,
            rotKickY * rand,
            0f);

        // Clampear rotación
        _rotTarget.x = Mathf.Clamp(_rotTarget.x, rotKickX * 3f, 0f);
        _rotTarget.y = Mathf.Clamp(_rotTarget.y, -rotKickY * 2f, rotKickY * 2f);
    }

    /// <summary>Resetear inmediatamente (al cambiar de arma).</summary>
    public void ResetInstant()
    {
        _posTarget = _posCurrent = Vector3.zero;
        _rotTarget = _rotCurrent = Vector3.zero;
        if (_baseInitialized)
        {
            transform.localPosition = _basePosLocal;
            transform.localRotation = _baseRotLocal;
        }
    }
}
