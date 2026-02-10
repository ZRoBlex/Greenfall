using UnityEngine;

/// <summary>
/// Estado amistoso - OPTIMIZADO
/// </summary>
public class FriendlyState : State<EnemyController>
{
    const float STOP_DISTANCE = 2.0f;
    const float STOP_DISTANCE_SQR = STOP_DISTANCE * STOP_DISTANCE;

    Transform cachedTransform;
    Transform cachedTarget;

    float lastUpdateTime;
    const float UPDATE_INTERVAL = 0.2f;

    public override void Enter(EnemyController o)
    {
        if (o == null) return;

        cachedTransform = o.transform;

        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.ResetSpecialBools();
            o.AnimatorBridge.SetBool("IsWalking", true);
        }

        if (o.Perception != null && o.Perception.CurrentTarget != null)
        {
            cachedTarget = o.Perception.CurrentTarget;
            if (o.Motor != null)
                o.Motor.SetTarget(cachedTarget);
        }

        lastUpdateTime = Time.time;
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

        // 🔥 Squared magnitude
        float distSqr = (cachedTransform.position - cachedTarget.position).sqrMagnitude;

        // Muy cerca → detenerse y mirar
        if (distSqr <= STOP_DISTANCE_SQR)
        {
            // Detener movimiento
            if (o.Motor != null && o.Motor.localGrid != null)
            {
                Vector2Int myCell = o.Motor.localGrid.WorldToCell(cachedTransform.position);
                o.Motor.SetDestination(myCell);
            }

            if (o.AnimatorBridge != null)
            {
                o.AnimatorBridge.SetBool("IsWalking", false);
                o.AnimatorBridge.SetBool("IsIdle", true);
            }

            LookAtPlayer(o, cachedTarget);
            return;
        }

        // Lejos → seguir
        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.SetBool("IsIdle", false);
            o.AnimatorBridge.SetBool("IsWalking", true);
        }

        // 🔥 Solo actualizar target cada X segundos
        if (Time.time - lastUpdateTime > UPDATE_INTERVAL)
        {
            if (o.Motor != null)
                o.Motor.SetTarget(cachedTarget);
            lastUpdateTime = Time.time;
        }
    }

    public override void Exit(EnemyController o)
    {
        if (o == null) return;

        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.SetBool("IsWalking", false);
            o.AnimatorBridge.SetBool("IsIdle", false);
        }
    }

    void LookAtPlayer(EnemyController o, Transform player)
    {
        if (o.stats == null || cachedTransform == null) return;

        Vector3 dir = player.position - cachedTransform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion rot = Quaternion.LookRotation(dir);
            cachedTransform.rotation = Quaternion.Slerp(
                cachedTransform.rotation,
                rot,
                o.stats.turnSpeed * Time.deltaTime
            );
        }
    }
}