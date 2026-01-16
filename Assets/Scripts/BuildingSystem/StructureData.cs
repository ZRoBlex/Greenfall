using UnityEngine;

[CreateAssetMenu(menuName = "Building/Structure Data")]
public class StructureData : ScriptableObject
{
    [Header("Prefabs")]
    public GameObject previewPrefab;
    public GameObject finalPrefab;

    [Header("Placement Rules")]
    public bool useGrid = true;
    public bool allowOverlap = false;
    public bool requiresSupport = true;
    public bool canFloat = false;

    [Header("Grid")]
    public Vector3 gridOffset;

    // 🧱 BUILDING STATS
    [Header("Structure Stats")]
    public int maxHealth = 300;
    public bool destructible = true;
    public bool canBeRepaired = true;

    [Tooltip("Resistencia al daño (0 = sin resistencia, 0.5 = 50%)")]
    [Range(0f, 1f)]
    public float damageResistance = 0f;
}
