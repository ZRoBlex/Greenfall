using UnityEngine;

public class RecruitedState : State<EnemyController>
{
    EnemyPathAgent agent;

    public override void Enter(EnemyController owner)
    {
        // quedar en equipo del jugador: team = 1
        if (owner.instanceOverrides != null) owner.instanceOverrides.overrideTeam = 1;

        agent = owner.GetComponent<EnemyPathAgent>();
        if (agent == null) agent = owner.gameObject.AddComponent<EnemyPathAgent>();
    }

    public override void Tick(EnemyController owner)
    {
        // comportamiento básico: seguir al jugador pero no atacar
        Transform player = GameObject.FindWithTag("Player")?.transform;
        if (player == null) return;

        float dist = Vector3.Distance(owner.transform.position, player.position);
        if (dist > 2f)
        {
            float speed = owner.stats.moveSpeed;
            agent.MoveTo(player.position, speed);
        }
        else
        {
            agent.Stop();
            owner.movement.StopInstantly();
        }
    }
}
