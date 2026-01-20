using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(GridPathfinder), typeof(CharacterController))]
public class EnemyMotor : MonoBehaviour
{
    public EnemyStats stats;

    GridPathfinder pathfinder;
    CharacterController controller;

    List<Vector3> path;
    int index;

    Transform target;
    Vector2Int targetCell;

    float repathTimer;
    float verticalVelocity;

    const float GRAVITY = -20f;
    const float REPATH_INTERVAL = 2f;

    public EnemyLocalGrid localGrid;
    public bool rotateTowardsMovement = true;

    [Header("Repath Optimization")]
    public float stuckCheckInterval = 0.5f;
    public float minMoveDistance = 0.1f;
    public LayerMask obstacleMask;

    float stuckTimer;
    Vector3 lastPosition;


    // 🔹 Suavizado de dirección (NO de velocidad)
    Vector3 smoothDirection;

    void Awake()
    {
        pathfinder = GetComponent<GridPathfinder>();
        controller = GetComponent<CharacterController>();
        localGrid = GetComponent<EnemyLocalGrid>();

        lastPosition = transform.position;


        if (localGrid == null)
            Debug.LogError($"[EnemyMotor] Missing EnemyLocalGrid on {name}");

        if (stats == null)
            Debug.LogError($"[EnemyMotor] Missing EnemyStats on {name}");
    }

    public void SetTarget(Transform t)
    {
        target = t;
        repathTimer = 0f;
    }

    public void SetDestination(Vector2Int cell)
    {
        target = null;
        targetCell = cell;
        RecalculatePath();
    }

    void Update()
    {
        HandleRepath();
        MoveAlongPath();
        ApplyGravity();
        CheckIfStuckOrBlocked();
    }


    /* ===================== PATH ===================== */

    void HandleRepath()
    {
        repathTimer -= Time.deltaTime;

        if (target != null && repathTimer <= 0f)
        {
            targetCell = FindBestFollowCell(target);
            RecalculatePath();
        }
    }

    void RecalculatePath()
    {
        if (localGrid == null || stats == null)
            return;

        // 🔹 Limitar repaths globales
        if (EnemyManager.Instance != null &&
            !EnemyManager.Instance.CanRepath())
            return;

        Vector2Int start = localGrid.WorldToCell(transform.position);
        path = pathfinder.FindPath(start, targetCell, stats);

        index = 0;
        repathTimer = REPATH_INTERVAL;
    }


    /* ===================== MOVEMENT ===================== */

    void MoveAlongPath()
    {
        if (path == null || index >= path.Count)
            return;

        Vector3 targetPos = path[index];
        Vector3 flatTarget = new Vector3(targetPos.x, transform.position.y, targetPos.z);

        Vector3 toTarget = flatTarget - transform.position;
        float distance = toTarget.magnitude;

        // 🔹 Umbral un poco mayor para evitar micro-zigzag
        if (distance < 0.35f)
        {
            index++;
            return;
        }

        Vector3 desiredDirection = toTarget.normalized;

        // 🔹 Suavizar SOLO la dirección (no la velocidad)
        smoothDirection = Vector3.Slerp(
            smoothDirection == Vector3.zero ? desiredDirection : smoothDirection,
            desiredDirection,
            Time.deltaTime * stats.turnSpeed
        );

        Vector3 direction = smoothDirection.normalized;

        // 🔹 Rotación suave hacia la dirección de movimiento
        if (rotateTowardsMovement && direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                stats.turnSpeed * Time.deltaTime
            );
        }

        // 🔹 Velocidad fija, sin interpolar (sin deslizamiento)
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

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (localGrid == null)
            localGrid = GetComponent<EnemyLocalGrid>();

        if (localGrid == null)
            return;

        if (path == null || path.Count == 0)
            return;

        // ===== PATH =====
        Gizmos.color = Color.cyan;
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 p = path[i] + Vector3.up * 0.2f;
            Gizmos.DrawSphere(p, 0.15f);

            if (i > 0)
                Gizmos.DrawLine(
                    path[i - 1] + Vector3.up * 0.2f,
                    p
                );
        }

        // ===== DESTINO =====
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(
            localGrid.CellToWorld(targetCell) + Vector3.up * 0.3f,
            0.3f
        );

        if (target != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(
                transform.position,
                target.position
            );
        }
    }
#endif

    Vector2Int FindBestFollowCell(Transform target)
    {
        Vector2Int targetCenter =
            localGrid.WorldToCell(target.position);

        Vector2Int myCell =
            localGrid.WorldToCell(transform.position);

        Vector2Int bestCell = myCell;
        float bestScore = float.MaxValue;

        for (int x = -stats.followMaxDistance; x <= stats.followMaxDistance; x++)
        {
            for (int y = -stats.followMaxDistance; y <= stats.followMaxDistance; y++)
            {
                Vector2Int candidate = targetCenter + new Vector2Int(x, y);

                int distToTarget =
                    Mathf.Abs(x) + Mathf.Abs(y);

                if (distToTarget < stats.followMinDistance)
                    continue;

                if (!localGrid.IsWalkable(candidate))
                    continue;

                float score =
                    Vector2Int.Distance(candidate, myCell);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                }
            }
        }

        return bestCell;
    }

    public bool HasReachedDestination()
    {
        return path == null || index >= path.Count;
    }

    void CheckIfStuckOrBlocked()
    {
        // =========================
        // 1. DETECTOR DE ATASCADO
        // =========================

        stuckTimer += Time.deltaTime;

        if (stuckTimer >= stuckCheckInterval)
        {
            float moved = Vector3.Distance(transform.position, lastPosition);

            if (moved < minMoveDistance)
            {
                // 🔹 Está atascado → forzar repath
                ForceRepath();
            }

            lastPosition = transform.position;
            stuckTimer = 0f;
        }

        // =========================
        // 2. DETECTOR DE OBSTÁCULO DELANTE
        // =========================

        if (path == null || index >= path.Count)
            return;

        Vector3 nextPoint = path[index];
        Vector3 flatNext = new Vector3(nextPoint.x, transform.position.y, nextPoint.z);

        Vector3 dir = (flatNext - transform.position).normalized;

        float checkDistance = 0.6f; // corto, barato

        if (Physics.Raycast(
                transform.position + Vector3.up * 0.5f,
                dir,
                out RaycastHit hit,
                checkDistance,
                obstacleMask,
                QueryTriggerInteraction.Ignore))
        {
            // 🔹 Algo nuevo bloquea el camino
            ForceRepath();
        }
    }

    void ForceRepath()
    {
        if (localGrid == null || stats == null)
            return;

        if (EnemyManager.Instance != null &&
            !EnemyManager.Instance.CanRepath())
            return;

        Vector2Int start = localGrid.WorldToCell(transform.position);

        if (target != null)
            targetCell = FindBestFollowCell(target);

        path = pathfinder.FindPath(start, targetCell, stats);

        index = 0;
        repathTimer = REPATH_INTERVAL;
    }



}
