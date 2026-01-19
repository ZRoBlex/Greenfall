using UnityEngine;
using System.Collections.Generic;

public class GridPathfinder : MonoBehaviour
{
    EnemyLocalGrid localGrid;

    static readonly Vector2Int[] Directions =
    {
        new Vector2Int( 0,  1),
        new Vector2Int( 0, -1),
        new Vector2Int(-1,  0),
        new Vector2Int( 1,  0),
        new Vector2Int( 1,  1),
        new Vector2Int( 1, -1),
        new Vector2Int(-1,  1),
        new Vector2Int(-1, -1),
    };

    void Awake()
    {
        localGrid = GetComponent<EnemyLocalGrid>();

        if (localGrid == null)
            Debug.LogError($"[GridPathfinder] Missing EnemyLocalGrid on {name}");
    }

    // ===================== ORIGINAL =====================
    public List<Vector3> FindPath(Vector2Int start, Vector2Int end)
    {
        return FindPath(start, end, null);
    }

    // ===================== NUEVO =====================
    public List<Vector3> FindPath(
        Vector2Int start,
        Vector2Int end,
        EnemyStats stats)
    {
        if (localGrid == null)
            return null;

        var open = new List<Vector2Int>();
        var closed = new HashSet<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var costSoFar = new Dictionary<Vector2Int, float>();

        open.Add(start);
        costSoFar[start] = 0f;

        while (open.Count > 0)
        {
            Vector2Int current = open[0];
            open.RemoveAt(0);

            if (current == end)
                return ReconstructPath(cameFrom, current);

            closed.Add(current);

            foreach (var dir in Directions)
            {
                Vector2Int next = current + dir;

                if (!localGrid.IsWalkable(next))
                    continue;

                // ===== ANTES (ELIMINADO) =====
                // if (stats != null &&
                //     GridManager.Instance.IsBlockedByStats(next, stats))
                //     continue;

                // ===== EVITAR CORTAR ESQUINAS =====
                if (dir.x != 0 && dir.y != 0)
                {
                    if (!localGrid.IsWalkable(
                            new Vector2Int(current.x + dir.x, current.y)) ||
                        !localGrid.IsWalkable(
                            new Vector2Int(current.x, current.y + dir.y)))
                        continue;
                }

                float moveCost = (dir.x != 0 && dir.y != 0) ? 1.4f : 1f;
                float newCost = costSoFar[current] + moveCost;

                if (costSoFar.ContainsKey(next) && newCost >= costSoFar[next])
                    continue;

                costSoFar[next] = newCost;
                cameFrom[next] = current;

                if (!open.Contains(next) && !closed.Contains(next))
                    open.Add(next);
            }
        }

        return null;
    }

    List<Vector3> ReconstructPath(
        Dictionary<Vector2Int, Vector2Int> cameFrom,
        Vector2Int current)
    {
        var path = new List<Vector3>();

        while (cameFrom.ContainsKey(current))
        {
            path.Add(localGrid.CellToWorld(current));
            current = cameFrom[current];
        }

        path.Reverse();
        return path;
    }
}
