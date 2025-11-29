using UnityEngine;

/// <summary>
/// El enemigo sigue al jugador a una distancia configurable.
/// - Mantiene rotación hacia donde camina.
/// - Se detiene si está suficientemente cerca.
/// </summary>
public class FollowPlayerState : State<EnemyController>
{
    Transform player;
    float followDistance = 2.5f;  // distancia mínima para no pegarse demasiado

    public FollowPlayerState(Transform targetPlayer)
    {
        player = targetPlayer;
    }

    public override void Enter(EnemyController owner)
    {
        owner.debugStateName = "Following Player";

        if (owner.animatorBridge != null)
            owner.animatorBridge.SetBool("IsFollowing", true);
    }

    public override void Tick(EnemyController owner)
    {
        if (player == null)
        {
            owner.ChangeState(new WanderState());
            return;
        }

        // Distancia actual (math: magnitud de la diferencia)
        float dist = Vector3.Distance(owner.transform.position, player.position);

        // Si está muy lejos → acercarse
        if (dist > followDistance)
        {
            float speed = owner.instanceOverrides != null ?
                owner.instanceOverrides.GetMoveSpeed(owner.stats.moveSpeed) :
                owner.stats.moveSpeed;

            owner.movement.MoveTowards(player.position, speed);
            return;
        }

        // Si está en la distancia ideal → parar pero seguir mirando al jugador
        owner.movement.StopInstantly();
        owner.movement.RotateTowards(player.position);
    }

    public override void Exit(EnemyController owner)
    {
        if (owner.animatorBridge != null)
            owner.animatorBridge.SetBool("IsFollowing", false);
    }
}
