using UnityEngine;

/// <summary>
/// Estado de patrulla - OPTIMIZADO
/// </summary>
public class WanderState : State<EnemyController>
{
    float waitTimer;
    float waitDuration;

    // Cache
    Transform cachedTransform;
    Vector3 lastPosition;

    public override void Enter(EnemyController o)
    {
        if (o == null) return;

        cachedTransform = o.transform;
        lastPosition = cachedTransform.position;

        PickNewDestination(o);

        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.ResetSpecialBools();
        }

        waitTimer = 0f;
        waitDuration = o.stats != null ? o.stats.wanderWaitTime : 2f;
    }

    public override void Tick(EnemyController o)
    {
        if (o == null || o.Motor == null) return;

        if (o.Motor.HasReachedDestination())
        {
            waitTimer += Time.deltaTime;

            if (o.AnimatorBridge != null)
                o.AnimatorBridge.SetBool("IsIdle", true);

            if (waitTimer >= waitDuration)
            {
                o.FSM.ChangeState(new LookingState());
            }
        }
    }

    void PickNewDestination(EnemyController o)
    {
        if (o.Motor == null || o.Motor.localGrid == null || o.stats == null)
            return;

        EnemyLocalGrid grid = o.Motor.localGrid;
        Vector2Int center = grid.WorldToCell(lastPosition);

        // Intentar 10 veces encontrar un punto válido
        for (int i = 0; i < 10; i++)
        {
            Vector2 rand = Random.insideUnitCircle * o.stats.wanderRadius;
            Vector2Int offset = new Vector2Int(
                Mathf.RoundToInt(rand.x),
                Mathf.RoundToInt(rand.y)
            );

            Vector2Int candidate = center + offset;

            if (grid.IsWalkable(candidate))
            {
                o.Motor.SetDestination(candidate);
                return;
            }
        }

        // Fallback: quedarse donde está
        o.Motor.SetDestination(center);
    }
}