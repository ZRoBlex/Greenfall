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
        if (owner.animatorBridge != null)
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

        // Si NO lo puede ver (FOV+raycast), salir del estado
        bool seesPlayer = owner.perception.CanSeeTarget(player);
        if (!seesPlayer)
        {
            owner.ChangeState(new WanderState());
            return;
        }

        // Siempre mirar al jugador
        owner.movement.RotateTowards(player.position);

        // obtenemos valores de safe/retreat (soportando instanceOverrides opcional)
        float safeDist = (owner.instanceOverrides != null)
            ? owner.instanceOverrides.GetPassiveSafeDistance(owner.stats.passiveSafeDistance)
            : owner.stats.passiveSafeDistance;

        float retreatSpeed = (owner.instanceOverrides != null)
            ? owner.instanceOverrides.GetPassiveRetreatSpeed(owner.stats.passiveRetreatSpeed)
            : owner.stats.passiveRetreatSpeed;

        float dist = Vector3.Distance(owner.transform.position, player.position);

        // Si el jugador está demasiado cerca → retroceder SIN rotar (mantiene la mirada)
        if (dist < safeDist)
        {
            Vector3 retreatDir = (owner.transform.position - player.position).normalized;
            owner.movement.MoveDirection_NoRotate(retreatDir, retreatSpeed);
            return;
        }

        // Si lo ve pero no está cerca → quedarse quieto mirando
        owner.movement.StopInstantly();
    }

    public override void Exit(EnemyController owner)
    {
        if (owner.animatorBridge != null)
            owner.animatorBridge.SetBool("IsScared", false);

        owner.movement.StopInstantly();
    }
}
