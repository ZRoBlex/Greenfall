using UnityEngine;

public class StunnedState : State<EnemyController>
{
    float timer;

    public override void Enter(EnemyController owner)
    {
        if (owner == null) return;

        var health = owner.GetComponent<NonLethalHealthAdapted>();
        timer = health != null ? health.stunDuration : 5f;

        if (owner.Motor != null)
            owner.Motor.enabled = false;

        if (owner.AnimatorBridge != null)
        {
            owner.AnimatorBridge.ResetSpecialBools();
            owner.AnimatorBridge.SetBool("IsIdle", true);
        }

        Debug.Log($"[{owner.stats.displayName}] Entró en StunnedState por {timer} segundos");
    }


    public override void Tick(EnemyController owner)
    {
        if (owner == null) return;

        // 🔒 Forzar Idle constantemente mientras esté stunned
        if (owner.AnimatorBridge != null)
        {
            owner.AnimatorBridge.ResetSpecialBools();
            owner.AnimatorBridge.SetBool("IsIdle", true);
            owner.AnimatorBridge.SetBool("IsWalking", false);
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            if (owner.Motor != null)
                owner.Motor.enabled = true;

            owner.FSM.ChangeState(new WanderState());
            Debug.Log($"[{owner.stats.displayName}] Salió de StunnedState");
        }
    }

}
