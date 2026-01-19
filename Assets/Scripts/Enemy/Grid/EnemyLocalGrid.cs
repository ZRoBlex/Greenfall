using UnityEngine;
using System.Collections.Generic;

public class EnemyLocalGrid : MonoBehaviour
{
    public EnemyStats stats;

    [Header("Grid Settings")]
    public float cellSize = 1f;
    public int gridRadius = 15;

    HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();

    /* ===================== WORLD <-> CELL ===================== */

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

    /* ===================== LIMIT ===================== */

    public bool IsInsideLocalGrid(Vector2Int cell)
    {
        Vector2Int center = WorldToCell(transform.position);
        Vector2Int delta = cell - center;

        return Mathf.Abs(delta.x) <= gridRadius &&
               Mathf.Abs(delta.y) <= gridRadius;
    }

    /* ===================== WALKABLE ===================== */

    public bool IsWalkable(Vector2Int cell)
    {
        if (!IsInsideLocalGrid(cell))
            return false;

        if (blocked.Contains(cell))
            return false;

        // 🔥 NUEVO: bloqueo dinámico por stats
        if (stats != null && IsBlockedByStats(cell))
            return false;

        return true;
    }

    /* ===================== OBSTÁCULOS (ANTES GridManager) ===================== */

    bool IsBlockedByStats(Vector2Int cell)
    {
        EnemyStats enemyStats = stats;
        // 🔹 Posición del centro del cubo de la celda
        Vector3 center = CellToWorld(cell) + Vector3.up * (stats.cellHeight * 0.5f);

        // 🔹 Tamaño del cubo (x, y, z)
        Vector3 halfExtents = new Vector3(
            cellSize * 0.45f,
            stats.cellHeight * 0.5f, // usamos la altura configurable
            cellSize * 0.45f
        );

        Collider[] hits = Physics.OverlapBox(
            center,
            halfExtents,
            Quaternion.identity,
            stats.obstacleLayers
        );

        foreach (var hit in hits)
        {
            if (stats.obstacleTags != null && stats.obstacleTags.Length > 0)
            {
                foreach (var tag in stats.obstacleTags)
                {
                    if (hit.CompareTag(tag))
                        return true;
                }
            }
            else
            {
                return true;
            }
        }

        return false;
    }


#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (stats == null)
            return;

        Vector2Int center = WorldToCell(transform.position);

        for (int x = -gridRadius; x <= gridRadius; x++)
        {
            for (int y = -gridRadius; y <= gridRadius; y++)
            {
                Vector2Int cell = center + new Vector2Int(x, y);
                Vector3 pos = CellToWorld(cell);

                // 🔹 Altura configurable
                float height = stats != null ? stats.obstacleCheckHeight : 1f;

                bool blockedByStats = stats != null && IsBlockedByStats(cell);

                if (blocked.Contains(cell) || blockedByStats)
                {
                    Gizmos.color = new Color(1, 0, 0, 0.35f);
                    Gizmos.DrawCube(pos + Vector3.up * height * 0.5f, new Vector3(cellSize, height, cellSize));
                }
                else
                {
                    Gizmos.color = new Color(0, 1, 0, 0.1f);
                    Gizmos.DrawWireCube(pos + Vector3.up * height * 0.5f, new Vector3(cellSize, height, cellSize));
                }
            }
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(CellToWorld(center) + Vector3.up * 0.1f, 0.2f);
    }
#endif
}
