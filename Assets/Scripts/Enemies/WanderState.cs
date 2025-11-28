using UnityEngine;

public class WanderState : State<EnemyController>
{
    int index = 0;
    float waitTimer = 0f;
    bool waiting = false;

    Vector3 randomTarget;     // ← Target aleatorio
    bool usingRandom = false; // ← Está usando wander aleatorio

    public override void Enter(EnemyController owner)
    {
        waitTimer = 0;
        waiting = false;
        PickNextTarget(owner);
    }

    public override void Tick(EnemyController owner)
    {
        // Detectar jugador
        if (owner.perception.HasDetectedTarget(out Transform player))
        {
            if (owner.stats.cannibalType == CannibalType.Passive)
                owner.ChangeState(new PassiveObserveState(player));
            else
                owner.ChangeState(new ChaseState(player));

            return;
        }

        // Espera cuando llega al punto
        if (waiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                waiting = false;
                PickNextTarget(owner);
            }
            return;
        }

        Vector3 targetPos = usingRandom ? randomTarget : owner.patrolPath.points[index].position;

        float speed = owner.instanceOverrides != null ?
            owner.instanceOverrides.GetMoveSpeed(owner.stats.moveSpeed) :
            owner.stats.moveSpeed;

        owner.movement.MoveTowards(targetPos, speed);

        // ⬇ Renderizar target para debug
        owner.debugTarget = targetPos;

        float dist = Vector3.Distance(owner.transform.position, targetPos);

        if (dist < 0.6f)
        {
            waiting = true;

            waitTimer = owner.instanceOverrides != null ?
                owner.instanceOverrides.GetWanderWait(owner.stats.wanderWaitTime) :
                owner.stats.wanderWaitTime;
        }
    }

    void PickNextTarget(EnemyController owner)
    {
        // Si tiene ruta → usar ruta
        if (owner.patrolPath != null && owner.patrolPath.points.Length > 0)
        {
            usingRandom = false;

            index++;
            if (index >= owner.patrolPath.points.Length)
                index = 0;

            return;
        }

        // Si NO tiene ruta → wander aleatorio
        usingRandom = true;

        Vector2 rnd = Random.insideUnitCircle * owner.stats.wanderRadius;
        randomTarget = owner.transform.position + new Vector3(rnd.x, 0, rnd.y);
    }
}
