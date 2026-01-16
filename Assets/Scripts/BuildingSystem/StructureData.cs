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
}
