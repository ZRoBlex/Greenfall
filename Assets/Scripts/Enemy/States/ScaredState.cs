using UnityEngine;

/// <summary>
/// Estado de miedo - ULTRA OPTIMIZADO
/// </summary>
public class ScaredState : State<EnemyController>
{
    float repathTimer;
    bool isMovingAway;

    // Cache
    Transform cachedTransform;
    Transform cachedTarget;
    EnemyLocalGrid cachedGrid;
    float safeDistanceSqr;

    public override void Enter(EnemyController o)
    {
        if (o == null) return;

        cachedTransform = o.transform;
        cachedGrid = o.Motor != null ? o.Motor.localGrid : null;

        if (o.stats != null)
            safeDistanceSqr = o.stats.scaredSafeDistance * o.stats.scaredSafeDistance;

        repathTimer = 0f;
        isMovingAway = false;

        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.ResetSpecialBools();
            o.AnimatorBridge.SetBool("IsScared", true);
        }

        if (o.Motor != null)
            o.Motor.rotateTowardsMovement = false;

        if (o.Perception != null)
            cachedTarget = o.Perception.CurrentTarget;

        LookAtPlayer(o);
    }

    public override void Exit(EnemyController o)
    {
        if (o != null && o.Motor != null)
            o.Motor.rotateTowardsMovement = true;
    }

    public override void Tick(EnemyController o)
    {
        if (o == null || cachedTransform == null) return;

        // Actualizar target si cambió
        if (o.Perception != null)
            cachedTarget = o.Perception.CurrentTarget;

        if (cachedTarget == null)
        {
            o.FSM.ChangeState(new WanderState());
            return;
        }

        LookAtPlayer(o);

        // 🔥 Squared magnitude para comparación
        float distSqr = (cachedTransform.position - cachedTarget.position).sqrMagnitude;

        // Ya está a distancia segura
        if (distSqr >= safeDistanceSqr)
        {
            if (isMovingAway)
            {
                isMovingAway = false;

                if (o.AnimatorBridge != null)
                {
                    o.AnimatorBridge.SetBool("IsWalking", false);
                    o.AnimatorBridge.SetBool("IsScared", true);
                }
            }
            return;
        }

        // Recalcular huida
        repathTimer -= Time.deltaTime;

        if (o.Motor != null && o.Motor.HasReachedDestination())
            repathTimer = 0f;

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

            MoveAway(o, cachedTarget);
            repathTimer = o.stats != null ? o.stats.scaredRepathTime : 1.5f;
        }
    }

    void LookAtPlayer(EnemyController o)
    {
        if (cachedTarget == null || cachedTransform == null || o.stats == null)
            return;

        Vector3 direction = cachedTarget.position - cachedTransform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            cachedTransform.rotation = Quaternion.Slerp(
                cachedTransform.rotation,
                targetRotation,
                o.stats.turnSpeed * Time.deltaTime
            );
        }
    }

    void MoveAway(EnemyController o, Transform player)
    {
        if (cachedGrid == null || o.stats == null) return;

        Vector2Int myCell = cachedGrid.WorldToCell(cachedTransform.position);
        Vector2Int playerCell = cachedGrid.WorldToCell(player.position);

        Vector2Int awayDirInt = myCell - playerCell;

        Vector2Int bestCell = myCell;
        float bestScore = float.MinValue;

        int radius = o.stats.scaredSearchRadius;

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2Int candidate = myCell + new Vector2Int(x, y);

                if (!cachedGrid.IsWalkable(candidate)) continue;

                Vector2Int dirToCandidate = candidate - myCell;
                Vector2 awayDir = new Vector2(awayDirInt.x, awayDirInt.y).normalized;
                Vector2 dirCandidateF = new Vector2(dirToCandidate.x, dirToCandidate.y).normalized;

                float dot = Vector2.Dot(awayDir, dirCandidateF);

                // 🔥 Usar distancia Manhattan (más rápida)
                int distToPlayer = Mathf.Abs(candidate.x - playerCell.x) + Mathf.Abs(candidate.y - playerCell.y);
                float score = dot * 2f + distToPlayer;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                }
            }
        }

        if (o.Motor != null)
            o.Motor.SetDestination(bestCell);
    }
}