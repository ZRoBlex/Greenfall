using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Optimized Enemy Motor:
/// - Smooth movement without stuttering
/// - Queued pathfinding through manager
/// - Velocity-based movement (no position snapping)
/// - Efficient stuck detection
/// </summary>
[RequireComponent(typeof(OptimizedGridPathfinder), typeof(CharacterController))]
public class OptimizedEnemyMotor : MonoBehaviour
{
    public OptimizedEnemyStats stats;

    private OptimizedGridPathfinder pathfinder;
    private CharacterController controller;
    private EnemyLocalGrid localGrid;

    // Movement state
    private List<Vector3> path;
    private int pathIndex;
    private Vector3 currentVelocity;
    private Vector3 targetDirection;

    // Target tracking
    private Transform followTarget;
    private Vector2Int destinationCell;

    // Timing
    private float repathTimer;
    private const float REPATH_INTERVAL = 2f;

    // Gravity
    private float verticalVelocity;
    private const float GRAVITY = -20f;

    // Stuck detection
    private float stuckTimer;
    private Vector3 lastPosition;
    private const float STUCK_CHECK_INTERVAL = 0.5f;
    private const float MIN_MOVE_DISTANCE = 0.1f;

    // Smoothing
    public float acceleration = 10f;
    public float deceleration = 15f;
    public bool rotateTowardsMovement = true;

    // State
    public bool IsMoving { get; private set; }

    void Awake()
    {
        pathfinder = GetComponent<OptimizedGridPathfinder>();
        controller = GetComponent<CharacterController>();
        localGrid = GetComponent<EnemyLocalGrid>();

        if (stats != null)
            stats.CacheExpensiveValues();

        lastPosition = transform.position;
    }

    void Update()
    {
        if (!enabled) return;

        HandleRepathTimer();
        MoveAlongPath();
        ApplyGravity();
        CheckIfStuck();
    }

    /// <summary>
    /// Set a transform to follow
    /// </summary>
    public void SetTarget(Transform target)
    {
        followTarget = target;
        repathTimer = REPATH_INTERVAL; // Reset interval

        // 🔥 CRITICAL: Calculate path IMMEDIATELY
        if (followTarget != null && localGrid != null)
        {
            destinationCell = FindBestFollowCell(followTarget);
            RequestPath();
        }
    }

    /// <summary>
    /// Set a specific destination
    /// </summary>
    public void SetDestination(Vector2Int cell)
    {
        followTarget = null;
        destinationCell = cell;
        RequestPath();
    }

    /// <summary>
    /// Stop movement
    /// </summary>
    public void Stop()
    {
        followTarget = null;
        path = null;
        pathIndex = 0;
        currentVelocity = Vector3.zero;
        IsMoving = false;
    }

    /// <summary>
    /// Check if reached destination
    /// </summary>
    public bool HasReachedDestination()
    {
        return path == null || pathIndex >= path.Count;
    }

    /// <summary>
    /// Handle repath timing for following targets
    /// </summary>
    private void HandleRepathTimer()
    {
        if (followTarget == null) return;

        repathTimer -= Time.deltaTime;

        if (repathTimer <= 0f)
        {
            destinationCell = FindBestFollowCell(followTarget);
            RequestPath();
        }
    }

    /// <summary>
    /// Request pathfinding through manager queue
    /// </summary>
    private void RequestPath()
    {
        if (localGrid == null || stats == null) return;

        Vector2Int start = localGrid.WorldToCell(transform.position);
        Vector2Int goal = destinationCell;

        // Queue pathfinding request
        if (OptimizedEnemyManager.Instance != null)
        {
            OptimizedEnemyManager.Instance.RequestPathfinding(() =>
            {
                CalculatePath(start, goal);
            });
        }
        else
        {
            // Fallback: calculate immediately
            CalculatePath(start, goal);
        }

        repathTimer = REPATH_INTERVAL;
    }

    /// <summary>
    /// Calculate path (called from queue)
    /// </summary>
    private void CalculatePath(Vector2Int start, Vector2Int goal)
    {
        if (pathfinder == null) return;

        path = pathfinder.FindPath(start, goal);
        pathIndex = 0;
    }

    /// <summary>
    /// Smooth movement along path
    /// </summary>
    private void MoveAlongPath()
    {
        if (path == null || pathIndex >= path.Count)
        {
            // Decelerate to stop
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, deceleration * Time.deltaTime);
            IsMoving = currentVelocity.sqrMagnitude > 0.01f;

            if (IsMoving)
            {
                ApplyMovement();
            }

            return;
        }

        Vector3 targetPos = path[pathIndex];
        Vector3 flatTarget = new Vector3(targetPos.x, transform.position.y, targetPos.z);

        Vector3 toTarget = flatTarget - transform.position;
        float distSqr = toTarget.sqrMagnitude;

        // Reached waypoint
        if (distSqr < stats.stopDistance * stats.stopDistance)
        {
            pathIndex++;
            return;
        }

        // Calculate desired direction
        float dist = Mathf.Sqrt(distSqr);
        targetDirection = toTarget / dist;

        // Smooth acceleration toward target direction
        Vector3 desiredVelocity = targetDirection * stats.moveSpeed;
        currentVelocity = Vector3.MoveTowards(currentVelocity, desiredVelocity, acceleration * Time.deltaTime);

        IsMoving = true;

        // Rotate toward movement direction
        if (rotateTowardsMovement && currentVelocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(currentVelocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, stats.turnSpeed * Time.deltaTime);
        }

        ApplyMovement();
    }

    /// <summary>
    /// Apply final movement to CharacterController
    /// </summary>
    private void ApplyMovement()
    {
        Vector3 move = currentVelocity;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }

    /// <summary>
    /// Apply gravity
    /// </summary>
    private void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += GRAVITY * Time.deltaTime;
        }
    }

    /// <summary>
    /// Check if stuck and force repath
    /// </summary>
    private void CheckIfStuck()
    {
        stuckTimer += Time.deltaTime;

        if (stuckTimer >= STUCK_CHECK_INTERVAL)
        {
            float moved = Vector3.Distance(transform.position, lastPosition);

            if (moved < MIN_MOVE_DISTANCE && IsMoving)
            {
                // Stuck - force repath
                RequestPath();
            }

            lastPosition = transform.position;
            stuckTimer = 0f;
        }
    }

    /// <summary>
    /// Find best cell to follow target
    /// </summary>
    private Vector2Int FindBestFollowCell(Transform target)
    {
        if (localGrid == null || stats == null) return Vector2Int.zero;

        Vector2Int targetCenter = localGrid.WorldToCell(target.position);
        Vector2Int myCell = localGrid.WorldToCell(transform.position);

        Vector2Int bestCell = myCell;
        float bestScore = float.MaxValue;

        int minDist = stats.followMinDistance;
        int maxDist = stats.followMaxDistance;

        for (int x = -maxDist; x <= maxDist; x++)
        {
            for (int y = -maxDist; y <= maxDist; y++)
            {
                int distToTarget = Mathf.Abs(x) + Mathf.Abs(y);

                if (distToTarget < minDist || distToTarget > maxDist)
                    continue;

                Vector2Int candidate = targetCenter + new Vector2Int(x, y);

                if (!localGrid.IsWalkable(candidate))
                    continue;

                float score = Vector2Int.Distance(candidate, myCell);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                }
            }
        }

        return bestCell;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (path == null || path.Count == 0) return;

        // Draw path
        Gizmos.color = Color.cyan;
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 p = path[i] + Vector3.up * 0.2f;
            Gizmos.DrawSphere(p, 0.15f);

            if (i > 0)
            {
                Gizmos.DrawLine(path[i - 1] + Vector3.up * 0.2f, p);
            }
        }

        // Draw destination
        if (localGrid != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(localGrid.CellToWorld(destinationCell) + Vector3.up * 0.3f, 0.3f);
        }

        // Draw target line
        if (followTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, followTarget.position);
        }
    }
#endif
}