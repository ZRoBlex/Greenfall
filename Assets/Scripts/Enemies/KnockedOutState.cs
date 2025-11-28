using UnityEngine;

public class KnockedOutState : State<EnemyController>
{
    float timer = 0f;
    float duration = 20f;

    public KnockedOutState() { }

    public override void Enter(EnemyController owner)
    {
        timer = duration;
        // desactivar movimiento
        owner.movement.StopInstantly();
    }

    public override void Tick(EnemyController owner)
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            // Recuperar
            owner.ChangeState(new WanderState());
            // también podrías restaurar parte de nonlethal capture meter
        }
    }
}
