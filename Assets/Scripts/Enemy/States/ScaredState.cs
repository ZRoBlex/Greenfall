using UnityEngine;

public class ScaredState : State<EnemyController>
{
    float repathTimer;

    public override void Enter(EnemyController o)
    {
        repathTimer = 0f;
        LookAtPlayer(o);
    }

    public override void Tick(EnemyController o)
    {
        if (o == null || o.Motor == null || o.Perception == null)
            return;

        Transform player = o.Perception.CurrentTarget;
        if (player == null)
        {
            o.FSM.ChangeState(new WanderState());
            return;
        }

        LookAtPlayer(o);

        float dist = Vector3.Distance(o.transform.position, player.position);

        if (dist >= o.stats.passiveSafeDistance)
        {
            Vector2Int myCell = o.Motor.localGrid.WorldToCell(o.transform.position);
            o.Motor.SetDestination(myCell);
            return;
        }

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            MoveAway(o, player);
            repathTimer = o.stats.scaredRepathTime;
        }
    }

    void LookAtPlayer(EnemyController o)
    {
        Transform player = o.Perception.CurrentTarget;
        if (player == null)
            return;

        Vector3 direction = player.position - o.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            o.transform.rotation = Quaternion.Slerp(o.transform.rotation, targetRotation, o.stats.turnSpeed * Time.deltaTime);
        }
    }

    void MoveAway(EnemyController o, Transform player)
    {
        var grid = o.Motor.localGrid;
        if (grid == null) return;

        Vector2Int myCell = grid.WorldToCell(o.transform.position);
        Vector2Int playerCell = grid.WorldToCell(player.position);

        Vector2Int awayDirInt = myCell - playerCell;

        Vector2Int bestCell = myCell;
        float bestScore = float.MinValue;

        for (int x = -o.stats.scaredSearchRadius; x <= o.stats.scaredSearchRadius; x++)
        {
            for (int y = -o.stats.scaredSearchRadius; y <= o.stats.scaredSearchRadius; y++)
            {
                Vector2Int candidate = myCell + new Vector2Int(x, y);

                if (!grid.IsWalkable(candidate)) continue;

                Vector2Int dirToCandidate = candidate - myCell;
                Vector2 awayDir = new Vector2(awayDirInt.x, awayDirInt.y).normalized;
                Vector2 dirCandidateF = new Vector2(dirToCandidate.x, dirToCandidate.y).normalized;

                float dot = Vector2.Dot(awayDir, dirCandidateF);
                float distToPlayer = Vector2Int.Distance(candidate, playerCell);
                float score = dot * 2f + distToPlayer;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                }
            }
        }

        o.Motor.SetDestination(bestCell);
    }
}
