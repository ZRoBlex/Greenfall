using UnityEngine;

[RequireComponent(typeof(MovementGrounded), typeof(Health))]
public class EnemyController : MonoBehaviour, IEnemy
{
    [Header("Config")]
    public EnemyStats stats;
    public EnemyInstanceStats instanceOverrides;
    public Profession profession;

    [Header("AI Path")]
    public PatrolPath patrolPath;  // ← ← ← AGREGADO AQUÍ

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
        if (!IsKnockedOut && !IsRecruited)
            fsm.Tick();
    }

    void OnDestroy()
    {
        if (health != null) health.OnDeath -= OnDeath;
        if (nonLethalHealth != null) nonLethalHealth.OnBecameUnconscious -= OnKnockedOut;
    }

    public void ChangeState(State<EnemyController> state) => fsm.ChangeState(state);

    public void KnockOut()
    {
        IsKnockedOut = true;
        fsm.ChangeState(new KnockedOutState());
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

    void OnDrawGizmos()
    {
        // Dibujar dirección de movimiento
        if (movement != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + movement.LastMoveDirection * 1.5f);
        }

        // Dibujar target de IA
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(debugTarget, 0.25f);
        Gizmos.DrawLine(transform.position, debugTarget);

        // Dibujar texto del estado
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, debugStateName);
#endif

        // Dibujar ruta si existe
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
