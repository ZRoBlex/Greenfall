using UnityEngine;

public class EnemyTeamBrain : MonoBehaviour
{
    EnemyTeamController team;
    EnemyPathAgent agent;
    EnemyController ec;

    Transform player;

    public float repathRate = 0.6f;
    float repathTimer = 0;

    void Awake()
    {
        team = GetComponent<EnemyTeamController>();
        agent = GetComponent<EnemyPathAgent>();
        ec = GetComponent<EnemyController>();
        player = GameObject.FindWithTag("Player")?.transform;
    }

    void Update()
    {
        repathTimer -= Time.deltaTime;
        if (repathTimer > 0) return;
        repathTimer = repathRate;

        if (team.currentTeam == FactionTeam.PlayerTeam)
        {
            if (player != null)
                agent.MoveTo(player.position);
            return;
        }

        // si el enemigo pertenece a una zona → moverse hacia ella
        if (team.HasHomeZone())
        {
            Vector3 homePos = team.GetHomePosition();
            agent.MoveTo(homePos);
        }
    }
}
