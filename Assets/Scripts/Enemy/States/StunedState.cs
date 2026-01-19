using UnityEngine;

public class StunnedState : State<EnemyController>
{
    float timer;

    public override void Enter(EnemyController owner)
    {

        if (owner == null) return;

        // Duración tomada desde NonLethalHealthAdapter
        var health = owner.GetComponent<NonLethalHealthAdapted>();
        timer = health != null ? health.stunDuration : 5f; // default 5s

        // Detener movimiento
        if (owner.Motor != null)
            owner.Motor.enabled = false;

        // Reproducir animación de stun
        var animatorBridge = owner.GetComponent<AnimatorBridge>();
        if (animatorBridge != null && owner.stats != null)
            animatorBridge.Play(owner.stats.stunnedAnim);
        //if (owner.animatorBridge != null && owner.stats != null)
        //    owner.animatorBridge.Play(owner.stats.stunnedAnim);

        Debug.Log($"[{owner.stats.displayName}] Entró en StunnedState por {timer} segundos");
    }

    public override void Tick(EnemyController owner)
    {
        if (owner == null) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            // Reactivar motor
            if (owner.Motor != null)
                owner.Motor.enabled = true;

            // Volver a Wander
            owner.FSM.ChangeState(new WanderState());
            Debug.Log($"[{owner.stats.displayName}] Salió de StunnedState");
        }
    }
}
