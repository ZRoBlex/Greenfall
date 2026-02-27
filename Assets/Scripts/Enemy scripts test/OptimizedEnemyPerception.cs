using UnityEngine;

/// <summary>
/// Optimized Enemy Perception:
/// - Detects Player AND Passive enemies
/// - Queued updates through manager
/// - Cached squared distances
/// - Minimal allocations
/// - Early exit optimizations
/// </summary>
public class OptimizedEnemyPerception : MonoBehaviour
{
    public OptimizedEnemyStats stats;

    private Transform player;
    private OptimizedEnemyController controller;

    // Cached results
    public bool CanSeePlayer { get; private set; }
    public bool IsPlayerClose { get; private set; }
    public bool HeardPlayer { get; private set; }
    public Transform CurrentTarget { get; private set; }

    // Update timing
    private float updateTimer = 0f;
    private float updateInterval = 0.2f;

    void Awake()
    {
        controller = GetComponent<OptimizedEnemyController>();

        // Get player from manager (no FindGameObjectWithTag)
        if (OptimizedEnemyManager.Instance != null)
            player = OptimizedEnemyManager.Instance.GetPlayer();
    }

    void Update()
    {
        // Skip if disabled by LOD
        if (!enabled) return;

        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            // Request update through manager queue
            if (OptimizedEnemyManager.Instance != null)
            {
                OptimizedEnemyManager.Instance.RequestPerceptionUpdate(this);
            }
            else
            {
                // Fallback: update directly if no manager
                UpdatePerception();
            }

            updateTimer = 0f;
        }
    }

    /// <summary>
    /// Called by manager when it's this enemy's turn
    /// Detects BOTH player and passive enemies
    /// </summary>
    public void UpdatePerception()
    {
        CurrentTarget = null;

        if (stats == null) return;

        // 🔥 For AGGRESSIVE enemies: detect player AND passive enemies
        if (controller != null && controller.CurrentType == CannibalType.Aggressive)
        {
            // First priority: Player
            if (player != null && CheckTarget(player))
            {
                CurrentTarget = player;
                return;
            }

            // Second priority: Passive enemies
            Transform passiveEnemy = FindNearestPassiveEnemy();
            if (passiveEnemy != null)
            {
                CurrentTarget = passiveEnemy;
                return;
            }
        }
        // 🔥 For OTHER types: only detect player
        else if (player != null && CheckTarget(player))
        {
            CurrentTarget = player;
        }
    }

    /// <summary>
    /// Check if a specific target is detectable
    /// </summary>
    private bool CheckTarget(Transform target)
    {
        if (target == null || stats == null) return false;

        // Early exit: target too far
        Vector3 toTarget = target.position - transform.position;
        float distSqr = toTarget.sqrMagnitude;

        if (distSqr > stats.perceptionRangeSqr)
        {
            CanSeePlayer = false;
            IsPlayerClose = false;
            HeardPlayer = false;
            return false;
        }

        // Check distance detection
        IsPlayerClose = distSqr <= stats.closeDetectionRadiusSqr;

        // Check hearing (60% of perception range)
        float hearingRangeSqr = stats.perceptionRangeSqr * 0.36f; // 0.6^2
        HeardPlayer = distSqr <= hearingRangeSqr;

        // Check vision (FOV cone)
        float dist = Mathf.Sqrt(distSqr); // Only sqrt once
        Vector3 dirToTarget = toTarget / dist; // Normalized without allocating

        float angle = Vector3.Angle(transform.forward, dirToTarget);
        CanSeePlayer = angle <= stats.halfFOV;

        // Detected if seen, close, or heard
        return (CanSeePlayer || IsPlayerClose || HeardPlayer);
    }

    /// <summary>
    /// Find nearest passive enemy within perception range
    /// Only for AGGRESSIVE enemies
    /// </summary>
    private Transform FindNearestPassiveEnemy()
    {
        if (OptimizedEnemyManager.Instance == null) return null;

        // Get all enemies near this position using spatial hash
        var nearbyEnemies = new System.Collections.Generic.List<Transform>();
        OptimizedEnemyManager.Instance.GetEnemiesNear(
            transform.position,
            stats.perceptionRange,
            nearbyEnemies
        );

        Transform nearestPassive = null;
        float nearestDistSqr = stats.perceptionRangeSqr;

        foreach (var enemyTransform in nearbyEnemies)
        {
            if (enemyTransform == transform) continue; // Skip self

            var enemyController = enemyTransform.GetComponent<OptimizedEnemyController>();
            if (enemyController == null) continue;

            // 🔥 Only target PASSIVE enemies
            if (enemyController.CurrentType != CannibalType.Passive) continue;

            // Check if in range and visible
            float distSqr = (enemyTransform.position - transform.position).sqrMagnitude;

            if (distSqr < nearestDistSqr)
            {
                // Check FOV
                Vector3 toEnemy = enemyTransform.position - transform.position;
                float angle = Vector3.Angle(transform.forward, toEnemy.normalized);

                if (angle <= stats.halfFOV)
                {
                    nearestDistSqr = distSqr;
                    nearestPassive = enemyTransform;
                }
            }
        }

        return nearestPassive;
    }

    /// <summary>
    /// Set external target (for AI states)
    /// </summary>
    public void SetExternalTarget(Transform target)
    {
        CurrentTarget = target;
    }

    /// <summary>
    /// Clear current target
    /// </summary>
    public void ClearTarget()
    {
        CurrentTarget = null;
        CanSeePlayer = false;
        IsPlayerClose = false;
        HeardPlayer = false;
    }

    /// <summary>
    /// Adjust update interval based on LOD
    /// </summary>
    public void SetUpdateInterval(float interval)
    {
        updateInterval = interval;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (stats == null) return;

        // Close detection (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stats.closeDetectionRadius);

        // Hearing range (blue)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, stats.perceptionRange * 0.6f);

        // Vision range (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stats.perceptionRange);

        // FOV cone (green)
        Vector3 left = Quaternion.Euler(0, -stats.halfFOV, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, stats.halfFOV, 0) * transform.forward;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + left * stats.perceptionRange);
        Gizmos.DrawLine(transform.position, transform.position + right * stats.perceptionRange);

        // Current target (magenta)
        if (CurrentTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, CurrentTarget.position);

            // Draw sphere at target
            Gizmos.DrawWireSphere(CurrentTarget.position, 0.5f);
        }
    }
#endif
}