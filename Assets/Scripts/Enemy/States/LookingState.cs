using UnityEngine;

public class LookingState : State<EnemyController>
{
    float timer;

    public override void Enter(EnemyController o)
    {
        if (o == null) return;

        timer = o.stats.lookDuration;

        AnimatorBridge ab = o.GetComponent<AnimatorBridge>();
        if (ab != null)
            ab.PlayLook(); // Tomará lookAnim desde EnemyStats

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
