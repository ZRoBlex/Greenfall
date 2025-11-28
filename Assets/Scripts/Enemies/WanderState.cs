using UnityEngine;

public class WanderState : State<EnemyController>
{
    int index = 0;
    float waitTimer = 0f;
    bool waiting = false;

    Vector3 randomTarget;
    bool usingRandom = false;

    public override void Enter(EnemyController owner)
    {
        waitTimer = 0;
        waiting = false;
        PickNextTarget(owner);
    }

    public override void Tick(EnemyController owner)
    {
        Transform player;

        // -----------------------------------------
        // 1) DETECCIÓN DEL PLAYER (vista O cercanía)
        // -----------------------------------------
        bool detected = owner.perception.HasDetectedTarget(out player);

        // Si no lo detectó aún, verificar proximidad manual
        if (!detected)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null)
            {
                if (owner.perception.IsTargetClose(p.transform))
                {
                    player = p.transform;
                    detected = true;
                }
            }
        }

        // Si lo detectó en cualquiera de las dos formas → cambiar estado
        if (detected && player != null)
        {
            if (owner.stats.cannibalType == CannibalType.Passive)
                owner.ChangeState(new PassiveObserveState(player));
            else
                owner.ChangeState(new ChaseState(player));

            return;
        }

        // -----------------------------------------
        // 2) LÓGICA DE ESPERA
        // -----------------------------------------
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

        // -----------------------------------------
        // 3) MOVERSE AL PUNTO OBJETIVO
        // -----------------------------------------
        Vector3 targetPos = usingRandom ? randomTarget : owner.patrolPath.points[index].position;

        float speed = (owner.instanceOverrides != null)
            ? owner.instanceOverrides.GetMoveSpeed(owner.stats.moveSpeed)
            : owner.stats.moveSpeed;

        owner.movement.MoveTowards(targetPos, speed);

        // Debug
        owner.debugTarget = targetPos;

        float dist = Vector3.Distance(owner.transform.position, targetPos);

        if (dist < 0.6f)
        {
            waiting = true;

            waitTimer = (owner.instanceOverrides != null)
                ? owner.instanceOverrides.GetWanderWait(owner.stats.wanderWaitTime)
                : owner.stats.wanderWaitTime;
        }
    }

    // ------------------------------------------------------
    // Seleccionar próximo punto de la ruta o wander aleatorio
    // ------------------------------------------------------
    void PickNextTarget(EnemyController owner)
    {
        if (owner.patrolPath != null && owner.patrolPath.points.Length > 0)
        {
            usingRandom = false;

            index++;
            if (index >= owner.patrolPath.points.Length)
                index = 0;

            return;
        }

        // Sin ruta → movimiento aleatorio
        usingRandom = true;

        Vector2 rnd = Random.insideUnitCircle * owner.stats.wanderRadius;
        randomTarget = owner.transform.position + new Vector3(rnd.x, 0, rnd.y);
    }
}
