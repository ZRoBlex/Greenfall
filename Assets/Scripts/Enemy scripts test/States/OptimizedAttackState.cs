using UnityEngine;

/// <summary>
/// Optimized Attack State:
/// - STOPS motor to attack in place
/// - Squared distances (no sqrt)
/// - Flags to avoid redundant animator calls
/// - Clean attack cooldown management
/// </summary>
public class OptimizedAttackState : State<OptimizedEnemyController>
{
    private bool hasSetAnimation = false;
    private bool hasStopped = false;

    public override void Enter(OptimizedEnemyController o)
    {
        if (o == null) return;

        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.ResetSpecialBools();
            o.AnimatorBridge.SetBool("IsChasing", true);
        }

        hasSetAnimation = true;
        hasStopped = false;

        // 🔥 CRITICAL: Stop the motor immediately
        if (o.Motor != null && o.LocalGrid != null)
        {
            Vector2Int currentCell = o.LocalGrid.WorldToCell(o.transform.position);
            o.Motor.SetDestination(currentCell);
            hasStopped = true;
        }
    }

    public override void Tick(OptimizedEnemyController o)
    {
        if (o == null || o.Motor == null || o.Perception == null)
            return;

        Transform player = o.Perception.CurrentTarget;

        // Lost player
        if (player == null)
        {
            if (o.CurrentType == CannibalType.Passive)
            {
                o.FSM.ChangeState(new OptimizedScaredState());
            }
            else
            {
                o.FSM.ChangeState(new OptimizedFollowingState());
            }
            return;
        }

        // Check distance (squared to avoid sqrt)
        float distSqr = (o.transform.position - player.position).sqrMagnitude;

        // Too far - switch to following
        if (distSqr > o.stats.attackRangeSqr)
        {
            o.FSM.ChangeState(new OptimizedFollowingState());
            return;
        }

        // 🔥 Make sure motor stays stopped
        if (!hasStopped && o.Motor != null && o.LocalGrid != null)
        {
            Vector2Int currentCell = o.LocalGrid.WorldToCell(o.transform.position);
            o.Motor.SetDestination(currentCell);
            hasStopped = true;
        }

        // Look at player while attacking
        LookAtPlayer(o, player);

        // Attack if cooldown ready
        if (o.attackCooldownTimer <= 0f)
        {
            PerformAttack(o, player);
            o.attackCooldownTimer = o.stats.attackCooldown;
        }
    }

    public override void Exit(OptimizedEnemyController o)
    {
        // Clean exit
    }

    /// <summary>
    /// Smooth rotation toward player
    /// </summary>
    private void LookAtPlayer(OptimizedEnemyController o, Transform player)
    {
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
    /// Perform attack on target (Player OR Passive Enemy)
    /// </summary>
    private void PerformAttack(OptimizedEnemyController o, Transform target)
    {
        // Check if this enemy can deal damage
        if (!o.stats.canDealDamage)
        {
            Debug.Log($"[{o.stats.displayName}] tried to attack but is friendly.");
            return;
        }

        // 🔥 TRY 1: Attack Player
        PlayerHealth playerHealth = target.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(o.stats.attackDamage);
            Debug.Log($"[{o.stats.displayName}] Attacked PLAYER dealing {o.stats.attackDamage} damage.");

            // Play attack animation
            if (o.AnimatorBridge != null)
            {
                o.AnimatorBridge.SetTrigger("Attack");
            }

            return;
        }

        // 🔥 TRY 2: Attack Enemy (Passive enemies)
        Health enemyHealth = target.GetComponentInParent<Health>();
        if (enemyHealth != null)
        {
            enemyHealth.ApplyDamage(o.stats.attackDamage);
            Debug.Log($"[{o.stats.displayName}] Attacked ENEMY {target.name} dealing {o.stats.attackDamage} damage.");

            // Play attack animation
            if (o.AnimatorBridge != null)
            {
                o.AnimatorBridge.SetTrigger("Attack");
            }

            return;
        }

        // No valid target found
        Debug.LogWarning($"[{o.stats.displayName}] No Health component found on {target.name} or parents.");
    }
}