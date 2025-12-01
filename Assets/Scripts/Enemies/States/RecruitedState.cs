using UnityEngine;

public class RecruitedState : State<EnemyController>
{
    public override void Enter(EnemyController owner)
    {
        // quedar en equipo del jugador: team = 1
        if (owner.instanceOverrides != null) owner.instanceOverrides.overrideTeam = 1;
    }

    public override void Tick(EnemyController owner)
    {
        // comportamiento básico: seguir al jugador pero no atacar
        Transform player = GameObject.FindWithTag("Player")?.transform;
        if (player == null) return;

        if (Vector3.Distance(owner.transform.position, player.position) > 2f)
            owner.movement.MoveTowards(player.position, owner.stats.moveSpeed);
        else
            owner.movement.StopInstantly();
    }
}
