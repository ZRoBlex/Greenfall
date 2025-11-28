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
        // 1) DETECCIÓN DE PLAYER POR VISIÓN (HasDetectedTarget)
        Transform seenPlayer;
        bool saw = owner.perception.HasDetectedTarget(out seenPlayer);

        if (saw && seenPlayer != null)
        {
            // cambio de estado solo si lo VIO (HasDetectedTarget está basado en FOV+raycast)
            if (owner.stats.cannibalType == CannibalType.Passive)
                owner.ChangeState(new PassiveObserveState(seenPlayer));
            else
                owner.ChangeState(new ChaseState(seenPlayer));

            return;
        }

        // 2) Si el player está físicamente CERCA → solo rotar hacia él (sin cambiar estado)
        GameObject pObj = GameObject.FindWithTag("Player");
        if (pObj != null)
        {
            Transform pT = pObj.transform;
            if (owner.perception.IsPlayerWithinCloseRange(pT))
            {
                owner.movement.RotateTowards(pT.position);
                // no hacemos más: seguimos patrullando pero mirando al jugador
            }
        }

        // 3) LÓGICA DE ESPERA
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

        // 4) MOVERSE AL PUNTO OBJETIVO (ruta o aleatorio)
        Vector3 targetPos = usingRandom ? randomTarget : (owner.patrolPath != null && owner.patrolPath.points.Length > 0 ? owner.patrolPath.points[index].position : owner.transform.position);

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

            // si tenemos ruta, avanzar indice
            if (owner.patrolPath != null && owner.patrolPath.points.Length > 0)
            {
                index++;
                if (index >= owner.patrolPath.points.Length) index = 0;
            }
            else
            {
                // si es wander aleatorio, calculamos uno nuevo al volver a salir de wait
            }
        }
    }

    void PickNextTarget(EnemyController owner)
    {
        if (owner.patrolPath != null && owner.patrolPath.points.Length > 0)
        {
            usingRandom = false;
            // si index está fuera, recórtalo
            if (index >= owner.patrolPath.points.Length) index = 0;
            return;
        }

        // Sin ruta → movimiento aleatorio
        usingRandom = true;
        Vector2 rnd = Random.insideUnitCircle * owner.stats.wanderRadius;
        randomTarget = owner.transform.position + new Vector3(rnd.x, 0, rnd.y);
    }
}
