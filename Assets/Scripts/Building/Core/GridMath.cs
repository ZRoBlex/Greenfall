using UnityEngine;

public static class GridMath
{
    public static Vector3 Snap(Vector3 pos, float size)
    {
        return new Vector3(
            Mathf.Round(pos.x / size) * size,
            Mathf.Round(pos.y / size) * size,
            Mathf.Round(pos.z / size) * size
        );
    }

    public static Vector3Int WorldToCell(Vector3 pos, float size)
    {
        return new Vector3Int(
            Mathf.FloorToInt(pos.x / size),
            Mathf.FloorToInt(pos.y / size),
            Mathf.FloorToInt(pos.z / size)
        );
    }
}
