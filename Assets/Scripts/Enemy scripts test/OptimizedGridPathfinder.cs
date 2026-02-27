using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Optimized A* Pathfinder with:
/// - Priority Queue (O(log n) vs O(n))
/// - Iteration limits to prevent frame spikes
/// - Object pooling for nodes
/// - Early exit optimizations
/// </summary>
public class OptimizedGridPathfinder : MonoBehaviour
{
    private EnemyLocalGrid localGrid;

    // Pre-allocated structures (no GC during pathfinding)
    private PriorityQueue<PathNode> openSet;
    private HashSet<Vector2Int> closedSet;
    private Dictionary<Vector2Int, PathNode> allNodes;
    private Stack<PathNode> nodePool;
    private List<Vector3> pathBuffer;

    // Iteration limit to prevent frame spikes
    private const int MAX_ITERATIONS = 500;

    private static readonly Vector2Int[] Directions = new Vector2Int[8]
    {
        new Vector2Int( 0,  1),  // N
        new Vector2Int( 0, -1),  // S
        new Vector2Int(-1,  0),  // W
        new Vector2Int( 1,  0),  // E
        new Vector2Int( 1,  1),  // NE
        new Vector2Int( 1, -1),  // SE
        new Vector2Int(-1,  1),  // NW
        new Vector2Int(-1, -1),  // SW
    };

    // Cached costs
    private const float STRAIGHT_COST = 1f;
    private const float DIAGONAL_COST = 1.41421356f; // sqrt(2)

    void Awake()
    {
        localGrid = GetComponent<EnemyLocalGrid>();

        if (localGrid == null)
            Debug.LogError($"[OptimizedGridPathfinder] Missing EnemyLocalGrid on {name}");

        // Pre-allocate collections
        openSet = new PriorityQueue<PathNode>(256);
        closedSet = new HashSet<Vector2Int>();
        allNodes = new Dictionary<Vector2Int, PathNode>(512);
        nodePool = new Stack<PathNode>(512);
        pathBuffer = new List<Vector3>(64);

        // Pre-warm node pool
        for (int i = 0; i < 100; i++)
        {
            nodePool.Push(new PathNode());
        }
    }

    /// <summary>
    /// Find path with iteration limit and early exit
    /// </summary>
    public List<Vector3> FindPath(Vector2Int start, Vector2Int goal)
    {
        if (localGrid == null || !localGrid.IsWalkable(goal))
            return null;

        // Early exit: already at goal
        if (start == goal)
            return new List<Vector3> { localGrid.CellToWorld(goal) };

        // Reset collections (reuse memory)
        openSet.Clear();
        closedSet.Clear();
        ReturnNodesToPool();
        allNodes.Clear();

        // Start node
        PathNode startNode = GetPooledNode();
        startNode.Set(start, 0, Heuristic(start, goal), null);

        openSet.Enqueue(startNode, startNode.fCost);
        allNodes[start] = startNode;

        int iterations = 0;

        while (openSet.Count > 0 && iterations < MAX_ITERATIONS)
        {
            iterations++;

            PathNode current = openSet.Dequeue();

            // Goal reached
            if (current.position == goal)
            {
                return ReconstructPath(current);
            }

            closedSet.Add(current.position);

            // Explore neighbors
            for (int i = 0; i < 8; i++)
            {
                Vector2Int dir = Directions[i];
                Vector2Int neighborPos = current.position + dir;

                // Skip if closed or unwalkable
                if (closedSet.Contains(neighborPos) || !localGrid.IsWalkable(neighborPos))
                    continue;

                // Prevent corner cutting
                if (dir.x != 0 && dir.y != 0)
                {
                    if (!localGrid.IsWalkable(new Vector2Int(current.position.x + dir.x, current.position.y)) ||
                        !localGrid.IsWalkable(new Vector2Int(current.position.x, current.position.y + dir.y)))
                        continue;
                }

                float moveCost = (dir.x != 0 && dir.y != 0) ? DIAGONAL_COST : STRAIGHT_COST;
                float newGCost = current.gCost + moveCost;

                // Get or create neighbor node
                if (!allNodes.TryGetValue(neighborPos, out PathNode neighbor))
                {
                    neighbor = GetPooledNode();
                    neighbor.Set(neighborPos, float.MaxValue, Heuristic(neighborPos, goal), null);
                    allNodes[neighborPos] = neighbor;
                }

                // Found better path to this neighbor
                if (newGCost < neighbor.gCost)
                {
                    neighbor.gCost = newGCost;
                    neighbor.parent = current;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Enqueue(neighbor, neighbor.fCost);
                    }
                }
            }
        }

        // No path found
        return null;
    }

    /// <summary>
    /// Manhattan distance heuristic (admissible for grid)
    /// </summary>
    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);

        // Octile distance (better for 8-directional movement)
        return STRAIGHT_COST * (dx + dy) + (DIAGONAL_COST - 2 * STRAIGHT_COST) * Mathf.Min(dx, dy);
    }

    /// <summary>
    /// Reconstruct path from goal to start
    /// </summary>
    private List<Vector3> ReconstructPath(PathNode goalNode)
    {
        pathBuffer.Clear();

        PathNode current = goalNode;
        while (current != null)
        {
            pathBuffer.Add(localGrid.CellToWorld(current.position));
            current = current.parent;
        }

        pathBuffer.Reverse();

        // Return copy to avoid external modification
        return new List<Vector3>(pathBuffer);
    }

    /// <summary>
    /// Get node from pool or create new one
    /// </summary>
    private PathNode GetPooledNode()
    {
        return nodePool.Count > 0 ? nodePool.Pop() : new PathNode();
    }

    /// <summary>
    /// Return all nodes to pool for reuse
    /// </summary>
    private void ReturnNodesToPool()
    {
        foreach (var node in allNodes.Values)
        {
            if (nodePool.Count < 512) // Limit pool size
                nodePool.Push(node);
        }
    }
}
