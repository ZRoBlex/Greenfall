using UnityEngine;

/// <summary>
/// Optimized Wander State:
/// - Random wandering behavior
/// - Efficient destination picking
/// - Smooth transitions to Looking state
/// </summary>
public class OptimizedWanderState : State<OptimizedEnemyController>
{
    private float waitTimer = 0f;
    private float waitDuration;

    public override void Enter(OptimizedEnemyController o)
    {
        if (o == null) return;

        PickNewDestination(o);

        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.ResetSpecialBools();
            // IsWalking is set by motor
        }

        waitTimer = 0f;
        waitDuration = o.stats.wanderWaitTime;
    }

    public override void Tick(OptimizedEnemyController o)
    {
        if (o == null || o.Motor == null)
            return;

        // Check if reached destination
        if (o.Motor.HasReachedDestination())
        {
            waitTimer += Time.deltaTime;

            if (o.AnimatorBridge != null)
            {
                o.AnimatorBridge.SetBool("IsIdle", true);
            }

            // Waited long enough - transition to looking
            if (waitTimer >= waitDuration)
            {
                o.FSM.ChangeState(new OptimizedLookingState());
            }
        }
    }

    public override void Exit(OptimizedEnemyController o)
    {
        if (o != null && o.AnimatorBridge != null)
        {
            o.AnimatorBridge.SetBool("IsIdle", false);
        }
    }

    /// <summary>
    /// Pick random walkable destination within wander radius
    /// </summary>
    private void PickNewDestination(OptimizedEnemyController o)
    {
        if (o.LocalGrid == null)
            return;

        Vector2Int center = o.LocalGrid.WorldToCell(o.transform.position);

        // Try up to 10 times to find valid position
        for (int i = 0; i < 10; i++)
        {
            Vector2 rand = Random.insideUnitCircle * o.stats.wanderRadius;
            Vector2Int offset = new Vector2Int(
                Mathf.RoundToInt(rand.x),
                Mathf.RoundToInt(rand.y)
            );

            Vector2Int candidate = center + offset;

            if (o.LocalGrid.IsWalkable(candidate))
            {
                o.Motor.SetDestination(candidate);
                return;
            }
        }

        // Fallback: stay in place
        o.Motor.SetDestination(center);
    }
}
