using UnityEngine;

/// <summary>
/// Estado de observación - SIMPLE Y RÁPIDO
/// </summary>
public class LookingState : State<EnemyController>
{
    float timer;

    public override void Enter(EnemyController o)
    {
        if (o == null || o.stats == null) return;

        timer = o.stats.lookDuration;

        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.ResetSpecialBools();
            o.AnimatorBridge.SetBool("IsIdle", true);
        }
    }

    public override void Tick(EnemyController o)
    {
        if (o == null) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            o.FSM.ChangeState(new WanderState());
        }
    }
}