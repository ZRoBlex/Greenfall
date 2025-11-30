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

        // ANIMACIÓN: Idle al entrar (aún no está caminando)
        if (owner.animatorBridge != null) owner.animatorBridge.SetIdle(true);
    }

    public override void Tick(EnemyController owner)
    {
        // --- 1) DETECCIÓN DEL PLAYER POR VISIÓN ---
        Transform seenPlayer;
        bool saw = owner.perception.HasDetectedTarget(out seenPlayer);

        if (saw && seenPlayer != null)
        {
            if (owner.stats.cannibalType == CannibalType.Passive)
                owner.ChangeState(new PassiveObserveState(seenPlayer));
            else
                owner.ChangeState(new ChaseState(seenPlayer));

            return;
        }

        // --- 2) ROTAR SI EL PLAYER ESTÁ CERCA ---
        GameObject pObj = GameObject.FindWithTag("Player");
        if (pObj != null)
        {
            Transform pT = pObj.transform;
            if (owner.perception.IsPlayerWithinCloseRange(pT))
            {
                owner.movement.RotateTowards(pT.position);
            }
        }

        // --- 3) LÓGICA DE ESPERA ---
        if (waiting)
        {
            // ANIMACIÓN: Idle
            if (owner.animatorBridge != null) owner.animatorBridge.SetIdle(true); owner.animatorBridge.SetWalking(false);

            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                waiting = false;
                PickNextTarget(owner);
            }
            return;
        }

        // --- 4) MOVIMIENTO ---
        Vector3 targetPos = usingRandom ? randomTarget :
            (owner.patrolPath != null && owner.patrolPath.points.Length > 0 ?
            owner.patrolPath.points[index].position :
            owner.transform.position);

        float speed = (owner.instanceOverrides != null)
            ? owner.instanceOverrides.GetMoveSpeed(owner.stats.moveSpeed)
            : owner.stats.moveSpeed;

        owner.movement.MoveTowards(targetPos, speed);

        owner.debugTarget = targetPos;

        float dist = Vector3.Distance(owner.transform.position, targetPos);

        // ANIMACIÓN: caminando si se está desplazando hacia el objetivo
        // usamos la distancia al punto para decidir (ya que MoveTowards no expone un booleano directamente)
        if (dist >= 0.6f)
        {
            if (owner.animatorBridge != null) owner.animatorBridge.SetWalking(true);
        }

        if (dist < 0.6f)
        {
            waiting = true;

            // ANIMACIÓN: se detuvo → Idle
            if (owner.animatorBridge != null) owner.animatorBridge.SetIdle(true);

            waitTimer = (owner.instanceOverrides != null)
                ? owner.instanceOverrides.GetWanderWait(owner.stats.wanderWaitTime)
                : owner.stats.wanderWaitTime;

            if (owner.patrolPath != null && owner.patrolPath.points.Length > 0)
            {
                index++;
                if (index >= owner.patrolPath.points.Length) index = 0;
            }
        }
    }

    void PickNextTarget(EnemyController owner)
    {
        if (owner.patrolPath != null && owner.patrolPath.points.Length > 0)
        {
            usingRandom = false;
            if (index >= owner.patrolPath.points.Length) index = 0;
            return;
        }

        usingRandom = true;
        Vector2 rnd = Random.insideUnitCircle * owner.stats.wanderRadius;
        randomTarget = owner.transform.position + new Vector3(rnd.x, 0, rnd.y);
    }
}
