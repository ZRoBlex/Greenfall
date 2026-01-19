using UnityEngine;

public class WanderState : State<EnemyController>
{
    float waitTimer = 0f;
    float waitDuration; // Tiempo que espera al llegar al destino

    public override void Enter(EnemyController o)
    {
        PickNewDestination(o);

        AnimatorBridge ab = o.GetComponent<AnimatorBridge>();
        if (ab != null)
            ab.PlayWalk(); // Animación de caminar

        waitTimer = 0f; // reset del timer

        waitDuration = o.stats.wanderWaitTime;
    }

    public override void Tick(EnemyController o)
    {
        if (o == null || o.Motor == null)
            return;

        if (o.Motor.HasReachedDestination())
        {
            // Incrementar temporizador
            waitTimer += Time.deltaTime;

            // Mantener animación idle mientras espera
            AnimatorBridge ab = o.GetComponent<AnimatorBridge>();
            if (ab != null)
                ab.PlayIdle();

            // Una vez transcurrido el tiempo de espera, cambiar estado
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
