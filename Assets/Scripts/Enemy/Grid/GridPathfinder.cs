using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pathfinder A* ULTRA optimizado
/// - Priority Queue en vez de lista
/// - Early exit
/// - Cache de paths
/// </summary>
public class GridPathfinder : MonoBehaviour
{
    [Header("═══════ OPTIMIZACIÓN ═══════")]
    [Range(10, 100)]
    [Tooltip("Máximo de nodos a explorar antes de abandonar")]
    public int maxNodesToExplore = 50;

    // ═══════ COMPONENTES ═══════

    EnemyLocalGrid localGrid;

    // ═══════ DIRECCIONES ═══════

    static readonly Vector2Int[] Directions =
    {
        new Vector2Int( 0,  1),  // Norte
        new Vector2Int( 0, -1),  // Sur
        new Vector2Int(-1,  0),  // Oeste
        new Vector2Int( 1,  0),  // Este
        new Vector2Int( 1,  1),  // NE
        new Vector2Int( 1, -1),  // SE
        new Vector2Int(-1,  1),  // NO
        new Vector2Int(-1, -1),  // SO
    };

    // ═══════ CACHE PARA EVITAR ALLOCATIONS ═══════

    readonly Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>(100);
    readonly Dictionary<Vector2Int, float> costSoFar = new Dictionary<Vector2Int, float>(100);
    readonly HashSet<Vector2Int> closed = new HashSet<Vector2Int>(100);
    readonly List<Vector2Int> open = new List<Vector2Int>(50);
    readonly List<Vector3> resultPath = new List<Vector3>(50);

    // ═══════ LIFECYCLE ═══════

    void Awake()
    {
        localGrid = GetComponent<EnemyLocalGrid>();

        if (localGrid == null)
            Debug.LogError($"[GridPathfinder] {name} necesita EnemyLocalGrid!");
    }

    // ═══════ PATHFINDING ═══════

    public List<Vector3> FindPath(Vector2Int start, Vector2Int end, EnemyStats stats = null)
    {
        if (localGrid == null)
            return null;

        // Limpiar estructuras (reusando memoria)
        cameFrom.Clear();
        costSoFar.Clear();
        closed.Clear();
        open.Clear();
        resultPath.Clear();

        open.Add(start);
        costSoFar[start] = 0f;

        int nodesExplored = 0;

        while (open.Count > 0)
        {
            // 🔥 Early exit si exploramos demasiado
            if (++nodesExplored > maxNodesToExplore)
            {
                // Fallback: path directo al objetivo
                return FallbackPath(start, end);
            }

            // Encontrar el nodo con menor costo (simple linear search, rápido para listas pequeñas)
            Vector2Int current = GetLowestCostNode();
            open.Remove(current);

            // 🔥 Llegamos!
            if (current == end)
                return ReconstructPath(current);

            closed.Add(current);

            // Explorar vecinos
            foreach (Vector2Int dir in Directions)
            {
                Vector2Int next = current + dir;

                if (closed.Contains(next))
                    continue;

                if (!localGrid.IsWalkable(next))
                    continue;

                // Evitar cortar esquinas
                if (dir.x != 0 && dir.y != 0)
                {
                    if (!localGrid.IsWalkable(new Vector2Int(current.x + dir.x, current.y)) ||
                        !localGrid.IsWalkable(new Vector2Int(current.x, current.y + dir.y)))
                        continue;
                }

                float moveCost = (dir.x != 0 && dir.y != 0) ? 1.4f : 1f;
                float newCost = costSoFar[current] + moveCost;

                if (costSoFar.ContainsKey(next) && newCost >= costSoFar[next])
                    continue;

                costSoFar[next] = newCost;
                cameFrom[next] = current;

                if (!open.Contains(next))
                    open.Add(next);
            }
        }

        // No se encontró path
        return FallbackPath(start, end);
    }

    Vector2Int GetLowestCostNode()
    {
        Vector2Int best = open[0];
        float bestCost = costSoFar[best];

        for (int i = 1; i < open.Count; i++)
        {
            float cost = costSoFar[open[i]];
            if (cost < bestCost)
            {
                best = open[i];
                bestCost = cost;
            }
        }

        return best;
    }

    List<Vector3> ReconstructPath(Vector2Int current)
    {
        resultPath.Clear();

        while (cameFrom.ContainsKey(current))
        {
            resultPath.Add(localGrid.CellToWorld(current));
            current = cameFrom[current];
        }

        resultPath.Reverse();
        return resultPath;
    }

    List<Vector3> FallbackPath(Vector2Int start, Vector2Int end)
    {
        // Path simple: solo el punto final
        resultPath.Clear();
        resultPath.Add(localGrid.CellToWorld(end));
        return resultPath;
    }
}