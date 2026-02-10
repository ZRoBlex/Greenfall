using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Grid local ULTRA optimizado
/// - Cache de células bloqueadas
/// - Sin OverlapBox en Update
/// </summary>
public class EnemyLocalGrid : MonoBehaviour
{
    public EnemyStats stats;

    [Header("═══════ GRID ═══════")]
    public float cellSize = 1f;
    public int gridRadius = 15;

    [Header("═══════ CACHE ═══════")]
    [Tooltip("Re-escanear obstáculos cada X segundos")]
    [Range(0.5f, 5f)]
    public float rescanInterval = 2f;

    // ═══════ ESTADO ═══════

    HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();

    // ═══════ CACHE ═══════

    Transform cachedTransform;
    Vector3 lastScanPosition;
    float rescanTimer;

    const float MIN_MOVE_TO_RESCAN = 5f; // Solo re-escanear si se movió 5 unidades

    // ═══════ LIFECYCLE ═══════

    void Awake()
    {
        cachedTransform = transform;
    }

    void Start()
    {
        ScanObstacles();
    }

    void Update()
    {
        // 🔥 OPTIMIZACIÓN: Solo re-escanear si:
        // 1. Pasó suficiente tiempo
        // 2. Se movió lo suficiente

        rescanTimer -= Time.deltaTime;

        if (rescanTimer <= 0f)
        {
            float movedDist = Vector3.Distance(cachedTransform.position, lastScanPosition);

            if (movedDist > MIN_MOVE_TO_RESCAN)
            {
                ScanObstacles();
                rescanTimer = rescanInterval;
            }
            else
            {
                rescanTimer = rescanInterval * 0.5f; // Check más seguido si no se movió
            }
        }
    }

    // ═══════ CONVERSIONES ═══════

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

    // ═══════ WALKABILITY ═══════

    public bool IsWalkable(Vector2Int cell)
    {
        if (!IsInsideLocalGrid(cell))
            return false;

        if (blocked.Contains(cell))
            return false;

        return true;
    }

    public bool IsInsideLocalGrid(Vector2Int cell)
    {
        Vector2Int center = WorldToCell(cachedTransform.position);
        Vector2Int delta = cell - center;

        return Mathf.Abs(delta.x) <= gridRadius &&
               Mathf.Abs(delta.y) <= gridRadius;
    }

    // ═══════ ESCANEO DE OBSTÁCULOS ═══════

    void ScanObstacles()
    {
        if (stats == null) return;

        blocked.Clear();
        lastScanPosition = cachedTransform.position;

        Vector2Int center = WorldToCell(cachedTransform.position);

        // Escanear solo celdas cercanas (no todo el grid)
        int scanRadius = Mathf.Min(gridRadius, 10);

        for (int x = -scanRadius; x <= scanRadius; x++)
        {
            for (int y = -scanRadius; y <= scanRadius; y++)
            {
                Vector2Int cell = center + new Vector2Int(x, y);

                if (IsBlockedByObstacle(cell))
                {
                    blocked.Add(cell);
                }
            }
        }
    }

    bool IsBlockedByObstacle(Vector2Int cell)
    {
        if (stats == null) return false;

        Vector3 center = CellToWorld(cell) + Vector3.up * (stats.cellHeight * 0.5f);
        Vector3 halfExtents = new Vector3(
            cellSize * 0.45f,
            stats.cellHeight * 0.5f,
            cellSize * 0.45f
        );

        Collider[] hits = Physics.OverlapBox(
            center,
            halfExtents,
            Quaternion.identity,
            stats.obstacleLayers,
            QueryTriggerInteraction.Ignore
        );

        if (hits.Length == 0)
            return false;

        // Verificar tags si están configurados
        if (stats.obstacleTags == null || stats.obstacleTags.Length == 0)
            return true;

        foreach (var hit in hits)
        {
            foreach (var tag in stats.obstacleTags)
            {
                if (hit.CompareTag(tag))
                    return true;
            }
        }

        return false;
    }

    // ═══════ API PÚBLICA ═══════

    public void ForceRescan()
    {
        ScanObstacles();
        rescanTimer = rescanInterval;
    }

    // ═══════ GIZMOS ═══════

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (stats == null) return;

        Vector2Int center = WorldToCell(transform.position);

        for (int x = -gridRadius; x <= gridRadius; x++)
        {
            for (int y = -gridRadius; y <= gridRadius; y++)
            {
                Vector2Int cell = center + new Vector2Int(x, y);
                Vector3 pos = CellToWorld(cell);

                float height = stats.obstacleCheckHeight;

                if (blocked.Contains(cell))
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

        // Centro
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(CellToWorld(center) + Vector3.up * 0.1f, 0.3f);
    }
#endif
}