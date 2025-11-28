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

        // -----------------------------------------
        // 1) SALIDA DEL ESTADO SI YA NO LO VE
        // -----------------------------------------
        bool seesPlayer = owner.perception.CanSeeTarget(player);

        // Rango cercano para "detectar sin verlo"
        float closeRadius = owner.stats.closeDetectionRadius;

        float dist = Vector3.Distance(owner.transform.position, player.position);
        bool playerClose = dist < closeRadius;

        // Si NO lo ve y NO está cerca → salir del estado
        if (!seesPlayer && !playerClose)
        {
            owner.ChangeState(new WanderState());
            return;
        }

        // -----------------------------------------
        // 2) Mientras lo vea o esté cerca → mantenerse
        // -----------------------------------------

        // Mirar SIEMPRE al jugador
        owner.movement.RotateTowards(player.position);

        float safeDist = owner.instanceOverrides != null
            ? owner.instanceOverrides.GetPassiveSafeDistance(owner.stats.passiveSafeDistance)
            : owner.stats.passiveSafeDistance;

        float retreatSpeed = owner.instanceOverrides != null
            ? owner.instanceOverrides.GetPassiveRetreatSpeed(owner.stats.passiveRetreatSpeed)
            : owner.stats.passiveRetreatSpeed;

        // Si está demasiado cerca → retroceder sin girar
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
        if (owner.animatorBridge)
            owner.animatorBridge.SetBool("IsScared", false);

        // Importante: NO seguir mirando al jugador fuera del estado
        owner.movement.StopInstantly();
    }
}
