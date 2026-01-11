//using UnityEngine;
//using System.Collections.Generic;

//[CreateAssetMenu(menuName = "Building/Structure")]
//public class StructureConfig : ScriptableObject
//{
//    public string id;

//    [Header("Prefabs")]
//    public GameObject previewPrefab; // sin colisión
//    public GameObject finalPrefab;   // con colisión

//    [Header("Grid Occupancy")]
//    public Vector3Int gridSize = new Vector3Int(4, 4, 1);
//    public List<Vector3Int> occupiedCells = new();

//    public int maxHealth = 100;
//}


//using UnityEngine;
//using System.Collections.Generic;

//[CreateAssetMenu(menuName = "Building/Structure")]
//public class StructureConfig : ScriptableObject
//{
//    public GameObject previewPrefab;
//    public GameObject finalPrefab;

//    [Header("Grid Size (X = ancho, Y = alto, Z = profundidad)")]
//    public Vector3Int gridSize = Vector3Int.one;

//    [HideInInspector]
//    public List<Vector3Int> occupiedCells;
//}

using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Building/Structure")]
public class StructureConfig : ScriptableObject
{
    public GameObject previewPrefab;
    public GameObject finalPrefab;

    [Header("Grid Bounds (solo referencia visual)")]
    public Vector3Int gridBounds = Vector3Int.one;

    [Header("Occupied Cells")]
    public List<Vector3Int> occupiedCells = new List<Vector3Int>();
}

