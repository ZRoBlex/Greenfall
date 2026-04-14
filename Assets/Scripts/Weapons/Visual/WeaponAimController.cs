// ============================================================
//  WeaponAimController.cs — v2  CORREGIDO
//  Greenfall: The Last Harvest
// ============================================================
//
//  QUÉ HACE ESTE SCRIPT:
//  Controla el apuntado del arma:
//   - Mueve el weaponRoot hacia la posición de apuntado (ADS)
//   - Hace zoom en la cámara (FOV reducido)
//   - Soporte para múltiples niveles de zoom (para francotirador)
//   - Modifica la sensibilidad del jugador mientras apunta
//
//  CORRECCIONES v2:
//
//  BUG 1 — "Al apuntar, el arma conserva la rotación torcida del sway"
//  Causa: StartAim() deshabilitaba SwayController pero la rotación del
//  arma quedaba congelada en baseRotation * currentSway. El lerp hacia
//  aimLocalRotation empezaba desde esa rotación sucia.
//  Fix: StartAim() ahora llama sway.ResetImmediately() ANTES de
//  deshabilitar el SwayController. El lerp arranca desde rotación limpia.
//
//  BUG 2 — "Al cambiar armas y volver, el arma recuerda la pos de apuntado"
//  Causa: OnDisable() llamaba InternalStopAim() que solo ponía isAiming=false
//  pero NUNCA reseteaba weaponRoot.localPosition. Cuando se volvía a equipar
//  el arma, InjectContext() llamaba CacheDefaults() que leía el weaponRoot
//  todavía en posición ADS y guardaba eso como "posición neutral". Los
//  defaults se corrompían permanentemente.
//  Fix: Se introduce _neutralPos / _neutralRot que se capturan UNA SOLA VEZ
//  al inicio (antes de cualquier modificación runtime). OnDisable() ahora
//  resetea weaponRoot a esos valores neutrales inmediatamente.
// ============================================================

using UnityEngine;
using System.Collections.Generic;

public class WeaponAimController : MonoBehaviour
{
    // ── Modo de apuntado ──────────────────────────────────────────────────
    [Header("Modo de apuntado")]
    [Tooltip("true = mantener presionado para apuntar / false = toggle con clic en los niveles de zoom.")]
    [SerializeField] bool holdToAim = true;

    // ── Posición ADS ──────────────────────────────────────────────────────
    [Header("Posición al apuntar (ADS)")]
    [Tooltip("El Transform raíz del arma que este controlador mueve. " +
             "Si no se asigna, se usa transform.parent automáticamente.")]
    [SerializeField] Transform weaponRoot;

    [Tooltip("Posición local del weaponRoot cuando se está apuntando.")]
    [SerializeField] Vector3 aimLocalPosition;

    [Tooltip("Rotación local (euler) del weaponRoot cuando se está apuntando.")]
    [SerializeField] Vector3 aimLocalRotation;

    [Tooltip("Velocidad del lerp entre posición idle y posición ADS. " +
             "10 = bastante rápido, 4 = lento y suave.")]
    [SerializeField] float aimSmoothSpeed = 10f;

    // ── Zoom de cámara ────────────────────────────────────────────────────
    [Header("Zoom de cámara")]
    [Tooltip("Lista de FOVs al apuntar. Para rifle normal: [40]. " +
             "Para francotirador con 2 niveles: [40, 20].")]
    [SerializeField] List<float> zoomLevels = new() { 40f, 20f };

    // Índice actual en zoomLevels (se avanza con clic si holdToAim=false)
    int _currentZoomIndex;

    // ── Modificadores mientras apunta ─────────────────────────────────────
    [Header("Modificadores al apuntar")]
    [Tooltip("Multiplicador de velocidad de movimiento mientras apunta. " +
             "0.4 = moverse al 40% de velocidad normal.")]
    [SerializeField] float moveMultiplier = 0.4f;

    // ── Modo francotirador ────────────────────────────────────────────────
    [Header("Modo francotirador (scope)")]
    [Tooltip("Si true, muestra una UI de mira óptica al apuntar.")]
    [SerializeField] bool useScopeUI = false;

    [Tooltip("Si true, oculta el modelo del arma cuando se muestra el scope (modo sniper).")]
    [SerializeField] bool hideWeaponModelWhenScoped = false;

    [Tooltip("El GameObject del modelo visual del arma (para ocultarlo con scope).")]
    [SerializeField] GameObject weaponModel;

    // ── Sensibilidad al apuntar ───────────────────────────────────────────
    [Header("Sensibilidad al apuntar")]
    [Tooltip("Si true, reduce la sensibilidad proporcionalmente al zoom (FOV actual / FOV base).")]
    [SerializeField] bool scaleSensitivityByFOV = true;

    [Tooltip("Multiplicador base de sensibilidad mientras apunta. " +
             "0.6 = 60% de la sensibilidad normal.")]
    [SerializeField] float baseAimSensitivityMultiplier = 0.6f;

    // ── Spread ────────────────────────────────────────────────────────────
    [Header("Spread al apuntar")]
    [Tooltip("Referencia al WeaponStats para reducir el spread al apuntar.")]
    public WeaponStats weaponStats;

    [Tooltip("Si true, pone el spreadAngle a 0 mientras apunta (más preciso).")]
    public bool overrideSpreadOnAim = true;

    // ── SwayController ────────────────────────────────────────────────────
    [Header("Sway")]
    [Tooltip("Referencia al SwayController del arma. " +
             "CORRECCIÓN: Ahora es SwayController directamente (no MonoBehaviour) " +
             "para poder llamar ResetImmediately() sin casteo.")]
    [SerializeField] SwayController swayController;

    // ─────────────────────────────────────────────────────────────────────
    //  ESTADO INTERNO
    // ─────────────────────────────────────────────────────────────────────

    // Referencia al contexto del jugador (cámara, controller, scopeUI, etc.)
    // Se inyecta por WeaponInventory al equipar el arma.
    PlayerWeaponContext _context;

    // Cámara del jugador
    Camera _cam;

    // Controlador de recoil de la cámara
    CameraRecoilController _recoil;

    // ── CORRECCIÓN BUG 2: Posición/rotación neutral capturada UNA SOLA VEZ ──
    //
    // _neutralPos y _neutralRot representan la posición y rotación del weaponRoot
    // cuando el arma está en su estado "en reposo" (sin apuntar, sin sway).
    // Se capturan en Awake() (si weaponRoot ya está asignado) o en el primer
    // InjectContext(). NUNCA se actualizan con valores runtime.
    //
    // Por qué es crítico: CacheDefaults() en la versión anterior leía de
    // weaponRoot.localPosition en cada InjectContext(). Si el arma se
    // desequipaba mientras apuntaba, la posición ADS se guardaba como "default".
    // Al volver a equiparla, el arma seguía en posición ADS para siempre.
    Vector3    _neutralPos;
    Quaternion _neutralRot;
    bool       _neutralCached; // Flag: ¿ya capturamos los neutrales?

    // FOV de la cámara cuando NO está apuntando (el FOV "real")
    float _defaultFOV;
    bool  _fovCached;

    // Spread original del arma (para restaurarlo al dejar de apuntar)
    float _initialSpread;

    // Estado actual
    bool _isAiming;
    bool _contextReady; // True después del primer InjectContext exitoso

    // ─────────────────────────────────────────────────────────────────────
    //  AWAKE — Captura neutral ANTES de cualquier modificación runtime
    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Si weaponRoot está asignado en el Inspector desde el prefab,
        // capturamos su posición neutral ahora — antes de que WeaponInventory
        // lo reasigne o lo mueva. Esta es la captura más temprana y más limpia.
        if (weaponRoot != null && !_neutralCached)
        {
            _neutralPos    = weaponRoot.localPosition;
            _neutralRot    = weaponRoot.localRotation;
            _neutralCached = true;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  INJECT CONTEXT — Llamado por WeaponInventory al equipar el arma
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// WeaponInventory llama este método cada vez que este arma es equipada.
    /// Recibe el contexto del jugador (cámara, controller, scopeUI).
    /// </summary>
    public void InjectContext(PlayerWeaponContext ctx)
    {
        _context = ctx;
        _cam     = ctx.playerCamera;

        // Si weaponRoot no fue asignado en el Inspector, usamos el padre
        if (weaponRoot == null)
            weaponRoot = transform.parent;

        // Buscamos el CameraRecoilController en la jerarquía de la cámara
        if (_cam != null)
            _recoil = _cam.GetComponentInParent<CameraRecoilController>();

        // ── CORRECCIÓN BUG 2: Captura neutral si no se hizo en Awake ───────
        // Esto cubre el caso donde weaponRoot se asigna dinámicamente o
        // si por alguna razón Awake no lo capturó.
        //
        // PRIMERO reseteamos el weaponRoot a neutral (si tenemos el dato).
        // LUEGO capturamos si aún no tenemos el dato.
        // Este orden garantiza que nunca cacheemos una posición ADS.
        if (!_neutralCached)
        {
            // Primera vez equipando: el weaponRoot debería estar en posición limpia
            _neutralPos    = weaponRoot.localPosition;
            _neutralRot    = weaponRoot.localRotation;
            _neutralCached = true;
        }
        else
        {
            // Volvemos a equipar el arma: reseteamos a neutral ANTES de hacer nada más
            // (Arregla el bug donde volver al arma lo dejaba en posición ADS)
            ApplyNeutralImmediate();
        }

        // ── FOV base (solo si no lo tenemos o si parece corrupto) ───────────
        //
        // Corrección del bug original de FOV: si el FOV actual de la cámara
        // es igual a uno de los niveles de zoom, significa que el arma anterior
        // se desequipó SIN restaurar el FOV. Usamos el último FOV guardado.
        if (_cam != null)
        {
            float currentFOV = _cam.fieldOfView;
            bool  isSafeToCache = true;

            // ¿El FOV actual coincide con un nivel de zoom del arma anterior?
            foreach (float zoomLevel in zoomLevels)
            {
                if (Mathf.Abs(currentFOV - zoomLevel) < 1f)
                {
                    isSafeToCache = false;
                    break;
                }
            }

            if (isSafeToCache || !_fovCached)
            {
                _defaultFOV = currentFOV;
                _fovCached  = true;
                // Forzamos que la cámara esté en FOV normal
                _cam.fieldOfView = _defaultFOV;
            }
            else if (_fovCached)
            {
                // El FOV parece corrupto, restauramos el guardado
                _cam.fieldOfView = _defaultFOV;
            }
        }

        // Guardamos el spread inicial para restaurarlo al dejar de apuntar
        if (weaponStats != null)
            _initialSpread = weaponStats.spreadAngle;

        // Estado listo
        _contextReady = true;

        // Aseguramos estado limpio de aim
        _isAiming        = false;
        _currentZoomIndex = 0;

        // Aseguramos que el scope está oculto y el modelo visible
        _context?.scopeUI?.SetActive(false);
        if (weaponModel != null) weaponModel.SetActive(true);
        _recoil?.SetAiming(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  ON DISABLE — CORRECCIÓN CLAVE del Bug 2
    // ─────────────────────────────────────────────────────────────────────
    private void OnDisable()
    {
        // OnDisable se llama cuando WeaponInventory desactiva este arma
        // (al cambiar de slot). Es el momento crítico que causaba el bug.

        if (!_contextReady) return;

        // PASO 1: Restauramos el FOV inmediatamente
        // (para que el siguiente arma lo cachee limpio)
        ForceRestoreFOV();

        // PASO 2: Reseteamos el weaponRoot a la posición neutral
        // CORRECCIÓN: La versión anterior NO hacía esto, dejando el
        // weaponRoot en posición ADS si el arma se cambiaba mientras apuntaba.
        ApplyNeutralImmediate();

        // PASO 3: Reseteamos el sway para que al re-habilitar el arma
        // no haya un "pop" visual
        if (swayController != null)
        {
            swayController.enabled = true;      // re-habilitamos si estaba off
            swayController.ResetImmediately(); // limpiamos el sway acumulado
        }

        // PASO 4: Limpiamos todos los estados
        _isAiming         = false;
        _currentZoomIndex = 0;

        // Restauramos modificadores del jugador (si el contexto sigue válido)
        _context?.playerController?.ResetAimModifiers();
        _context?.scopeUI?.SetActive(false);
        if (weaponModel != null) weaponModel.SetActive(true);
        if (weaponStats != null) weaponStats.spreadAngle = _initialSpread;
        _recoil?.SetAiming(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────────────────────────────────
    private void Update()
    {
        if (!_contextReady) return;
        HandleInput();
        UpdateAimTransform();
        UpdateFOV();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  INPUT
    // ─────────────────────────────────────────────────────────────────────
    private void HandleInput()
    {
        if (holdToAim)
        {
            // Hold to aim: mantener para apuntar, soltar para dejar de apuntar
            if (Input.GetMouseButtonDown(1)) StartAim();
            if (Input.GetMouseButtonUp(1))   StopAim();
        }
        else
        {
            // Toggle: primer clic = apuntar, clic adicional = siguiente zoom,
            // último zoom = volver a idle
            if (Input.GetMouseButtonDown(1))
            {
                if (!_isAiming) StartAim();
                else            CycleZoomOrStop();
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  INICIAR APUNTADO
    // ─────────────────────────────────────────────────────────────────────
    private void StartAim()
    {
        _isAiming         = true;
        _currentZoomIndex = 0;

        // ── CORRECCIÓN BUG 1: Reset del sway ANTES de deshabilitarlo ───────
        //
        // Sin este reset, el WeaponAimController intentaba hacer lerp desde
        // la rotación "sucia" del sway (baseRotation * currentSway) hacia la
        // posición de apuntado. El arma aparecía torcida o rotada incorrectamente.
        //
        // Con el reset, la rotación del arma vuelve a su base limpia
        // INSTANTÁNEAMENTE antes de que el lerp de aim empiece. El usuario
        // no percibe este reset porque ocurre en el mismo frame del click.
        if (swayController != null)
        {
            swayController.ResetImmediately(); // ← La corrección del Bug 1
            swayController.enabled = false;    // Ahora sí lo deshabilitamos
        }

        // Aplicamos modificadores al jugador
        _context.playerController.SetAimMoveMultiplier(moveMultiplier);
        _context.playerController.SetAimSensitivityMultiplier(GetSensitivityMultiplier());

        // UI y modelo
        if (useScopeUI) _context.scopeUI?.SetActive(true);
        if (hideWeaponModelWhenScoped && weaponModel != null)
            weaponModel.SetActive(false);

        // Reducimos el spread a 0 para máxima precisión al apuntar
        if (overrideSpreadOnAim && weaponStats != null)
            weaponStats.spreadAngle = 0f;

        _recoil?.SetAiming(true);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  DETENER APUNTADO
    // ─────────────────────────────────────────────────────────────────────
    private void StopAim()
    {
        _isAiming         = false;
        _currentZoomIndex = 0;

        // Restauramos modificadores del jugador
        _context?.playerController?.ResetAimModifiers();

        // UI y modelo
        _context?.scopeUI?.SetActive(false);
        if (weaponModel != null) weaponModel.SetActive(true);

        // Restauramos spread original
        if (overrideSpreadOnAim && weaponStats != null)
            weaponStats.spreadAngle = _initialSpread;

        _recoil?.SetAiming(false);

        // Re-habilitamos el sway (OnEnable del SwayController hará el reset)
        if (swayController != null)
            swayController.enabled = true;
    }

    // Avanza al siguiente nivel de zoom, o sale del modo aim si no hay más
    private void CycleZoomOrStop()
    {
        if (zoomLevels.Count > 1 && _currentZoomIndex < zoomLevels.Count - 1)
            _currentZoomIndex++;
        else
            StopAim();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  UPDATE: TRANSFORM Y FOV
    // ─────────────────────────────────────────────────────────────────────

    // Mueve el weaponRoot suavemente entre posición idle y posición ADS
    private void UpdateAimTransform()
    {
        if (!weaponRoot) return;

        // Target: si apuntando → posición ADS; si no → posición neutral
        Vector3    targetPos = _isAiming ? aimLocalPosition              : _neutralPos;
        Quaternion targetRot = _isAiming ? Quaternion.Euler(aimLocalRotation) : _neutralRot;

        weaponRoot.localPosition = Vector3.Lerp(
            weaponRoot.localPosition, targetPos,
            Time.deltaTime * aimSmoothSpeed);

        weaponRoot.localRotation = Quaternion.Slerp(
            weaponRoot.localRotation, targetRot,
            Time.deltaTime * aimSmoothSpeed);
    }

    // Interpola el FOV de la cámara hacia el nivel de zoom objetivo
    private void UpdateFOV()
    {
        if (!_cam || !_fovCached) return;

        float targetFOV = _isAiming ? zoomLevels[_currentZoomIndex] : _defaultFOV;
        _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFOV, Time.deltaTime * 8f);

        // Actualizamos la sensibilidad cada frame mientras apunta (porque
        // el FOV cambia gradualmente, la sensibilidad debe ir con él)
        if (_isAiming && _context?.playerController != null)
            _context.playerController.SetAimSensitivityMultiplier(GetSensitivityMultiplier());
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HELPERS INTERNOS
    // ─────────────────────────────────────────────────────────────────────

    // Aplica la posición/rotación neutral AL INSTANTE (sin lerp)
    // Para uso en OnDisable y InjectContext
    private void ApplyNeutralImmediate()
    {
        if (!weaponRoot || !_neutralCached) return;
        weaponRoot.localPosition = _neutralPos;
        weaponRoot.localRotation = _neutralRot;
    }

    // Restaura el FOV de la cámara al valor neutral (sin lerp)
    private void ForceRestoreFOV()
    {
        if (_cam != null && _fovCached && _defaultFOV > 0f)
            _cam.fieldOfView = _defaultFOV;
    }

    // Calcula el multiplicador de sensibilidad según el zoom actual
    private float GetSensitivityMultiplier()
    {
        if (!scaleSensitivityByFOV || !_cam || _defaultFOV <= 0f)
            return baseAimSensitivityMultiplier;

        // Proporción: si el FOV es la mitad del normal → sensibilidad a la mitad
        return baseAimSensitivityMultiplier * (_cam.fieldOfView / _defaultFOV);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  API PÚBLICA — Para uso desde WeaponInventory u otros sistemas
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fuerza la salida del modo aim y resetea TODO al estado neutral.
    /// Útil cuando WeaponInventory cambia de arma o en situaciones especiales.
    /// </summary>
    public void ForceStopAim()
    {
        if (!_contextReady) return;
        StopAim();
        ApplyNeutralImmediate();
    }

    /// <summary>
    /// Devuelve true si el arma está actualmente en modo aim.
    /// </summary>
    public bool IsAiming => _isAiming;
}