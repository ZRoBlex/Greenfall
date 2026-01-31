using UnityEngine;
using System.Collections.Generic;

public class NPCLocalGrid : MonoBehaviour
{
    public NPCStats stats;

    public float cellSize = 1f;
    public int gridRadius = 15;

    HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();

    public Vector2Int WorldToCell(Vector3 world)
    {
        return new Vector2Int(
            Mathf.RoundToInt(world.x / cellSize),
            Mathf.RoundToInt(world.z / cellSize)
        );
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(
            cell.x * cellSize,
            0f,
            cell.y * cellSize
        );
    }

    public bool IsInsideLocalGrid(Vector2Int cell)
    {
        Vector2Int center = WorldToCell(transform.position);
        Vector2Int delta = cell - center;

        return Mathf.Abs(delta.x) <= gridRadius &&
               Mathf.Abs(delta.y) <= gridRadius;
    }

    public bool IsWalkable(Vector2Int cell)
    {
        if (!IsInsideLocalGrid(cell))
            return false;

        if (blocked.Contains(cell))
            return false;

        if (stats != null && IsBlockedByStats(cell))
            return false;

        return true;
    }

    bool IsBlockedByStats(Vector2Int cell)
    {
        Vector3 center = CellToWorld(cell) + Vector3.up * (stats.cellHeight * 0.5f);
        Vector3 halfExtents = new Vector3(cellSize * 0.45f, stats.cellHeight * 0.5f, cellSize * 0.45f);

        Collider[] hits = Physics.OverlapBox(center, halfExtents, Quaternion.identity, stats.obstacleLayers);
        foreach (var hit in hits)
        {
            if (stats.obstacleTags != null && stats.obstacleTags.Length > 0)
            {
                foreach (var tag in stats.obstacleTags)
                    if (hit.CompareTag(tag))
                        return true;
            }
            else
                return true;
        }

        return false;
    }
}
