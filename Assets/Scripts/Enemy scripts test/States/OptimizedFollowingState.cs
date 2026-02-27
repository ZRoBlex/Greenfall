using UnityEngine;

/// <summary>
/// Optimized Following State:
/// - Continuously updates target position
/// - Can follow Player or Passive enemies
/// - Smooth pursuit with distance checks
/// - Transitions to Attack when in range
/// </summary>
public class OptimizedFollowingState : State<OptimizedEnemyController>
{
    private bool hasSetAnimation = false;
    private float updateTargetTimer = 0f;
    private const float UPDATE_TARGET_INTERVAL = 0.1f; // Update target every 0.1s

    public override void Enter(OptimizedEnemyController o)
    {
        if (o == null) return;

        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.ResetSpecialBools();
            o.AnimatorBridge.SetBool("IsChasing", true);
            o.AnimatorBridge.SetBool("IsWalking", true);
        }

        hasSetAnimation = true;
        updateTargetTimer = 0f;

        // 🔥 CRITICAL: Set target immediately
        if (o.Perception.CurrentTarget != null && o.Motor != null)
        {
            o.Motor.SetTarget(o.Perception.CurrentTarget);
            Debug.Log($"[{o.stats.displayName}] Started following {o.Perception.CurrentTarget.name}");
        }
    }

    public override void Tick(OptimizedEnemyController o)
    {
        if (o == null || o.Motor == null || o.Perception == null)
            return;

        Transform target = o.Perception.CurrentTarget;

        // Lost target - return to looking/wander
        if (target == null)
        {
            o.FSM.ChangeState(new OptimizedLookingState());
            return;
        }

        // 🔥 CRITICAL: Continuously update target position
        updateTargetTimer += Time.deltaTime;
        if (updateTargetTimer >= UPDATE_TARGET_INTERVAL)
        {
            o.Motor.SetTarget(target);
            updateTargetTimer = 0f;
        }

        // Check distance to target (squared to avoid sqrt)
        float distSqr = (o.transform.position - target.position).sqrMagnitude;

        // 🔥 Close enough to attack - switch to AttackState
        if (distSqr <= o.stats.attackRangeSqr)
        {
            o.FSM.ChangeState(new OptimizedAttackState());
            return;
        }

        // 🔥 Target too far - lost track
        if (distSqr > o.stats.perceptionRangeSqr * 2f) // Double perception range
        {
            o.FSM.ChangeState(new OptimizedLookingState());
            return;
        }
    }

    public override void Exit(OptimizedEnemyController o)
    {
        if (o == null) return;

        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.SetBool("IsChasing", false);
        }
    }
}