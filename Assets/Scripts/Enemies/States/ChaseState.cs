using UnityEngine;

public class ChaseState : State<EnemyController>
{
    Transform target;

    public ChaseState(Transform t)
    {
        target = t;
    }

    public override void Tick(EnemyController owner)
    {
        if (target == null)
        {
            owner.ChangeState(new WanderState());
            return;
        }

        if (!owner.perception.HasDetectedTarget(out Transform seen) || seen != target)
        {
            owner.ChangeState(new WanderState());
            return;
        }

        float dist = Vector3.Distance(owner.transform.position, target.position);

        var priority = owner.GetComponent<MovementPriorityController>();

        // Passive → no persigue
        if (owner.stats.cannibalType == CannibalType.Passive)
        {
            owner.movement.RotateTowards(target.position);
            if (dist < owner.stats.passiveSafeDistance)
            {
                Vector3 retreat = (owner.transform.position - target.position).normalized;
                owner.movement.MoveDirection(retreat, owner.stats.passiveRetreatSpeed);
            }
            else
            {
                priority.Stop();
            }
            return;
        }

        // Aggressive → path hacia el jugador
        priority.MoveTo(target.position, MovementMode.Chase);

        // ataque
        if (dist <= owner.stats.attackRange)
            owner.ChangeState(new AttackState(target));
    }
}
