using UnityEngine;

public static class GridMath
{
    public static float CellSize = 0.5f; // CONFIGURABLE

    public static Vector3Int WorldToCell(Vector3 worldPos)
    {
        return new Vector3Int(
            Mathf.FloorToInt(worldPos.x / CellSize),
            Mathf.FloorToInt(worldPos.y / CellSize),
            Mathf.FloorToInt(worldPos.z / CellSize)
        );
    }

    public static Vector3 CellToWorld(Vector3Int cell)
    {
        return new Vector3(
            cell.x * CellSize,
            cell.y * CellSize,
            cell.z * CellSize
        );
    }
}
