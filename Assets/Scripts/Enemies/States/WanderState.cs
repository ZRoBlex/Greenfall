using UnityEngine;
using System.Collections.Generic;

public class WanderState : State<EnemyController>
{
    int index = 0;
    float waitTimer = 0f;
    bool waiting = false;

    Vector3 randomTarget;
    bool usingRandom = false;

    List<Vector3> path = new List<Vector3>();
    int pathIndex = 0;

    // configuración local de intentos para targets fallidos
    int attemptsBeforeGiveUp = 3;
    float minTargetDistance = 0.8f; // si el target queda demasiado cerca del origen, se regenera

    public override void Enter(EnemyController owner)
    {
        waitTimer = 0;
        waiting = false;
        PickNextTarget(owner);
        owner.debugStateName = "Wander";

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
            if (owner.animatorBridge != null) { owner.animatorBridge.SetIdle(true); owner.animatorBridge.SetWalking(false); }

            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                waiting = false;
                PickNextTarget(owner);
            }
            return;
        }

        // --- 4) MOVIMIENTO (seguir path) ---
        if (path == null || path.Count == 0)
        {
            // fallback: intenta moverse al randomTarget directo; si tampoco hay, pick next
            if (randomTarget == Vector3.zero || Vector3.Distance(owner.transform.position, randomTarget) < minTargetDistance)
            {
                PickNextTarget(owner);
                return;
            }

            float speedFallback = (owner.instanceOverrides != null) ? owner.instanceOverrides.GetMoveSpeed(owner.stats.moveSpeed) : owner.stats.moveSpeed;
            owner.movement.MoveTowards(randomTarget, speedFallback);
            owner.debugTarget = randomTarget;
            return;
        }

        // si pathIndex fuera inválido, reiniciar
        pathIndex = Mathf.Clamp(pathIndex, 0, Mathf.Max(0, path.Count - 1));
        Vector3 node = path[pathIndex];

        float speed = (owner.instanceOverrides != null) ? owner.instanceOverrides.GetMoveSpeed(owner.stats.moveSpeed) : owner.stats.moveSpeed;

        owner.movement.MoveTowards(node, speed);



        LocalGridPathfinder pf = owner.GetComponent<LocalGridPathfinder>();

        // --- VALIDACIÓN DINÁMICA DEL SIGUIENTE NODO ---
        if (pf.IsNodeBlocked(node))
        {
            // Recalcular SOLO si es necesario, NO cada frame
            List<Vector3> newPath = pf.FindPath(owner.transform.position, usingRandom ? randomTarget : node);

            if (newPath != null && newPath.Count > 0)
            {
                path = newPath;
                pathIndex = 0;
                node = path[pathIndex];
            }
            else
            {
                // si no se puede recalcular → generar nuevo target
                PickNextTarget(owner);
                return;
            }
        }



        owner.debugTarget = node;

        float dist = Vector3.Distance(owner.transform.position, node);

        // animaciones
        if (dist >= 0.6f)
        {
            if (owner.animatorBridge != null) owner.animatorBridge.SetWalking(true);
        }

        if (dist < 0.6f)
        {
            pathIndex++;
            if (pathIndex >= path.Count)
            {
                // llegó al final
                waiting = true;
                waitTimer = (owner.instanceOverrides != null) ? owner.instanceOverrides.GetWanderWait(owner.stats.wanderWaitTime) : owner.stats.wanderWaitTime;

                // avanzar en patrol path si existiera
                if (owner.patrolPath != null && owner.patrolPath.points.Length > 0)
                {
                    index++;
                    if (index >= owner.patrolPath.points.Length) index = 0;
                }
            }
            else
            {
                // continuar al siguiente nodo
            }
        }

        // Si el enemigo se queda estancado (por ejemplo, no puede moverse y el nodo no cambia),
        // detectarlo: si hemos intentado muchos frames sin avanzar, regen target.
        // (Este bloque es simple y evita quedarse pegado en el mismo punto indefinidamente)
        // Puedes mejorar con contadores por frame si quieres más robustez.
    }

    void PickNextTarget(EnemyController owner)
    {
        // escoger objetivo (patrol o random)
        if (owner.patrolPath != null && owner.patrolPath.points.Length > 0)
        {
            usingRandom = false;
            if (index >= owner.patrolPath.points.Length) index = 0;
            randomTarget = owner.patrolPath.points[index].position;
        }
        else
        {
            usingRandom = true;

            // generar con attempts y evitar targets demasiado cercanos o iguales
            int attempts = 0;
            Vector3 candidate = owner.transform.position;
            do
            {
                attempts++;
                Vector2 rnd = Random.insideUnitCircle * owner.stats.wanderRadius;
                candidate = owner.transform.position + new Vector3(rnd.x, 0, rnd.y);

                // ajustar al piso
                if (Physics.Raycast(candidate + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 30f))
                    candidate = hit.point;

                // si el candidate coincide con la posición del agente o es muy cercano, volver a generar
                if (Vector3.Distance(candidate, owner.transform.position) < minTargetDistance) continue;

                break;
            } while (attempts < attemptsBeforeGiveUp);

            randomTarget = candidate;
        }

        // Obtener/crear pathfinder en el owner
        LocalGridPathfinder pf = owner.GetComponent<LocalGridPathfinder>();
        if (pf == null)
            pf = owner.gameObject.AddComponent<LocalGridPathfinder>();

        // IMPORTANT: Ajusta en inspector pf.obstacleMask para EXCLUIR la capa del propio enemigo
        // Si quieres, aquí podrías modificar pf.obstacleMask en runtime: pf.obstacleMask &= ~ (1 << owner.gameObject.layer);
        // Pero normalmente es mejor configurar las capas desde el editor.

        // calcular ruta
        Vector3 start = owner.transform.position;
        Vector3 dest = randomTarget;

        // intentos para generar path; si falla, acortar destino hacia el start
        path = pf.FindPath(start, dest);
        int tries = 0;
        while ((path == null || path.Count == 0) && tries < 3)
        {
            tries++;
            // recortar destino a un punto intermedio para facilitar encontrar camino
            dest = Vector3.Lerp(start, dest, 0.5f);
            path = pf.FindPath(start, dest);
        }

        pathIndex = 0;

        // Si aún no hay path, forzar otro target (evitar quedarse en el mismo punto forever)
        if (path == null || path.Count == 0)
        {
            // marcar target invalid y forzar regeneración la próxima Tick
            randomTarget = Vector3.zero;
            usingRandom = true;
        }
    }
}
