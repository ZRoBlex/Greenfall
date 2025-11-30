using UnityEngine;

public class ChaseState : State<EnemyController>
{
    Transform target;
    public ChaseState(Transform t) { target = t; }

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

        if (owner.stats.cannibalType == CannibalType.Passive)
        {
            owner.movement.RotateTowards(target.position);

            if (dist < owner.stats.passiveSafeDistance)
            {
                Vector3 retreat = (owner.transform.position - target.position).normalized;
                owner.movement.MoveDirection(retreat, owner.stats.passiveRetreatSpeed);
                return;
            }
            else
            {
                owner.movement.StopInstantly();
                return;
            }
        }

        float speed = owner.instanceOverrides != null
    ? owner.instanceOverrides.GetRunSpeed(owner.stats.runSpeed)
    : owner.stats.runSpeed;


        owner.movement.MoveTowards(target.position, speed);

        if (dist <= owner.stats.attackRange)
            owner.ChangeState(new AttackState(target));
    }
}
