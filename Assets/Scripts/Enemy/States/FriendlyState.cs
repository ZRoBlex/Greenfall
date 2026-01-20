using UnityEngine;

public class FriendlyState : State<EnemyController>
{
    // Distancia mínima cómoda para no pegarse al jugador
    float stopDistance = 2.0f;

    public override void Enter(EnemyController o)
    {
        if (o == null) return;

        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.ResetSpecialBools();
            o.AnimatorBridge.SetBool("IsWalking", true);
        }

        if (o.Perception.CurrentTarget != null)
            o.Motor.SetTarget(o.Perception.CurrentTarget);

        Debug.Log($"[{o.stats.displayName}] Entró en FriendlyState.");
    }

    public override void Tick(EnemyController o)
    {
        if (o == null || o.Motor == null || o.Perception == null)
            return;

        Transform player = o.Perception.CurrentTarget;

        if (player == null)
        {
            // Si pierde al jugador, vuelve a Wander
            o.FSM.ChangeState(new WanderState());
            return;
        }

        float dist = Vector3.Distance(o.transform.position, player.position);

        // 🔹 Si está muy cerca, se detiene y solo mira al jugador
        if (dist <= stopDistance)
        {
            // Detener movimiento
            Vector2Int myCell = o.LocalGrid.WorldToCell(o.transform.position);
            o.Motor.SetDestination(myCell);

            if (o.AnimatorBridge != null)
            {
                o.AnimatorBridge.SetBool("IsWalking", false);
                o.AnimatorBridge.SetBool("IsIdle", true);
            }

            LookAtPlayer(o, player);
            return;
        }

        // 🔹 Si está lejos, sigue caminando detrás del jugador
        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.SetBool("IsIdle", false);
            o.AnimatorBridge.SetBool("IsWalking", true);
        }

        o.Motor.SetTarget(player);
    }

    public override void Exit(EnemyController o)
    {
        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.SetBool("IsWalking", false);
            o.AnimatorBridge.SetBool("IsIdle", false);
        }
    }

    void LookAtPlayer(EnemyController o, Transform player)
    {
        Vector3 dir = player.position - o.transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion rot = Quaternion.LookRotation(dir);
            o.transform.rotation = Quaternion.Slerp(
                o.transform.rotation,
                rot,
                o.stats.turnSpeed * Time.deltaTime
            );
        }
    }
}
