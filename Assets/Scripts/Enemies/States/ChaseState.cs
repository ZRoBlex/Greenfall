using UnityEngine;

/// <summary>
/// ChaseState definitivo que usa EnemyPathAgent obligatoriamente.
/// - Cancela rutas anteriores (agent.Stop()) al comenzar.
/// - Pide una ruta al jugador (agent.MoveTo) una sola vez al entrar en persecución.
/// - Actualiza dinámicamente la ruta (agent.UpdateDynamicPath).
/// - Se mueve por nodos (agent.GetNextNode + owner.movement.MoveTowards).
/// - Fallback directo si no hay path (temporal).
/// - Logs para depuración.
/// </summary>
public class ChaseState : State<EnemyController>
{
    Transform target;
    EnemyPathAgent agent;
    bool startedChasing = false;

    public ChaseState(Transform t) { target = t; }

    public override void Enter(EnemyController owner)
    {
        // opcional: si tu State base no usa Enter, no pasa nada; esto ayudará si sí lo usa
        agent = owner.GetComponent<EnemyPathAgent>();
        startedChasing = false;
    }

    public override void Tick(EnemyController owner)
    {
        // asegurar referencia al agent
        if (agent == null) agent = owner.GetComponent<EnemyPathAgent>();

        // 1) Validaciones básicas
        if (target == null)
        {
            Debug.Log($"[CHASE] target==null -> volver a Wander ({owner.name})");
            if (agent != null) agent.Stop();
            owner.ChangeState(new WanderState());
            return;
        }

        if (!owner.perception.HasDetectedTarget(out Transform seen) || seen != target)
        {
            Debug.Log($"[CHASE] perdió visión del target -> Stop y Wander ({owner.name})");
            if (agent != null) agent.Stop();
            owner.ChangeState(new WanderState());
            return;
        }

        // distancia actual al jugador
        float dist = Vector3.Distance(owner.transform.position, target.position);

        // 2) Modo Passive (sin pathfinding, igual que antes)
        if (owner.stats.cannibalType == CannibalType.Passive)
        {
            // cancelar cualquier ruta previa
            if (agent != null && startedChasing) { agent.Stop(); startedChasing = false; }

            owner.movement.RotateTowards(target.position);

            if (dist < owner.stats.passiveSafeDistance)
            {
                Vector3 retreat = (owner.transform.position - target.position).normalized;
                owner.movement.MoveDirection(retreat, owner.stats.passiveRetreatSpeed);
                return;
            }
            else
            {
                owner.movement.StopInstantly();
                return;
            }
        }

        // 3) AGGRESSIVE: iniciar persecución basada en pathfinding
        if (!startedChasing)
        {
            startedChasing = true;

            // cancelar ruta previa y pedir nueva ruta al jugador YA
            if (agent != null)
            {
                Debug.Log($"[CHASE] Iniciando chase: Stop agent previo y MoveTo jugador ({owner.name})");
                agent.Stop();                       // cancela ruta de Wander inmediatamente
                agent.MoveTo(target.position);      // solicita nueva ruta al jugador
            }
            else
            {
                Debug.LogWarning($"[CHASE] No se encontró EnemyPathAgent en {owner.name}");
            }
        }

        // 4) Actualizar path dinámico (si agent existe)
        if (agent != null)
        {
            agent.currentTarget = target.position;
            agent.UpdateDynamicPath(); // <-- Forzar repath frecuente

        }

        // 5) Movimiento: usar nodo del agent si hay path, si no -> fallback directo
        float speed = owner.instanceOverrides != null
            ? owner.instanceOverrides.GetRunSpeed(owner.stats.runSpeed)
            : owner.stats.runSpeed;

        bool usedAgent = false;
        if (agent != null && agent.HasPath)
        {
            Vector3 node = agent.GetNextNode();
            owner.debugTarget = node;
            owner.movement.MoveTowards(node, speed);
            usedAgent = true;

            // avanzar nodo; si AdvanceNode devuelve true significa que el path terminó
            if (agent.AdvanceNode(0.45f))
            {
                // si el jugador aún está lejos, solicitar otra ruta (actualizar)
                if (Vector3.Distance(owner.transform.position, target.position) > owner.stats.attackRange)
                {
                    Debug.Log($"[CHASE] Llegó al final del path; pidiendo nueva ruta al jugador ({owner.name})");
                    agent.MoveTo(target.position);
                }
            }
        }

        if (!usedAgent)
        {
            // fallback: moverse directo (temporal) pero solo si no hay path (y avisar)
            Debug.Log($"[CHASE] Fallback directo (sin path) hacia player ({owner.name})");
            owner.debugTarget = target.position;
            owner.movement.MoveTowards(target.position, speed);
        }

        // 6) Si está en rango de ataque -> cambiar a AttackState (detiene agent)
        if (dist <= owner.stats.attackRange)
        {
            Debug.Log($"[CHASE] En rango de ataque -> AttackState ({owner.name})");
            if (agent != null) agent.Stop();
            owner.ChangeState(new AttackState(target));
        }
    }
}
