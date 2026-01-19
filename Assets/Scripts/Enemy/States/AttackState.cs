using UnityEngine;

public class AttackState : State<EnemyController>
{
    float attackTimer;

    public override void Enter(EnemyController o)
    {
        attackTimer = 0f;
        Debug.Log($"[{o.stats.displayName}] Entró en AttackState.");
    }

    public override void Tick(EnemyController o)
    {
        if (o == null || o.Motor == null || o.Perception == null)
            return;

        Transform player = o.Perception.CurrentTarget;

        if (player == null)
        {
            if (o.CurrentType == CannibalType.Passive)
                o.FSM.ChangeState(new ScaredState());
            else
                o.FSM.ChangeState(new FollowingState());

            return;
        }

        LookAtPlayer(o, player);

        float distance = Vector3.Distance(o.transform.position, player.position);

        if (distance > o.stats.attackRange)
        {
            o.FSM.ChangeState(new FollowingState());
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            PerformAttack(o, player);
            attackTimer = o.stats.attackCooldown;
        }
    }

    void LookAtPlayer(EnemyController o, Transform player)
    {
        Vector3 direction = player.position - o.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            o.transform.rotation = Quaternion.Slerp(o.transform.rotation, targetRotation, o.stats.turnSpeed * Time.deltaTime);
        }
    }

    void PerformAttack(EnemyController o, Transform player)
    {
        Debug.Log($"[{o.stats.displayName}] Atacó al jugador causando {o.stats.attackDamage} de daño.");
    }
}
