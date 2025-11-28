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
    }

    public override void Tick(EnemyController owner)
    {
        if (player == null)
        {
            owner.ChangeState(new WanderState());
            return;
        }

        // --- NUEVO: DETECCIÓN POR CERCANÍA ---
        bool close = owner.perception.IsTargetClose(player);

        // --- DETECCIÓN NORMAL (visión) ---
        bool visible = owner.perception.CanSeeTarget(player);

        // Si no lo ve y no está cerca, volver a patrulla
        if (!visible && !close)
        {
            owner.ChangeState(new WanderState());
            return;
        }

        // Siempre mirar al jugador
        owner.movement.RotateTowards(player.position);

        // Distancia
        float dist = Vector3.Distance(owner.transform.position, player.position);

        // Obtener valores pasivos
        float safeDist = (owner.instanceOverrides != null)
            ? owner.instanceOverrides.GetPassiveSafeDistance(owner.stats.passiveSafeDistance)
            : owner.stats.passiveSafeDistance;

        float retreatSpeed = (owner.instanceOverrides != null)
            ? owner.instanceOverrides.GetPassiveRetreatSpeed(owner.stats.passiveRetreatSpeed)
            : owner.stats.passiveRetreatSpeed;

        // Si el jugador está demasiado cerca, retroceder mirando al jugador
        if (dist < safeDist)
        {
            Vector3 backward = -owner.transform.forward; // retroceder sin girar
            owner.movement.MoveDirection_NoRotate(backward, retreatSpeed);

            if (owner.animatorBridge != null)
                owner.animatorBridge.SetFloat("Speed", retreatSpeed);

            return;
        }

        // Si lo ve pero no está dentro de safeDist → quedarse quieto mirando al jugador
        owner.movement.StopInstantly();
        if (owner.animatorBridge != null)
            owner.animatorBridge.SetFloat("Speed", 0f);
    }

    public override void Exit(EnemyController owner)
    {
        if (owner.animatorBridge != null)
            owner.animatorBridge.SetBool("IsScared", false);
    }
}
