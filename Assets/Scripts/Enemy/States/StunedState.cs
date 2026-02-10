using UnityEngine;

/// <summary>
/// Estado aturdido - OPTIMIZADO
/// </summary>
public class StunnedState : State<EnemyController>
{
    float timer;

    public override void Enter(EnemyController owner)
    {
        if (owner == null) return;

        NonLethalHealthAdapted health = owner.GetComponent<NonLethalHealthAdapted>();
        timer = health != null ? health.stunDuration : 5f;

        // Desactivar motor
        if (owner.Motor != null)
            owner.Motor.enabled = false;

        if (owner.AnimatorBridge != null)
        {
            owner.AnimatorBridge.ResetSpecialBools();
            owner.AnimatorBridge.SetBool("IsIdle", true);
        }
    }

    public override void Tick(EnemyController owner)
    {
        if (owner == null) return;

        // Forzar animación idle
        if (owner.AnimatorBridge != null)
        {
            owner.AnimatorBridge.SetBool("IsIdle", true);
            owner.AnimatorBridge.SetBool("IsWalking", false);
        }

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            // Reactivar motor
            if (owner.Motor != null)
                owner.Motor.enabled = true;

            owner.FSM.ChangeState(new WanderState());
        }
    }
}