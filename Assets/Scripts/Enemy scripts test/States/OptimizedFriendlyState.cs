using UnityEngine;

/// <summary>
/// Optimized Friendly State:
/// - Smooth following with distance management
/// - Cached calculations
/// - Minimal state changes
/// </summary>
public class OptimizedFriendlyState : State<OptimizedEnemyController>
{
    private float stopDistance = 2.0f;
    private float stopDistanceSqr;

    private bool isStopped = false;

    public override void Enter(OptimizedEnemyController o)
    {
        if (o == null) return;

        stopDistanceSqr = stopDistance * stopDistance;

        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.ResetSpecialBools();
            o.AnimatorBridge.SetBool("IsWalking", true);
        }

        if (o.Perception.CurrentTarget != null)
        {
            o.Motor.SetTarget(o.Perception.CurrentTarget);
        }

        isStopped = false;
    }

    public override void Tick(OptimizedEnemyController o)
    {
        if (o == null || o.Motor == null || o.Perception == null)
            return;

        Transform player = o.Perception.CurrentTarget;

        // Lost player - return to wander
        if (player == null)
        {
            o.FSM.ChangeState(new OptimizedWanderState());
            return;
        }

        // Calculate distance (squared to avoid sqrt)
        Vector3 toPlayer = player.position - o.transform.position;
        float distSqr = toPlayer.sqrMagnitude;

        // Close enough - stop and look at player
        if (distSqr <= stopDistanceSqr)
        {
            if (!isStopped)
            {
                // Just stopped
                Vector2Int myCell = o.LocalGrid.WorldToCell(o.transform.position);
                o.Motor.SetDestination(myCell);

                if (o.AnimatorBridge != null)
                {
                    o.AnimatorBridge.SetBool("IsWalking", false);
                    o.AnimatorBridge.SetBool("IsIdle", true);
                }

                isStopped = true;
            }

            LookAtPlayer(o, player);
        }
        else
        {
            // Too far - follow player
            if (isStopped)
            {
                // Just started moving
                if (o.AnimatorBridge != null)
                {
                    o.AnimatorBridge.SetBool("IsIdle", false);
                    o.AnimatorBridge.SetBool("IsWalking", true);
                }

                o.Motor.SetTarget(player);
                isStopped = false;
            }
        }
    }

    public override void Exit(OptimizedEnemyController o)
    {
        if (o == null) return;

        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.SetBool("IsWalking", false);
            o.AnimatorBridge.SetBool("IsIdle", false);
        }
    }

    /// <summary>
    /// Smooth rotation toward player
    /// </summary>
    private void LookAtPlayer(OptimizedEnemyController o, Transform player)
    {
        Vector3 dir = player.position - o.transform.position;
        dir.y = 0f;

        float sqrMag = dir.sqrMagnitude;

        if (sqrMag > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            o.transform.rotation = Quaternion.Slerp(
                o.transform.rotation,
                targetRot,
                o.stats.turnSpeed * Time.deltaTime
            );
        }
    }
}
