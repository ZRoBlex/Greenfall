// ============================================================
//  SwayController.cs — v2  CORREGIDO
//  Greenfall: The Last Harvest
// ============================================================
//
//  QUÉ HACE ESTE SCRIPT:
//  Aplica tres tipos de movimiento suave al arma para que se
//  sienta viva: balanceo idle, balanceo al caminar y balanceo
//  al mover la cámara (look sway).
//
//  CORRECCIÓN v2:
//  Bug anterior: al apuntar (click derecho), el WeaponAimController
//  deshabilitaba este componente pero la rotación quedaba congelada
//  en el valor del sway de ese instante (baseRotation * currentSway).
//  El arma apuntaba torcida porque WeaponAimController intentaba
//  moverse desde esa rotación sucia hacia la posición de apuntado.
//
//  Fix: Se agrega ResetImmediately() que WeaponAimController llama
//  ANTES de deshabilitar este componente. Eso limpia la rotación
//  para que el lerp de apuntado empiece desde una base limpia.
// ============================================================

using UnityEngine;

public class SwayController : MonoBehaviour
{
    // ── Habilitar / deshabilitar tipos de sway individualmente ────────────
    [Header("Estados activos")]
    public bool enableIdleSway = true;   // Balanceo cuando está quieto
    public bool enableMoveSway = true;   // Balanceo al caminar
    public bool enableLookSway = true;   // Balanceo al mover el mouse

    // ── Configuración del Idle Sway ───────────────────────────────────────
    [Header("Idle Sway")]
    [Tooltip("Amplitud en X e Y del balanceo idle (en grados).")]
    [SerializeField] Vector2 idleAmplitude = new Vector2(0.2f, 0.2f);

    [Tooltip("Velocidad del balanceo idle. Más alto = más rápido.")]
    [SerializeField] float idleSpeed = 1.5f;

    // ── Configuración del Move Sway ───────────────────────────────────────
    [Header("Move Sway")]
    [Tooltip("Amplitud en X e Y del balanceo al moverse (en grados).")]
    [SerializeField] Vector2 moveAmplitude = new Vector2(0.6f, 0.6f);

    [Tooltip("Velocidad del balanceo de movimiento.")]
    [SerializeField] float moveSpeed = 6f;

    // ── Configuración del Look Sway ───────────────────────────────────────
    [Header("Look Sway")]
    [Tooltip("Cuánto se inclina el arma al mover el mouse. Más alto = más inclinación.")]
    [SerializeField] float lookAmount = 1.5f;

    [Tooltip("Qué tan suave se interpola el sway. Más alto = más responsivo.")]
    [SerializeField] float lookSmooth = 8f;

    // ── Estado interno ────────────────────────────────────────────────────

    // La rotación "neutra" del arma, sin ningún sway aplicado.
    // Se captura en Awake() cuando el arma está en su posición inicial
    // y NUNCA se modifica después. Este es el punto de retorno.
    Quaternion _baseRotation;

    // Inputs que recibe desde el PlayerController
    Vector2 _movementInput;  // WASD del jugador
    Vector2 _lookInput;      // Delta del mouse
    bool _isMoving;          // Si el jugador está en movimiento

    // Temporizador para la animación sinusoidal del idle
    float _idleTimer;

    // El sway calculado en este frame (suavizado con Lerp)
    Quaternion _currentSway;

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Capturamos la rotación limpia del arma AHORA, antes de que
        // cualquier sistema (inventario, aim, etc.) la modifique.
        // Este es el "norte magnético" al que siempre volvemos.
        _baseRotation = transform.localRotation;
        _currentSway  = Quaternion.identity;
    }

    // ─────────────────────────────────────────────────────────────────────
    private void Update()
    {
        // Calculamos el sway total combinando los tres tipos habilitados
        Quaternion sway = Quaternion.identity;

        // Solo idle sway cuando el jugador NO se mueve
        if (enableIdleSway && !_isMoving)
            sway *= GetIdleSway();

        // Solo move sway cuando SÍ se mueve
        if (enableMoveSway && _isMoving)
            sway *= GetMoveSway();

        // Look sway siempre (depende del input del mouse)
        if (enableLookSway)
            sway *= GetLookSway();

        // Suavizamos la transición entre el sway anterior y el nuevo.
        // lookSmooth controla la velocidad de respuesta.
        _currentSway = Quaternion.Lerp(
            _currentSway,
            sway,
            Time.deltaTime * lookSmooth
        );

        // Aplicamos el sway SOBRE la rotación base limpia.
        // Importante: no modificamos _baseRotation, solo leemos de ella.
        transform.localRotation = _baseRotation * _currentSway;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  CÁLCULO DE CADA TIPO DE SWAY
    // ─────────────────────────────────────────────────────────────────────

    // Usa una función seno/coseno para crear un movimiento orgánico
    // de figura-8, como si el arma "respirara" suavemente.
    Quaternion GetIdleSway()
    {
        _idleTimer += Time.deltaTime * idleSpeed;

        float x = Mathf.Sin(_idleTimer)       * idleAmplitude.x;
        float y = Mathf.Cos(_idleTimer * 0.8f) * idleAmplitude.y;

        return Quaternion.Euler(x, y, 0f);
    }

    // Se basa en el input WASD para inclinar el arma en la dirección
    // opuesta al movimiento (como si tuviera inercia).
    Quaternion GetMoveSway()
    {
        float x = -_movementInput.y * moveAmplitude.x;  // W/S → inclina arriba/abajo
        float y =  _movementInput.x * moveAmplitude.y;  // A/D → inclina izquierda/derecha

        return Quaternion.Euler(x, y, 0f);
    }

    // Se basa en el delta del mouse para inclinar el arma ligeramente
    // en la dirección contraria al giro de la cámara (sensación de peso).
    Quaternion GetLookSway()
    {
        Vector3 lookEuler = new Vector3(
            -_lookInput.y * lookAmount,   // Mover mouse arriba/abajo
             _lookInput.x * lookAmount,   // Mover mouse izquierda/derecha
            0f
        );

        return Quaternion.Euler(lookEuler);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  API PÚBLICA — Lo que el PlayerController llama cada frame
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Recibe el input de movimiento del jugador (WASD normalizado).
    /// Llamar desde PlayerController.Update().
    /// </summary>
    public void SetMovementInput(Vector2 input)
    {
        _movementInput = input;
        _isMoving = input.sqrMagnitude > 0.01f; // sqrMagnitude es más barato que magnitude
    }

    /// <summary>
    /// Recibe el delta del mouse para el look sway.
    /// Llamar desde PlayerController.Update().
    /// </summary>
    public void SetLookInput(Vector2 input)
    {
        _lookInput = input;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  RESET — CRÍTICO para la integración con WeaponAimController
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// CORRECCIÓN PRINCIPAL v2.
    ///
    /// Snappea el sway instantáneamente a cero (sin lerp) y resetea
    /// la rotación del arma a su base limpia.
    ///
    /// WeaponAimController llama este método ANTES de deshabilitar
    /// este componente al apuntar. Sin este reset, el arma quedaba
    /// torcida durante el apuntado porque el lerp de aim arrancaba
    /// desde la rotación sucia del sway.
    ///
    /// También se llama cuando el arma es equipada de nuevo (OnEnable)
    /// para garantizar un estado limpio.
    /// </summary>
    public void ResetImmediately()
    {
        _currentSway  = Quaternion.identity;  // El sway calculado vuelve a cero
        _idleTimer    = 0f;                   // El timer idle también se reinicia
        // para evitar que el idle empiece desde un punto medio del seno

        // Aplicamos inmediatamente la rotación base limpia
        transform.localRotation = _baseRotation;
    }

    /// <summary>
    /// Actualiza la rotación base si el sistema de inventario
    /// cambió la rotación del arma (por offset de inventario).
    /// Llamar DESPUÉS de que ApplyInventoryOffset() modifique la rotación.
    /// </summary>
    public void RefreshBaseRotation()
    {
        // Primero reseteamos el sway para que la lectura sea limpia
        _currentSway = Quaternion.identity;
        // Luego capturamos la nueva rotación "neutral"
        _baseRotation = transform.localRotation;
    }

    // ─────────────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        // Cuando el componente se reactiva (después del apuntado),
        // nos aseguramos de arrancar desde un estado limpio.
        // Esto evita un "pop" visual al dejar de apuntar.
        ResetImmediately();
    }
}