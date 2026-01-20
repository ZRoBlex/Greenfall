using UnityEngine;

[RequireComponent(typeof(GridPathfinder))]
[RequireComponent(typeof(EnemyMotor))]
//[RequireComponent(typeof(EnemyPerception))]
[RequireComponent(typeof(EnemyLocalGrid))]
[RequireComponent(typeof(AnimatorBridge))]
[RequireComponent(typeof(ProfessionController))]
[RequireComponent(typeof(NonLethalHealth))]
[RequireComponent(typeof(Health))]
//[RequireComponent(typeof(NonLethalHealthAdapted))]
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

    void Awake()
    {
        // Obtener todos los componentes (ya serán obligatorios por RequireComponent)
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

        // Inicializa tipo y equipo por defecto
        //currentType = CannibalType.Aggressive;
        currentTeam = "Enemy";
    }

    void Start()
    {
        ApplyTypeBehavior();
    }

    void Update()
    {
        // Prioridad: si está stun, solo tickea FSM
        if (Health != null && Health.IsStunned())
        {
            FSM.Tick();
            return;
        }

        FSM.Tick();
        UpdateBehavior();
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
                    FSM.ChangeState(new FollowingState());
                break;

        }

        Debug.Log($"[EnemyController] {name} comportamiento aplicado según tipo {currentType}");
    }

    public void UpdateBehavior()
    {
        if (FSM == null) return;

        // Tick adicional si está stun
        if (Health != null && Health.IsStunned())
        {
            FSM.Tick();
            return;
        }

        // Cambios de estado según tipo
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
