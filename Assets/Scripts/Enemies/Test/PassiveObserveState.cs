using UnityEngine;

public class PassiveObserveState : State<EnemyController>
{
    Transform player;

    public PassiveObserveState(Transform target)
    {
        player = target;
    }

    public override void Enter(EnemyController owner)
    {
        if (owner.animatorBridge)
            owner.animatorBridge.SetBool("IsScared", true);

        owner.debugStateName = "PassiveObserve";
    }

    public override void Tick(EnemyController owner)
    {
        if (player == null)
        {
            owner.ChangeState(new WanderState());
            return;
        }

        // Siempre mirar al jugador
        owner.movement.RotateTowards(player.position);

        // Distancia al jugador
        float dist = Vector3.Distance(owner.transform.position, player.position);

        float safeDist = (owner.instanceOverrides != null)
            ? owner.instanceOverrides.GetPassiveSafeDistance(owner.stats.passiveSafeDistance)
            : owner.stats.passiveSafeDistance;

        float retreatSpeed = (owner.instanceOverrides != null)
            ? owner.instanceOverrides.GetPassiveRetreatSpeed(owner.stats.passiveRetreatSpeed)
            : owner.stats.passiveRetreatSpeed;

        // Si el jugador está dentro de la zona segura → retroceder pero sin girar
        if (dist < safeDist)
        {
            Vector3 retreatDir = (owner.transform.position - player.position).normalized;
            owner.movement.MoveDirection_NoRotate(retreatDir, retreatSpeed);
            return;
        }

        // Si el jugador está lejos → quedarse quieto mirando
        owner.movement.StopInstantly();
    }

    public override void Exit(EnemyController owner)
    {
        if (owner.animatorBridge)
            owner.animatorBridge.SetBool("IsScared", false);
    }
}
