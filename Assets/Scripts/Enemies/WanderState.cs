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
        owner.debugStateName = "Wander";
    }

    public override void Tick(EnemyController owner)
    {
        Transform player;

        // 1) SOLO cambia de estado si lo VE (no por cercanía)
        bool seesPlayer = owner.perception.HasDetectedTarget(out player);

        // HasDetectedTarget combina FOV + closeRadius, debemos limitarlo:
        if (seesPlayer && owner.perception.CanSeeTarget(player))
        {
            if (owner.stats.cannibalType == CannibalType.Passive)
                owner.ChangeState(new PassiveObserveState(player));
            else
                owner.ChangeState(new ChaseState(player));

            return;
        }

        // 2) Si el jugador está cerca → SOLO rotar hacia él (no asustarse)
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            if (owner.perception.IsTargetClose(p.transform))
            {
                owner.movement.RotateTowards(p.transform.position);
            }
        }

        // 3) Lógica de espera
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

        // 4) Moverse hacia objetivo
        Vector3 targetPos = usingRandom ? randomTarget : owner.patrolPath.points[index].position;

        float speed = (owner.instanceOverrides != null)
            ? owner.instanceOverrides.GetMoveSpeed(owner.stats.moveSpeed)
            : owner.stats.moveSpeed;

        owner.movement.MoveTowards(targetPos, speed);

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

        usingRandom = true;

        Vector2 rnd = Random.insideUnitCircle * owner.stats.wanderRadius;
        randomTarget = owner.transform.position + new Vector3(rnd.x, 0, rnd.y);
    }
}
