using UnityEngine;

[RequireComponent(typeof(MovementGrounded), typeof(Health))]
public class EnemyController : MonoBehaviour, IEnemy
{
    [Header("Config")]
    public EnemyStats stats;
    public EnemyInstanceStats instanceOverrides;
    public Profession profession;

    [Header("AI Path")]
    public PatrolPath patrolPath;

    [Header("Debug Info")]
    public Vector3 debugTarget;
    public string debugStateName = "";

    // Components
    public MovementGrounded movement { get; private set; }
    public Perception perception { get; private set; }
    public Combat combat { get; private set; }
    public Health health { get; private set; }
    public NonLethalHealth nonLethalHealth { get; private set; }
    public AnimatorBridge animatorBridge { get; private set; }

    public StateMachine<EnemyController> fsm { get; private set; }

    public bool IsKnockedOut { get; private set; } = false;
    public bool IsRecruited { get; private set; } = false;

    Transform player;

    public Transform Transform => transform;
    public EnemyStats Stats => stats;
    Health IEnemy.Health => health;

    // umbral para considerar "movimiento" (no dependencia float->animación)
    [Header("Animation (threshold)")]
    [Tooltip("Si la CurrentSpeed es mayor a esto, consideramos que camina.")]
    public float walkSpeedThreshold = 0.1f;

    void Awake()
    {
        movement = GetComponent<MovementGrounded>();
        perception = GetComponent<Perception>();
        combat = GetComponent<Combat>();
        health = GetComponent<Health>();
        nonLethalHealth = GetComponent<NonLethalHealth>();
        animatorBridge = GetComponent<AnimatorBridge>();
        fsm = new StateMachine<EnemyController>(this);

        player = GameObject.FindWithTag("Player")?.transform;

        if (health != null) health.OnDeath += OnDeath;
        if (nonLethalHealth != null) nonLethalHealth.OnBecameUnconscious += OnKnockedOut;
    }

    void Start()
    {
        fsm.Initialize(new WanderState());
    }

    void Update()
    {
        // Sólo tickea la FSM si no está KO ni reclutado
        if (!IsKnockedOut && !IsRecruited)
            fsm.Tick();

        // centraliza animaciones u otras cosas si quieres...
        UpdateAnimationFlags();
    }

    void OnDestroy()
    {
        if (health != null) health.OnDeath -= OnDeath;
        if (nonLethalHealth != null) nonLethalHealth.OnBecameUnconscious -= OnKnockedOut;
    }

    public void ChangeState(State<EnemyController> state) => fsm.ChangeState(state);

    /// <summary>
    /// Pone al enemigo en estado KO.
    /// Desactiva los sistemas relevantes para que no actúen mientras está KO.
    /// </summary>
    public void KnockOut()
    {
        if (IsKnockedOut) return;

        IsKnockedOut = true;

        // desactivar componentes que afectan la IA/ movimiento (opcional)
        if (movement) movement.enabled = false;
        if (perception) perception.enabled = false;
        if (combat) combat.enabled = false;

        fsm.ChangeState(new KnockedOutState());
    }

    /// <summary>
    /// Revive al enemigo (sale del KO) y vuelve a ponerlo en patrulla.
    /// </summary>
    public void Revive()
    {
        if (!IsKnockedOut) return;

        IsKnockedOut = false;

        // reactivar componentes
        if (movement) movement.enabled = true;
        if (perception) perception.enabled = true;
        if (combat) combat.enabled = true;

        // opcional: limpiar y reiniciar timers u otros valores aquí

        // volver a patrullar
        ChangeState(new WanderState());
    }

    public void Recruit()
    {
        IsRecruited = true;
        fsm.ChangeState(new RecruitedState());
    }

    public void TakeDamage(float amount, GameObject source = null)
    {
        health.ApplyDamage(amount);
    }

    public void ApplyStun(float seconds)
    {
        movement.ApplyStun(seconds);
    }

    void OnDeath() => Destroy(gameObject);

    void OnKnockedOut() => KnockOut();

    // ----------------------------
    // Animations helper (mantener si la usas)
    // ----------------------------
    void UpdateAnimationFlags()
    {
        if (animatorBridge == null || movement == null || fsm == null) return;

        bool isChasing = fsm.CurrentState is ChaseState;
        bool isScared = fsm.CurrentState is PassiveObserveState;
        bool isWalking = movement.CurrentSpeed > walkSpeedThreshold && !(isChasing || isScared);
        bool isIdle = !isWalking && !isChasing && !isScared;

        //animatorBridge.SetChasing(isChasing);
        //animatorBridge.SetScared(isScared);
        //animatorBridge.SetWalking(isWalking);
        //animatorBridge.SetIdle(isIdle);

        if (isChasing) debugStateName = "Chase";
        else if (isScared) debugStateName = "Scared";
        else if (isWalking) debugStateName = "Walking";
        else if (isIdle) debugStateName = "Idle";
        //else debugStateName = "Idle";
    }

    // OnDrawGizmos (mantener si ya lo tienes)
    void OnDrawGizmos()
    {
        if (movement != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + movement.LastMoveDirection * 1.5f);
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(debugTarget, 0.25f);
        Gizmos.DrawLine(transform.position, debugTarget);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, debugStateName);
#endif

        if (patrolPath != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPath.points.Length; i++)
            {
                Transform p = patrolPath.points[i];
                Transform next = patrolPath.points[(i + 1) % patrolPath.points.Length];
                Gizmos.DrawSphere(p.position, 0.2f);
                Gizmos.DrawLine(p.position, next.position);
            }
        }
    }
}
