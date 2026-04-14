// ============================================================
//  CityBuildingEntryPoint.cs
//  Punto de entrada a un edificio estilo GTA.
//  Colocado por CityGenerator frente a los edificios.
//
//  Setup del prefab:
//  1. Crea un GameObject vacío
//  2. Agrégale este componente
//  3. Agrega un MeshRenderer (cilindro o quad) como hijo y asígnalo a portalMesh
//  4. Agrega un TextMeshPro (World Space) como hijo para el label
//  5. Guarda como prefab y asígnalo en CityData → entryPointEffectPrefab
// ============================================================

using UnityEngine;

#if TMP_PRESENT
using TMPro;
#endif

[AddComponentMenu("CityGenerator/Building Entry Point")]
public class CityBuildingEntryPoint : MonoBehaviour
{
    // ──────────────────────────────────────────
    // CONFIGURACIÓN
    // ──────────────────────────────────────────

    [Header("Configuración")]
    [Tooltip("Distancia al jugador para activar el prompt.")]
    public float activationRadius = 2.5f;

    [Tooltip("Tecla de interacción.")]
    public KeyCode interactKey = KeyCode.E;

    // ──────────────────────────────────────────
    // REFERENCIAS DEL PREFAB
    // ──────────────────────────────────────────

    [Header("Referencias (asignar en el prefab)")]
    [Tooltip("El objeto visual del portal (MeshRenderer con material emisivo).")]
    [SerializeField] private MeshRenderer portalMesh;

#if TMP_PRESENT
    [Tooltip("Label de texto en World Space.")]
    [SerializeField] private TextMeshPro  promptLabel;
#else
    // Si no tienes TMP, usa un componente de texto normal
    // o simplemente deja el campo vacío y usa solo el efecto visual
    [SerializeField] private MeshRenderer _labelPlaceholder; // solo para que compile
#endif

    // ──────────────────────────────────────────
    // ESTADO INTERNO
    // ──────────────────────────────────────────

    private bool      _playerNearby;
    private Transform _playerTr;

    // MaterialPropertyBlock: modifica el material sin crear copias (cero garbage)
    private MaterialPropertyBlock _mpb;
    private static readonly int   EmissionColorID = Shader.PropertyToID("_EmissionColor");
    private static readonly int   BaseColorID     = Shader.PropertyToID("_Color");

    private Color _portalColor = new Color(1f, 0.8f, 0f, 0.9f);
    private float _pulseTimer;

    // ──────────────────────────────────────────
    // UNITY CALLBACKS
    // ──────────────────────────────────────────

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _pulseTimer = UnityEngine.Random.Range(0f, Mathf.PI * 2f); // offset para variedad

        // Ocultar el label al inicio
#if TMP_PRESENT
        if (promptLabel != null) promptLabel.gameObject.SetActive(false);
#endif
    }

    private void Start()
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            _playerTr = playerGO.transform;
    }

    private void Update()
    {
        if (_playerTr == null) return;

        // ── Distancia al jugador ──────────────────────────────────────
        float distSq = (transform.position - _playerTr.position).sqrMagnitude;
        bool  isNear = distSq <= activationRadius * activationRadius;

        // ── Mostrar/ocultar prompt ────────────────────────────────────
        if (isNear != _playerNearby)
        {
            _playerNearby = isNear;
#if TMP_PRESENT
            if (promptLabel != null) promptLabel.gameObject.SetActive(isNear);
#endif
        }

        // ── Interacción ───────────────────────────────────────────────
        if (_playerNearby && Input.GetKeyDown(interactKey))
            OnPlayerEnter();

        // ── Animación del portal ──────────────────────────────────────
        AnimatePortal();
    }

    // ──────────────────────────────────────────
    // ENTRADA AL EDIFICIO
    // ──────────────────────────────────────────

    private void OnPlayerEnter()
    {
        Debug.Log($"[EntryPoint] Jugador entra a '{name}' en {transform.position}");

        // Aquí irá la lógica de transición:
        // - Fade a negro
        // - Cargar escena interior
        // - Teleportar al jugador
        // Por ahora dispara el evento y muestra un log.

        // Ejemplo: CitySceneTransition.Instance?.Enter(this);
    }

    // ──────────────────────────────────────────
    // ANIMACIÓN DEL PORTAL (pulso suave)
    // ──────────────────────────────────────────

    private void AnimatePortal()
    {
        if (portalMesh == null) return;

        _pulseTimer += Time.deltaTime * 2.5f;
        float pulse = (Mathf.Sin(_pulseTimer) + 1f) * 0.5f; // [0, 1]

        // Color con alpha pulsante
        Color animated = _portalColor;
        animated.a = Mathf.Lerp(0.45f, 1.0f, pulse);

        // Aplicar via MaterialPropertyBlock (sin garbage)
        portalMesh.GetPropertyBlock(_mpb);
        _mpb.SetColor(BaseColorID, animated);
        // Si usas shader con emisión:
        _mpb.SetColor(EmissionColorID, animated * Mathf.Lerp(0.5f, 1.5f, pulse));
        portalMesh.SetPropertyBlock(_mpb);

        // Escala pulsante sutil en Y
        float sy = Mathf.Lerp(0.92f, 1.08f, pulse);
        transform.localScale = new Vector3(1f, sy, 1f);
    }

    // ──────────────────────────────────────────
    // INICIALIZACIÓN (llamado por CityGenerator)
    // ──────────────────────────────────────────

    /// <summary>
    /// Configura el entry point con los parámetros del tipo de edificio.
    /// El CityGenerator llama esto justo después de instanciar el prefab.
    /// </summary>
    public void Initialize(string promptText, Color color)
    {
        _portalColor = color;

#if TMP_PRESENT
        if (promptLabel != null)
        {
            promptLabel.text  = promptText;
            promptLabel.color = color;
        }
#endif

        // Aplicar color inicial al portal
        if (portalMesh != null)
        {
            portalMesh.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorID, color);
            portalMesh.SetPropertyBlock(_mpb);
        }
    }

    // ──────────────────────────────────────────
    // GIZMOS
    // ──────────────────────────────────────────

    private void OnDrawGizmos()
    {
        // Esfera semi-transparente mostrando el radio de activación
        Gizmos.color = new Color(_portalColor.r, _portalColor.g, _portalColor.b, 0.2f);
        Gizmos.DrawSphere(transform.position, activationRadius);

        Gizmos.color = new Color(_portalColor.r, _portalColor.g, _portalColor.b, 1f);
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }
}