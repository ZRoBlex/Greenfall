using UnityEngine;

public class AttackState : State<EnemyController>
{
    Transform target;
    public AttackState(Transform t) { target = t; }

    public override void Enter(EnemyController owner)
    {
        // entrar a anim de ataque si se desea
    }

    public override void Tick(EnemyController owner)
    {
        if (target == null) { owner.ChangeState(new WanderState()); return; }

        owner.movement.RotateTowards(target.position);

        float dist = Vector3.Distance(owner.transform.position, target.position);
        if (dist > owner.stats.attackRange + 0.5f)
        {
            owner.ChangeState(new ChaseState(target));
            return;
        }

        // tratar de atacar
        owner.combat.TryMeleeAttack(target);
    }
}
