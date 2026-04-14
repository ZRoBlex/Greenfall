// ============================================================
//  BuildingFloor.cs  — v2 + DoorFacing
//  Assets/CityGenerator/Scripts/BuildingFloor.cs
// ============================================================

using UnityEngine;

[AddComponentMenu("CityGenerator/Building Floor")]
public class BuildingFloor : MonoBehaviour
{
    // ── Planta baja ───────────────────────────────────────────────────────
    [Header("Planta baja (con puerta/entrada)")]
    [Tooltip("Prefab del PRIMER PISO (con puerta).\n" +
             "Si está vacío se usa este mismo prefab como planta baja.")]
    public GameObject groundFloorPrefab;

    // ── Puerta ────────────────────────────────────────────────────────────
    [Header("Dirección de la puerta")]
    [Tooltip("¿Hacia qué dirección mira la puerta cuando la rotación del prefab es (0,0,0)?\n\n" +
             "Forward  = la puerta mira hacia +Z  (frente del prefab)\n" +
             "Right    = la puerta mira hacia +X\n" +
             "Back     = la puerta mira hacia -Z\n" +
             "Left     = la puerta mira hacia -X\n\n" +
             "El generador usará esto para girar cada edificio y\n" +
             "asegurarse de que la puerta siempre mire hacia la calle.")]
    public DoorFacing doorFacing = DoorFacing.Forward;

    // ── Dimensiones ───────────────────────────────────────────────────────
    [Header("Dimensiones del piso")]
    [Tooltip("Si true, detecta las dimensiones desde el MeshFilter/Bounds.")]
    public bool autoDetectSize = true;

    [Min(0.1f)] public float height = 3.0f;
    [Min(0.1f)] public float width  = 10.0f;
    [Min(0.1f)] public float depth  = 10.0f;

    // ── Propiedades ───────────────────────────────────────────────────────
    public float Height => autoDetectSize ? DetectedSize.y : height;
    public float Width  => autoDetectSize ? DetectedSize.x : width;
    public float Depth  => autoDetectSize ? DetectedSize.z : depth;

    // ── API ───────────────────────────────────────────────────────────────

    /// <summary>Prefab para el índice de piso dado. 0 = planta baja.</summary>
    public GameObject GetPrefabForFloor(int floorIndex)
    {
        if (floorIndex == 0 && groundFloorPrefab != null) return groundFloorPrefab;
        return gameObject;
    }

    /// <summary>
    /// Altura REAL del piso indicado.
    /// Piso 0 → lee BuildingFloor del groundFloorPrefab.
    /// Piso 1+ → usa Height de este prefab.
    /// Esto corrige el offset cuando planta baja ≠ pisos intermedios.
    /// </summary>
    public float GetFloorHeight(int floorIndex)
    {
        if (floorIndex == 0 && groundFloorPrefab != null)
        {
            var gfd = groundFloorPrefab.GetComponent<BuildingFloor>();
            if (gfd != null) return gfd.Height;
        }
        return Height;
    }

    /// <summary>
    /// Calcula la rotación Y necesaria para que la puerta mire
    /// hacia targetWorldAngleDeg (0=norte/+Z, 90=este, 180=sur, 270=oeste).
    /// </summary>
    public float GetDoorRotationY(float targetWorldAngleDeg)
    {
        float localDoorAngle = (float)doorFacing;
        return targetWorldAngleDeg - localDoorAngle;
    }

    // ── Utilidades ────────────────────────────────────────────────────────
    public Vector3 DetectedSize
    {
        get
        {
            var mf = GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                var b = mf.sharedMesh.bounds.size;
                var s = transform.localScale;
                return new Vector3(
                    Mathf.Max(0.01f, b.x * Mathf.Abs(s.x)),
                    Mathf.Max(0.01f, b.y * Mathf.Abs(s.y)),
                    Mathf.Max(0.01f, b.z * Mathf.Abs(s.z)));
            }
            var r = GetComponentInChildren<Renderer>();
            if (r != null) return r.bounds.size;
            return new Vector3(width, height, depth);
        }
    }

    [ContextMenu("Bake dimensiones desde el mesh")]
    public void BakeSize()
    {
        var s = DetectedSize;
        width = s.x; height = s.y; depth = s.z;
        autoDetectSize = false;
        Debug.Log($"[BuildingFloor] Baked: {width:F2}m × {height:F2}m × {depth:F2}m");
    }

    private void OnValidate()
    {
        height = Mathf.Max(0.1f, height);
        width  = Mathf.Max(0.1f, width);
        depth  = Mathf.Max(0.1f, depth);
    }

    private void OnDrawGizmosSelected()
    {
        float h = Height, w = Width, d = Depth;
        Gizmos.color  = new Color(0.2f, 0.9f, 0.4f, 0.18f);
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawCube(Vector3.up * (h * 0.5f), new Vector3(w, h, d));
        Gizmos.color  = new Color(0.2f, 0.9f, 0.4f, 0.85f);
        Gizmos.DrawWireCube(Vector3.up * (h * 0.5f), new Vector3(w, h, d));

        // Flecha de dirección de puerta
        Gizmos.color = Color.red;
        Vector3 doorDir = doorFacing switch
        {
            DoorFacing.Right => transform.right,
            DoorFacing.Back  => -transform.forward,
            DoorFacing.Left  => -transform.right,
            _                => transform.forward
        };
        Vector3 origin = transform.position + Vector3.up * (h * 0.5f);
        Gizmos.DrawRay(origin, doorDir * (w * 0.7f));
    }
}

// ── Enum de dirección de puerta ────────────────────────────────────────────
/// <summary>
/// Indica hacia qué dirección local mira la puerta cuando el prefab
/// tiene rotación (0,0,0). El generador la usará para orientar el edificio.
///
/// Forward  (+Z) = frente del prefab en Unity  = rotY 0°
/// Right    (+X) = derecha del prefab           = rotY 90°
/// Back     (-Z) = atrás del prefab             = rotY 180°
/// Left     (-X) = izquierda del prefab         = rotY 270°
/// </summary>
public enum DoorFacing
{
    Forward = 0,    // puerta mira +Z a rotación 0°
    Right   = 90,   // puerta mira +X a rotación 0°
    Back    = 180,  // puerta mira -Z a rotación 0°
    Left    = 270   // puerta mira -X a rotación 0°
}