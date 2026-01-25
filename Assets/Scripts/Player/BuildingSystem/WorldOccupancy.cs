using UnityEngine;
using System.Collections.Generic;

public class WorldOccupancy : MonoBehaviour
{
    HashSet<Vector3Int> occupied = new();

    public bool IsOccupied(Bounds bounds, float cellSize)
    {
        foreach (var c in BoundsToCells(bounds, cellSize))
            if (occupied.Contains(c))
                return true;

        return false;
    }

    public void Register(Bounds bounds, float cellSize)
    {
        foreach (var c in BoundsToCells(bounds, cellSize))
            occupied.Add(c);
    }

    IEnumerable<Vector3Int> BoundsToCells(Bounds b, float size)
    {
        for (float x = b.min.x; x <= b.max.x; x += size)
            for (float z = b.min.z; z <= b.max.z; z += size)
            {
                yield return new Vector3Int(
                    Mathf.FloorToInt(x / size),
                    0,
                    Mathf.FloorToInt(z / size)
                );
            }
    }
}
