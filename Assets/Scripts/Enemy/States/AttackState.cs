using UnityEngine;

/// <summary>
/// Estado de ataque - ULTRA OPTIMIZADO
/// </summary>
public class AttackState : State<EnemyController>
{
    // Cache
    Transform cachedTransform;
    Transform cachedTarget;
    float attackRangeSqr;

    PlayerHealth playerHealth;
    bool hasValidTarget;

    public override void Enter(EnemyController o)
    {
        if (o == null) return;

        cachedTransform = o.transform;

        if (o.stats != null)
            attackRangeSqr = o.stats.attackRange * o.stats.attackRange;

        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.ResetSpecialBools();
            o.AnimatorBridge.SetBool("IsChasing", true);
        }

        // Cache player health una sola vez
        if (o.Perception != null && o.Perception.CurrentTarget != null)
        {
            cachedTarget = o.Perception.CurrentTarget;
            playerHealth = cachedTarget.GetComponentInParent<PlayerHealth>();
            hasValidTarget = playerHealth != null;
        }
        else
        {
            hasValidTarget = false;
        }
    }

    public override void Tick(EnemyController o)
    {
        if (o == null || cachedTransform == null) return;

        // Validar target
        if (cachedTarget == null || !hasValidTarget)
        {
            o.FSM.ChangeState(GetFallbackState(o));
            return;
        }

        // Rotar hacia el jugador
        LookAtTarget(o, cachedTarget);

        // 🔥 Usar squared magnitude (más rápido que Distance)
        float distSqr = (cachedTransform.position - cachedTarget.position).sqrMagnitude;

        // Muy lejos → volver a perseguir
        if (distSqr > attackRangeSqr)
        {
            o.FSM.ChangeState(new FollowingState());
            return;
        }

        // Atacar si el cooldown terminó
        if (o.attackCooldownTimer <= 0f)
        {
            PerformAttack(o);
            o.attackCooldownTimer = o.stats != null ? o.stats.attackCooldown : 1f;
        }
    }

    void LookAtTarget(EnemyController o, Transform target)
    {
        if (o.stats == null) return;

        Vector3 direction = target.position - cachedTransform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            cachedTransform.rotation = Quaternion.Slerp(
                cachedTransform.rotation,
                targetRotation,
                o.stats.turnSpeed * Time.deltaTime
            );
        }
    }

    void PerformAttack(EnemyController o)
    {
        if (o.stats == null || !o.stats.canDealDamage)
            return;

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(o.stats.attackDamage);
        }
    }

    State<EnemyController> GetFallbackState(EnemyController o)
    {
        if (o.CurrentType == CannibalType.Passive)
            return new ScaredState();
        else
            return new FollowingState();
    }
}