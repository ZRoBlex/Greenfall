using UnityEngine;

public class LookingState : State<EnemyController>
{
    float timer;

    public override void Enter(EnemyController o)
    {
        timer = o.stats.lookDuration;

        if (o.AnimatorBridge != null)
        {
            o.AnimatorBridge.ResetSpecialBools();
            o.AnimatorBridge.SetBool("IsIdle", true);
        }

        Debug.Log($"[{o.stats.displayName}] Entró en LookingState por {timer} segundos.");
    }


    public override void Tick(EnemyController o)
    {
        if (o == null) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            // Volver a WanderState
            o.FSM.ChangeState(new WanderState());
        }
    }
}
