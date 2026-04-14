// ============================================================
//  GridBuildingEntryPoint.cs  — Sistema Grid (INDEPENDIENTE)
//  Assets/GridCityGenerator/Scripts/
// ============================================================

using UnityEngine;

[AddComponentMenu("GridCity/Building Entry Point")]
public class GridBuildingEntryPoint : MonoBehaviour
{
    [Header("Configuración")]
    public float   activationRadius = 2.5f;
    public KeyCode interactKey      = KeyCode.E;

    [Header("Referencias del Prefab")]
    [SerializeField] private MeshRenderer portalMesh;
    [SerializeField] private MeshRenderer labelMesh; // opcional

    // ── Estado ────────────────────────────────────────────────────────────
    private bool      _near;
    private Transform _player;
    private MaterialPropertyBlock _mpb;
    private Color  _color   = new Color(1f, 0.8f, 0f);
    private float  _pulse;

    private static readonly int ColorID = Shader.PropertyToID("_Color");
    private static readonly int EmitID  = Shader.PropertyToID("_EmissionColor");

    // ── Unity ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        _mpb   = new MaterialPropertyBlock();
        _pulse = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
    }

    private void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) _player = p.transform;
    }

    private void Update()
    {
        if (_player == null) return;

        bool isNear = (_player.position - transform.position).sqrMagnitude
                      <= activationRadius * activationRadius;

        if (isNear != _near)
        {
            _near = isNear;
            if (labelMesh) labelMesh.gameObject.SetActive(isNear);
        }

        if (_near && Input.GetKeyDown(interactKey))
        {
            Debug.Log($"[GridEntryPoint] Entrando a {name}");
            // Aquí va la lógica de transición de escena
        }

        Animate();
    }

    private void Animate()
    {
        if (!portalMesh) return;
        _pulse += Time.deltaTime * 2.5f;
        float t = (Mathf.Sin(_pulse) + 1f) * 0.5f;
        Color c = _color;
        c.a = Mathf.Lerp(0.45f, 1f, t);
        portalMesh.GetPropertyBlock(_mpb);
        _mpb.SetColor(ColorID, c);
        _mpb.SetColor(EmitID, c * Mathf.Lerp(0.5f, 1.5f, t));
        portalMesh.SetPropertyBlock(_mpb);
        transform.localScale = new Vector3(1f, Mathf.Lerp(0.92f, 1.08f, t), 1f);
    }

    /// <summary> Llamado por GridCityGenerator después de obtener del pool. </summary>
    public void Initialize(string promptText, Color color)
    {
        _color = color;
        if (portalMesh)
        {
            portalMesh.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorID, color);
            portalMesh.SetPropertyBlock(_mpb);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(_color.r, _color.g, _color.b, 0.2f);
        Gizmos.DrawSphere(transform.position, activationRadius);
        Gizmos.color = new Color(_color.r, _color.g, _color.b, 1f);
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }
}