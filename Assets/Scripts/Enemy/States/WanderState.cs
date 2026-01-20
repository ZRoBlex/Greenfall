using UnityEngine;

public class WanderState : State<EnemyController>
{
    float waitTimer = 0f;
    float waitDuration; // Tiempo que espera al llegar al destino

    public override void Enter(EnemyController o)
    {
        PickNewDestination(o);

        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.ResetSpecialBools();
            // IsWalking lo pone el motor
        }

        waitTimer = 0f;
        waitDuration = o.stats.wanderWaitTime;
    }

    public override void Tick(EnemyController o)
    {
        if (o == null || o.Motor == null)
            return;

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
        EnemyLocalGrid grid = o.Motor.GetComponent<EnemyLocalGrid>();
        if (grid == null) return;

        Vector2Int center = grid.WorldToCell(o.transform.position);

        for (int i = 0; i < 10; i++)
        {
            Vector2 rand = Random.insideUnitCircle * o.stats.wanderRadius;
            Vector2Int offset = new Vector2Int(Mathf.RoundToInt(rand.x), Mathf.RoundToInt(rand.y));
            Vector2Int candidate = center + offset;

            if (grid.IsWalkable(candidate))
            {
                o.Motor.SetDestination(candidate);
                return;
            }
        }
    }
}
