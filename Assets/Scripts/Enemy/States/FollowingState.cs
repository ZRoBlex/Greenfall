using UnityEngine;

public class FollowingState : State<EnemyController>
{
    public override void Enter(EnemyController o)
    {
        if (o.Perception.CurrentTarget != null)
            o.Motor.SetTarget(o.Perception.CurrentTarget);
    }

    public override void Tick(EnemyController o)
    {
        if (o.Perception.CurrentTarget == null)
        {
            o.FSM.ChangeState(new LookingState());
            return;
        }

        // Si es friendly, sigue al jugador
        if (o.CurrentType == CannibalType.Friendly)
        {
            o.Motor.SetTarget(o.Perception.CurrentTarget);
            // Aquí puedes agregar lógica de distancia mínima/follow speed
        }
        else
        {
            // Enemigos normales siguen su lógica normal (patrulla o atacar)
            o.Motor.SetTarget(o.Perception.CurrentTarget);
        }

        // Mantiene el target actualizado (por si cambia)
        o.Motor.SetTarget(o.Perception.CurrentTarget);
    }
}
