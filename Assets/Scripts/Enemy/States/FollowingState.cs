using UnityEngine;

/// <summary>
/// Estado de persecución - OPTIMIZADO
/// </summary>
public class FollowingState : State<EnemyController>
{
    Transform cachedTarget;
    float lastSetTargetTime;
    const float SET_TARGET_INTERVAL = 0.3f; // Solo actualizar cada 0.3s

    public override void Enter(EnemyController o)
    {
        if (o == null) return;

        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.ResetSpecialBools();
            o.AnimatorBridge.SetBool("IsChasing", true);
        }

        if (o.Perception != null && o.Perception.CurrentTarget != null)
        {
            cachedTarget = o.Perception.CurrentTarget;
            o.Motor.SetTarget(cachedTarget);
            lastSetTargetTime = Time.time;
        }
    }

    public override void Tick(EnemyController o)
    {
        if (o == null || o.Motor == null) return;

        // Perdió el target
        if (cachedTarget == null)
        {
            o.FSM.ChangeState(new LookingState());
            return;
        }

        // 🔥 OPTIMIZACIÓN: Solo actualizar target cada X segundos
        if (Time.time - lastSetTargetTime > SET_TARGET_INTERVAL)
        {
            o.Motor.SetTarget(cachedTarget);
            lastSetTargetTime = Time.time;
        }
    }
}