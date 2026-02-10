using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Motor de movimiento ULTRA optimizado
/// </summary>
[RequireComponent(typeof(GridPathfinder), typeof(CharacterController))]
public class EnemyMotor : MonoBehaviour
{
    public EnemyStats stats;

    [Header("═══════ REFERENCIAS ═══════")]
    public EnemyLocalGrid localGrid;

    [Header("═══════ CONFIGURACIÓN ═══════")]
    public bool rotateTowardsMovement = true;

    [Header("═══════ OPTIMIZACIÓN ═══════")]
    [Range(0.3f, 2f)]
    public float stuckCheckInterval = 0.8f;

    [Range(0.1f, 0.5f)]
    public float minMoveDistance = 0.15f;

    public LayerMask obstacleMask;

    // ═══════ COMPONENTES ═══════

    GridPathfinder pathfinder;
    CharacterController controller;

    // ═══════ PATHFINDING ═══════

    List<Vector3> path;
    int currentPathIndex;

    Transform followTarget;
    Vector2Int targetCell;

    // ═══════ TIMERS ═══════

    float repathTimer;
    float stuckTimer;

    const float REPATH_INTERVAL = 2f;
    const float GRAVITY = -20f;

    // ═══════ MOVIMIENTO ═══════

    float verticalVelocity;
    Vector3 smoothDirection;
    Vector3 lastPosition;

    // ═══════ CACHE ═══════

    Transform cachedTransform;

    // ═══════ UNITY LIFECYCLE ═══════

    void Awake()
    {
        cachedTransform = transform;

        pathfinder = GetComponent<GridPathfinder>();
        controller = GetComponent<CharacterController>();
        localGrid = GetComponent<EnemyLocalGrid>();

        lastPosition = cachedTransform.position;

        ValidateComponents();
    }

    void Update()
    {
        HandleRepath();
        MoveAlongPath();
        ApplyGravity();
        CheckIfStuck();
    }

    // ═══════ VALIDACIÓN ═══════

    void ValidateComponents()
    {
        if (localGrid == null)
            Debug.LogError($"[EnemyMotor] {name} no tiene EnemyLocalGrid!");

        if (stats == null)
            Debug.LogError($"[EnemyMotor] {name} no tiene EnemyStats!");
    }

    // ═══════ PATHFINDING ═══════

    public void SetTarget(Transform target)
    {
        followTarget = target;
        repathTimer = 0f;
    }

    public void SetDestination(Vector2Int cell)
    {
        followTarget = null;
        targetCell = cell;
        RecalculatePath();
    }

    void HandleRepath()
    {
        repathTimer -= Time.deltaTime;

        if (followTarget != null && repathTimer <= 0f)
        {
            targetCell = FindBestFollowCell(followTarget);
            RecalculatePath();
        }
    }

    void RecalculatePath()
    {
        if (localGrid == null || stats == null)
            return;

        if (EnemyManager.Instance != null && !EnemyManager.Instance.CanRepath())
            return;

        Vector2Int start = localGrid.WorldToCell(cachedTransform.position);
        path = pathfinder.FindPath(start, targetCell, stats);

        currentPathIndex = 0;
        repathTimer = REPATH_INTERVAL;
    }

    Vector2Int FindBestFollowCell(Transform target)
    {
        Vector2Int targetCenter = localGrid.WorldToCell(target.position);
        Vector2Int myCell = localGrid.WorldToCell(cachedTransform.position);

        Vector2Int bestCell = myCell;
        float bestScore = float.MaxValue;

        for (int x = -stats.followMaxDistance; x <= stats.followMaxDistance; x++)
        {
            for (int y = -stats.followMaxDistance; y <= stats.followMaxDistance; y++)
            {
                Vector2Int candidate = targetCenter + new Vector2Int(x, y);

                int distToTarget = Mathf.Abs(x) + Mathf.Abs(y);

                if (distToTarget < stats.followMinDistance)
                    continue;

                if (!localGrid.IsWalkable(candidate))
                    continue;

                // 🔥 Usar distancia Manhattan (más rápida)
                int distToMe = Mathf.Abs(candidate.x - myCell.x) + Mathf.Abs(candidate.y - myCell.y);

                if (distToMe < bestScore)
                {
                    bestScore = distToMe;
                    bestCell = candidate;
                }
            }
        }

        return bestCell;
    }

    // ═══════ MOVIMIENTO ═══════

    void MoveAlongPath()
    {
        if (path == null || currentPathIndex >= path.Count)
            return;

        Vector3 targetPos = path[currentPathIndex];
        Vector3 flatTarget = new Vector3(targetPos.x, cachedTransform.position.y, targetPos.z);

        Vector3 toTarget = flatTarget - cachedTransform.position;
        float distanceSqr = toTarget.sqrMagnitude;

        // Llegamos al waypoint
        if (distanceSqr < 0.35f * 0.35f) // sqr para evitar sqrt
        {
            currentPathIndex++;
            return;
        }

        Vector3 desiredDirection = toTarget.normalized;

        smoothDirection = Vector3.Slerp(
            smoothDirection == Vector3.zero ? desiredDirection : smoothDirection,
            desiredDirection,
            Time.deltaTime * stats.turnSpeed
        );

        Vector3 direction = smoothDirection.normalized;

        if (rotateTowardsMovement && direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            cachedTransform.rotation = Quaternion.Slerp(
                cachedTransform.rotation,
                targetRot,
                stats.turnSpeed * Time.deltaTime
            );
        }

        Vector3 velocity = direction * stats.moveSpeed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity += GRAVITY * Time.deltaTime;
    }

    // ═══════ DETECCIÓN DE ATASCOS - OPTIMIZADA ═══════

    void CheckIfStuck()
    {
        stuckTimer += Time.deltaTime;

        if (stuckTimer >= stuckCheckInterval)
        {
            float movedSqr = (cachedTransform.position - lastPosition).sqrMagnitude;

            // Está atascado
            if (movedSqr < minMoveDistance * minMoveDistance && path != null && currentPathIndex < path.Count)
            {
                ForceRepath();
            }

            lastPosition = cachedTransform.position;
            stuckTimer = 0f;
        }
    }

    void ForceRepath()
    {
        if (localGrid == null || stats == null)
            return;

        if (EnemyManager.Instance != null && !EnemyManager.Instance.CanRepath())
            return;

        Vector2Int start = localGrid.WorldToCell(cachedTransform.position);

        if (followTarget != null)
            targetCell = FindBestFollowCell(followTarget);

        path = pathfinder.FindPath(start, targetCell, stats);

        currentPathIndex = 0;
        repathTimer = REPATH_INTERVAL;
    }

    // ═══════ CONSULTAS ═══════

    public bool HasReachedDestination()
    {
        return path == null || currentPathIndex >= path.Count;
    }

    // ═══════ GIZMOS ═══════

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (localGrid == null || path == null || path.Count == 0)
            return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 p = path[i] + Vector3.up * 0.2f;
            Gizmos.DrawSphere(p, 0.15f);

            if (i > 0)
                Gizmos.DrawLine(path[i - 1] + Vector3.up * 0.2f, p);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(
            localGrid.CellToWorld(targetCell) + Vector3.up * 0.3f,
            0.3f
        );

        if (followTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, followTarget.position);
        }
    }
#endif
}