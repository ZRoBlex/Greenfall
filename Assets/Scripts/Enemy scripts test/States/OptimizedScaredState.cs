using UnityEngine;

/// <summary>
/// Optimized Scared State:
/// - Flee from player
/// - Squared distances (no sqrt)
/// - Efficient direction calculations
/// - Smart repath timing
/// </summary>
public class OptimizedScaredState : State<OptimizedEnemyController>
{
    private float repathTimer;
    private bool isMovingAway;

    // Cached squared distance
    private float safeDistanceSqr;

    public override void Enter(OptimizedEnemyController o)
    {
        if (o == null) return;

        repathTimer = 0f;
        isMovingAway = false;

        // Cache squared distance
        safeDistanceSqr = o.stats.scaredSafeDistanceSqr;

        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.ResetSpecialBools();
            o.AnimatorBridge.SetBool("IsScared", true);
        }

        // Don't rotate towards movement while scared
        if (o.Motor != null)
        {
            o.Motor.rotateTowardsMovement = false;
        }

        LookAtPlayer(o);
    }

    public override void Exit(OptimizedEnemyController o)
    {
        if (o != null && o.Motor != null)
        {
            o.Motor.rotateTowardsMovement = true;
        }
    }

    public override void Tick(OptimizedEnemyController o)
    {
        if (o == null || o.Motor == null || o.Perception == null)
            return;

        Transform player = o.Perception.CurrentTarget;

        // Lost player - go back to wander
        if (player == null)
        {
            o.FSM.ChangeState(new OptimizedWanderState());
            return;
        }

        LookAtPlayer(o);

        // Calculate distance (squared)
        Vector3 toPlayer = player.position - o.transform.position;
        float distSqr = toPlayer.sqrMagnitude;

        // Already at safe distance
        if (distSqr >= safeDistanceSqr)
        {
            if (isMovingAway)
            {
                isMovingAway = false;

                if (o.AnimatorBridge != null)
                {
                    o.AnimatorBridge.SetBool("IsWalking", false);
                    o.AnimatorBridge.SetBool("IsScared", true);
                }
            }

            // Stay idle and watch player
            return;
        }

        // Update repath timer
        repathTimer -= Time.deltaTime;

        // Force immediate repath if reached destination
        if (o.Motor.HasReachedDestination())
        {
            repathTimer = 0f;
        }

        // Time to recalculate flee path
        if (repathTimer <= 0f)
        {
            if (!isMovingAway)
            {
                isMovingAway = true;

                if (o.AnimatorBridge != null)
                {
                    o.AnimatorBridge.SetBool("IsScared", false);
                    o.AnimatorBridge.SetBool("IsWalking", true);
                }
            }

            MoveAway(o, player);
            repathTimer = o.stats.scaredRepathTime;
        }
    }

    /// <summary>
    /// Look at player (backward facing while fleeing)
    /// </summary>
    private void LookAtPlayer(OptimizedEnemyController o)
    {
        Transform player = o.Perception.CurrentTarget;
        if (player == null) return;

        Vector3 direction = player.position - o.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            o.transform.rotation = Quaternion.Slerp(
                o.transform.rotation,
                targetRotation,
                o.stats.turnSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// Calculate best flee position away from player
    /// </summary>
    private void MoveAway(OptimizedEnemyController o, Transform player)
    {
        var grid = o.LocalGrid;
        if (grid == null) return;

        Vector2Int myCell = grid.WorldToCell(o.transform.position);
        Vector2Int playerCell = grid.WorldToCell(player.position);

        Vector2Int awayDirInt = myCell - playerCell;

        Vector2Int bestCell = myCell;
        float bestScore = float.MinValue;

        int searchRadius = o.stats.scaredSearchRadius;

        // Search for best flee position
        for (int x = -searchRadius; x <= searchRadius; x++)
        {
            for (int y = -searchRadius; y <= searchRadius; y++)
            {
                Vector2Int candidate = myCell + new Vector2Int(x, y);

                if (!grid.IsWalkable(candidate))
                    continue;

                // Calculate score based on:
                // 1. Direction away from player (dot product)
                // 2. Distance from player
                Vector2Int dirToCandidate = candidate - myCell;
                Vector2 awayDir = new Vector2(awayDirInt.x, awayDirInt.y).normalized;
                Vector2 dirCandidateF = new Vector2(dirToCandidate.x, dirToCandidate.y).normalized;

                float dot = Vector2.Dot(awayDir, dirCandidateF);
                float distToPlayer = Vector2Int.Distance(candidate, playerCell);

                // Weight: prefer direction over pure distance
                float score = dot * 2f + distToPlayer;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                }
            }
        }

        o.Motor.SetDestination(bestCell);
    }
}
