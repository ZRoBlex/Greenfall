using UnityEngine;

[RequireComponent(typeof(GridPathfinder))]
[RequireComponent(typeof(EnemyMotor))]
[RequireComponent(typeof(EnemyLocalGrid))]
[RequireComponent(typeof(AnimatorBridge))]
[RequireComponent(typeof(ProfessionController))]
[RequireComponent(typeof(NonLethalHealth))]
[RequireComponent(typeof(Health))]
public class EnemyController : MonoBehaviour
{
    [Header("Stats")]
    public EnemyStats stats;

    [Header("Tipo y Equipo dinámico")]
    [SerializeField] private CannibalType currentType;
    [SerializeField] private string currentTeam;

    public CannibalType CurrentType => currentType;
    public string CurrentTeam => currentTeam;

    // -----------------------
    // Componentes internos
    // -----------------------
    public EnemyMotor Motor { get; private set; }
    public EnemyPerception Perception { get; private set; }
    public GridPathfinder Pathfinder { get; private set; }
    public EnemyLocalGrid LocalGrid { get; private set; }
    public AnimatorBridge AnimatorBridge { get; private set; }
    public ProfessionController Profession { get; private set; }
    public NonLethalHealthAdapted Health { get; private set; }

    public StateMachine<EnemyController> FSM { get; private set; }

    // -----------------------
    // LOD
    // -----------------------
    EnemyLOD currentLOD = EnemyLOD.Active;
    float semiActiveTimer;

    void Awake()
    {
        Motor = GetComponent<EnemyMotor>();
        Perception = GetComponent<EnemyPerception>();
        Pathfinder = GetComponent<GridPathfinder>();
        LocalGrid = GetComponent<EnemyLocalGrid>();
        AnimatorBridge = GetComponent<AnimatorBridge>();
        Profession = GetComponent<ProfessionController>();
        Health = GetComponent<NonLethalHealthAdapted>();

        Motor.stats = stats;
        Perception.stats = stats;

        FSM = new StateMachine<EnemyController>(this);

        currentTeam = "Enemy";
    }

    void Start()
    {
        ApplyTypeBehavior();

        // 🔹 Registrarse en el EnemyManager
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.Register(this);
    }

    void Update()
    {
        // 🔴 Sleep → no hacer nada
        if (currentLOD == EnemyLOD.Sleep)
            return;

        // 🟡 SemiActive → FSM lenta
        if (currentLOD == EnemyLOD.SemiActive)
        {
            semiActiveTimer -= Time.deltaTime;
            if (semiActiveTimer <= 0f)
            {
                TickAI();
                semiActiveTimer = 0.5f; // 2 veces por segundo
            }
            return;
        }

        // 🟢 Active
        TickAI();
    }

    void TickAI()
    {
        // Prioridad: stun
        if (Health != null && Health.IsStunned())
        {
            FSM.Tick();
            return;
        }

        FSM.Tick();
        UpdateBehavior();
    }

    // -----------------------
    // LOD API
    // -----------------------
    public void SetLOD(EnemyLOD lod)
    {
        if (currentLOD == lod) return;

        currentLOD = lod;

        switch (lod)
        {
            case EnemyLOD.Active:
                EnableFullAI();
                break;

            case EnemyLOD.SemiActive:
                EnableCheapAI();
                break;

            case EnemyLOD.Sleep:
                EnableSleepAI();
                break;
        }
    }

    void EnableFullAI()
    {
        enabled = true;
        Motor.enabled = true;
        Perception.enabled = true;
    }

    void EnableCheapAI()
    {
        enabled = true;
        Motor.enabled = false;        // no path
        Perception.enabled = false;  // no raycasts
    }

    void EnableSleepAI()
    {
        enabled = false;
        Motor.enabled = false;
        Perception.enabled = false;
    }

    // -----------------------
    // Métodos para cambiar tipo/equipo
    // -----------------------
    public void SetType(CannibalType newType)
    {
        currentType = newType;
        ApplyTypeBehavior();
    }

    public void SetTeam(string teamName)
    {
        currentTeam = teamName;
        Debug.Log($"[EnemyController] {name} ahora pertenece al equipo {teamName}");
    }

    public void SetTypeAndTeam(CannibalType newType, string teamName)
    {
        currentType = newType;
        currentTeam = teamName;
        ApplyTypeBehavior();
    }

    private void ApplyTypeBehavior()
    {
        switch (currentType)
        {
            case CannibalType.Aggressive:
                FSM.ChangeState(new WanderState());
                break;

            case CannibalType.Passive:
                FSM.ChangeState(new ScaredState());
                break;

            case CannibalType.Neutral:
                FSM.ChangeState(new WanderState());
                break;

            case CannibalType.Friendly:
                if (Perception.CurrentTarget != null)
                    FSM.ChangeState(new FriendlyState());
                break;
        }
    }

    public void UpdateBehavior()
    {
        if (FSM == null) return;

        if (Health != null && Health.IsStunned())
        {
            FSM.Tick();
            return;
        }

        switch (currentType)
        {
            case CannibalType.Aggressive:
                if (Perception.CurrentTarget != null)
                {
                    float dist = Vector3.Distance(transform.position, Perception.CurrentTarget.position);

                    if (dist <= stats.attackRange && !(FSM.CurrentState is AttackState))
                        FSM.ChangeState(new AttackState());
                    else if (!(FSM.CurrentState is FollowingState))
                        FSM.ChangeState(new FollowingState());
                }
                else if (!(FSM.CurrentState is WanderState))
                    FSM.ChangeState(new WanderState());
                break;

            case CannibalType.Passive:
                if (Perception.CurrentTarget != null && !(FSM.CurrentState is ScaredState))
                    FSM.ChangeState(new ScaredState());
                else if (Perception.CurrentTarget == null && !(FSM.CurrentState is WanderState))
                    FSM.ChangeState(new WanderState());
                break;

            case CannibalType.Neutral:
                if (!(FSM.CurrentState is WanderState))
                    FSM.ChangeState(new WanderState());
                break;

            case CannibalType.Friendly:
                if (Perception.CurrentTarget != null && !(FSM.CurrentState is FriendlyState))
                    FSM.ChangeState(new FriendlyState());
                else if (Perception.CurrentTarget == null && !(FSM.CurrentState is WanderState))
                    FSM.ChangeState(new WanderState());
                break;
        }

        FSM.Tick();
    }
}
