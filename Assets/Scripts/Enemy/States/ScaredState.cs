using UnityEngine;

public class ScaredState : State<EnemyController>
{
    float repathTimer;
    bool isMovingAway;

    public override void Enter(EnemyController o)
    {
        repathTimer = 0f;
        isMovingAway = false;

        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.ResetSpecialBools();
            o.AnimatorBridge.SetBool("IsScared", true);
            // IsWalking lo controla EnemyMotor
        }

        // Mientras esté asustado, NO rotar hacia el movimiento
        o.Motor.rotateTowardsMovement = false;


        LookAtPlayer(o);
    }

    public override void Exit(EnemyController o)
    {
        if (o != null && o.Motor != null)
            o.Motor.rotateTowardsMovement = true;
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

        // 🔹 Si ya está a distancia segura
        if (dist >= o.stats.passiveSafeDistance)
        {
            // Se queda quieto, vuelve a estar asustado idle
            if (isMovingAway)
            {
                isMovingAway = false;

                if (o.AnimatorBridge != null)
                {
                    o.AnimatorBridge.SetBool("IsWalking", false);
                    o.AnimatorBridge.SetBool("IsScared", true);
                }
            }

            // Ya está a distancia segura → solo mirar al jugador y quedarse idle
            if (isMovingAway)
            {
                isMovingAway = false;

                if (o.AnimatorBridge != null)
                {
                    o.AnimatorBridge.SetBool("IsWalking", false);
                    o.AnimatorBridge.SetBool("IsScared", true);
                }
            }

            // ❌ NO vuelvas a setear destino aquí
            return;

        }

        // 🔹 Recalcular huida cada cierto tiempo
        repathTimer -= Time.deltaTime;
        repathTimer -= Time.deltaTime;

        // 🔹 Si no tiene path activo o llegó al final → fuerza huida inmediata
        if (o.Motor.HasReachedDestination())
        {
            repathTimer = 0f;
        }

        if (repathTimer <= 0f)
        {
            if (!isMovingAway)
            {
                isMovingAway = true;

                if (o.AnimatorBridge != null)
                {
                    o.AnimatorBridge.SetBool("IsScared", false);
                    o.AnimatorBridge.SetBool("IsWalking", true);
                }
            }

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
            o.transform.rotation = Quaternion.Slerp(
                o.transform.rotation,
                targetRotation,
                o.stats.turnSpeed * Time.deltaTime
            );
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
        // IsWalking ya está forzado arriba
    }
}
