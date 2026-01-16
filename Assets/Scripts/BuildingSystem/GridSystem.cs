using UnityEngine;

public class GridSystem : MonoBehaviour
{
    public float cellSize = 1f;

    public Vector3Int WorldToCellStable(Vector3 world, Vector3 rayDir)
    {
        // bias según dirección para evitar indecisión
        Vector3 bias = rayDir.normalized * 0.01f;

        return new Vector3Int(
            Mathf.FloorToInt((world.x + bias.x) / cellSize),
            0,
            Mathf.FloorToInt((world.z + bias.z) / cellSize)
        );
    }

    public Vector3 CellToWorld(Vector3Int cell)
    {
        return new Vector3(
            (cell.x + 0.5f) * cellSize,
            0,
            (cell.z + 0.5f) * cellSize
        );
    }
}
