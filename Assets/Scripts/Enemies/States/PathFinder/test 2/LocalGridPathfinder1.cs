using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LocalGridPathfinder1 - A* grid local, con funciones dinámicas:
/// - FindPath(start, target)
/// - IsNodeBlocked(nodePos)
/// - IsPathStillValid(path)
/// - RecalculateIfInvalid(currentPos, finalTarget, oldPath)
/// - Draw debug grid & path
/// </summary>
public class LocalGridPathfinder1 : MonoBehaviour
{
    [Header("Grid settings")]
    public int gridRadius = 8;
    public float cellSize = 0.75f;
    public float agentRadius = 0.35f;
    public float agentHeight = 1.6f;
    public float maxStepHeight = 0.5f;
    [Tooltip("Capas que cuentan como obstáculo (set en inspector)")]
    public LayerMask obstacleMask = ~0;

    [Header("Debug")]
    public bool drawGrid = false;
    public bool drawPath = true;

    Vector3 lastOrigin;
    Node[,] lastGrid = null;
    List<Vector3> lastPath = new List<Vector3>();

    class Node
    {
        public int x, y;
        public Vector3 worldPos;
        public bool walkable;
        public float g, h;
        public Node parent;
        public float f => g + h;
        public Node(int x, int y, Vector3 w, bool walkable)
        {
            this.x = x; this.y = y; worldPos = w; this.walkable = walkable;
        }
    }

    public List<Vector3> FindPath(Vector3 startWorld, Vector3 targetWorld)
    {
        lastPath.Clear();

        int size = gridRadius * 2 + 1;
        Node[,] grid = new Node[size, size];

        Vector3 origin = startWorld - new Vector3(gridRadius * cellSize, 0, gridRadius * cellSize);
        lastOrigin = origin;

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                Vector3 cellCenter = origin + new Vector3(i * cellSize + cellSize * 0.5f, 0, j * cellSize + cellSize * 0.5f);

                // raycast downward to find ground
                if (Physics.Raycast(cellCenter + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f, ~0, QueryTriggerInteraction.Ignore))
                {
                    cellCenter.y = hit.point.y;
                }
                else
                {
                    grid[i, j] = new Node(i, j, cellCenter, false);
                    continue;
                }

                Vector3 bottom = cellCenter + Vector3.up * agentRadius;
                Vector3 top = bottom + Vector3.up * Mathf.Max(0.01f, agentHeight - agentRadius * 2f);
                bool blocked = Physics.CheckCapsule(bottom, top, agentRadius, obstacleMask, QueryTriggerInteraction.Ignore);

                grid[i, j] = new Node(i, j, cellCenter, !blocked);
            }
        }

        lastGrid = grid;

        System.Func<Vector3, (int, int)> WorldToGrid = (Vector3 w) =>
        {
            int gx = Mathf.RoundToInt((w.x - origin.x - cellSize * 0.5f) / cellSize);
            int gy = Mathf.RoundToInt((w.z - origin.z - cellSize * 0.5f) / cellSize);
            gx = Mathf.Clamp(gx, 0, size - 1);
            gy = Mathf.Clamp(gy, 0, size - 1);
            return (gx, gy);
        };

        var s = WorldToGrid(startWorld);
        var t = WorldToGrid(targetWorld);

        Node start = grid[s.Item1, s.Item2];
        Node target = grid[t.Item1, t.Item2];

        if (!start.walkable)
        {
            Node near = FindNearestWalkable(grid, start);
            if (near == null) return lastPath;
            start = near;
        }

        if (!target.walkable)
        {
            Node near = FindNearestWalkable(grid, target);
            if (near == null) return lastPath;
            target = near;
        }

        var open = new List<Node>();
        var closed = new HashSet<Node>();

        start.g = 0;
        start.h = Heuristic(start, target);
        start.parent = null;
        open.Add(start);

        while (open.Count > 0)
        {
            open.Sort((a, b) => a.f.CompareTo(b.f));
            Node current = open[0];
            open.RemoveAt(0);
            closed.Add(current);

            if (current == target)
            {
                var rev = new List<Vector3>();
                Node cur = current;
                while (cur != null)
                {
                    rev.Add(cur.worldPos);
                    cur = cur.parent;
                }
                rev.Reverse();
                lastPath = SimplifyPath(rev);
                return new List<Vector3>(lastPath);
            }

            for (int nx = -1; nx <= 1; nx++)
            {
                for (int ny = -1; ny <= 1; ny++)
                {
                    if (nx == 0 && ny == 0) continue;
                    int ix = current.x + nx;
                    int iy = current.y + ny;
                    if (ix < 0 || iy < 0 || ix >= size || iy >= size) continue;
                    Node neighbor = grid[ix, iy];
                    if (!neighbor.walkable || closed.Contains(neighbor)) continue;

                    float dh = Mathf.Abs(neighbor.worldPos.y - current.worldPos.y);
                    if (dh > maxStepHeight) continue;

                    float tentativeG = current.g + Vector3.Distance(current.worldPos, neighbor.worldPos);
                    bool inOpen = open.Contains(neighbor);
                    if (!inOpen || tentativeG < neighbor.g)
                    {
                        neighbor.g = tentativeG;
                        neighbor.h = Heuristic(neighbor, target);
                        neighbor.parent = current;
                        if (!inOpen) open.Add(neighbor);
                    }
                }
            }
        }

        return lastPath;
    }

    Node FindNearestWalkable(Node[,] grid, Node target)
    {
        int sx = grid.GetLength(0), sy = grid.GetLength(1);
        int maxR = Mathf.Max(sx, sy);
        for (int r = 1; r < maxR; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    int nx = target.x + dx;
                    int ny = target.y + dy;
                    if (nx < 0 || ny < 0 || nx >= sx || ny >= sy) continue;
                    if (grid[nx, ny].walkable) return grid[nx, ny];
                }
            }
        }
        return null;
    }

    float Heuristic(Node a, Node b) => Vector3.Distance(a.worldPos, b.worldPos);

    List<Vector3> SimplifyPath(List<Vector3> pts)
    {
        if (pts == null) return new List<Vector3>();
        if (pts.Count < 3) return new List<Vector3>(pts);
        List<Vector3> res = new List<Vector3>();
        res.Add(pts[0]);
        for (int i = 1; i < pts.Count - 1; i++)
        {
            Vector3 prev = res[res.Count - 1];
            Vector3 curr = pts[i];
            Vector3 next = pts[i + 1];
            Vector3 v1 = (curr - prev).normalized;
            Vector3 v2 = (next - curr).normalized;
            if (Vector3.Dot(v1, v2) < 0.999f) res.Add(curr);
        }
        res.Add(pts[pts.Count - 1]);
        return res;
    }

    // -------------------------
    // DYNAMIC HELPERS
    // -------------------------
    public bool IsNodeBlocked(Vector3 nodePos)
    {
        Vector3 bottom = nodePos + Vector3.up * agentRadius;
        Vector3 top = bottom + Vector3.up * Mathf.Max(0.01f, agentHeight - agentRadius * 2f);
        return Physics.CheckCapsule(bottom, top, agentRadius, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    public bool IsPathStillValid(List<Vector3> path)
    {
        if (path == null || path.Count == 0) return false;
        for (int i = 0; i < path.Count; i++)
        {
            if (IsNodeBlocked(path[i])) return false;
        }
        return true;
    }

    public List<Vector3> RecalculateIfInvalid(Vector3 currentPos, Vector3 finalTarget, List<Vector3> oldPath)
    {
        if (IsPathStillValid(oldPath)) return oldPath;
        return FindPath(currentPos, finalTarget);
    }

    void OnDrawGizmos()
    {
        if (drawGrid && lastGrid != null)
        {
            Gizmos.color = new Color(0.25f, 0.25f, 0.25f, 0.6f);
            int sx = lastGrid.GetLength(0), sy = lastGrid.GetLength(1);
            for (int i = 0; i < sx; i++)
            {
                for (int j = 0; j < sy; j++)
                {
                    Node n = lastGrid[i, j];
                    if (n == null) continue;
                    Gizmos.color = n.walkable ? new Color(0.0f, 0.8f, 0.0f, 0.15f) : new Color(0.8f, 0, 0, 0.25f);
                    Gizmos.DrawCube(n.worldPos + Vector3.up * 0.01f, new Vector3(cellSize * 0.9f, 0.02f, cellSize * 0.9f));
                }
            }
        }

        if (drawPath && lastPath != null && lastPath.Count > 0)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < lastPath.Count; i++)
            {
                Gizmos.DrawSphere(lastPath[i] + Vector3.up * 0.05f, 0.12f);
                if (i < lastPath.Count - 1) Gizmos.DrawLine(lastPath[i] + Vector3.up * 0.05f, lastPath[i + 1] + Vector3.up * 0.05f);
            }
        }
    }
}
