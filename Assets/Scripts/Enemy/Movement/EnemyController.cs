using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controlador principal del enemigo - ULTRA OPTIMIZADO
/// </summary>
[RequireComponent(typeof(GridPathfinder))]
[RequireComponent(typeof(EnemyMotor))]
[RequireComponent(typeof(EnemyLocalGrid))]
[RequireComponent(typeof(AnimatorBridge))]
[RequireComponent(typeof(ProfessionController))]
[RequireComponent(typeof(NonLethalHealthAdapted))]
[RequireComponent(typeof(Health))]
public class EnemyController : MonoBehaviour
{
    [Header("═══════ CONFIGURACIÓN ═══════")]
    public EnemyStats stats;

    [Header("═══════ TIPO Y EQUIPO ═══════")]
    [SerializeField] CannibalType currentType;
    [SerializeField] string currentTeam = "Enemy";

    [Header("═══════ TIPO ALEATORIO AL SPAWN ═══════")]
    [SerializeField] bool randomizeTypeOnSpawn = true;

    [SerializeField]
    List<CannibalTypeProbability> typeProbabilities = new List<CannibalTypeProbability>()
    {
        new CannibalTypeProbability { type = CannibalType.Aggressive, weight = 40f },
        new CannibalTypeProbability { type = CannibalType.Passive,    weight = 30f },
        new CannibalTypeProbability { type = CannibalType.Neutral,    weight = 20f },
        new CannibalTypeProbability { type = CannibalType.Friendly,   weight = 10f },
    };

    // ═══════ PROPIEDADES PÚBLICAS ═══════

    public CannibalType CurrentType => currentType;
    public string CurrentTeam => currentTeam;
    public EnemyLOD CurrentLOD => currentLOD;

    // ═══════ COMPONENTES (Cached) ═══════

    public EnemyMotor Motor { get; private set; }
    public EnemyPerception Perception { get; private set; }
    public GridPathfinder Pathfinder { get; private set; }
    public EnemyLocalGrid LocalGrid { get; private set; }
    public AnimatorBridge AnimatorBridge { get; private set; }
    public ProfessionController Profession { get; private set; }
    public NonLethalHealthAdapted Health { get; private set; }

    public StateMachine<EnemyController> FSM { get; private set; }

    // ═══════ LOD SYSTEM ═══════

    EnemyLOD currentLOD = EnemyLOD.Active;
    float semiActiveTimer;
    const float SEMI_ACTIVE_TICK_RATE = 0.5f;

    // ═══════ COMBATE ═══════

    public float attackCooldownTimer = 0f;

    // ═══════ OPTIMIZACIÓN ═══════

    GameObject enemyUIRoot;
    Transform cachedTransform;
    CharacterController characterController; // 🔥 NUEVO

    bool isInitialized = false;

    // ═══════ UNITY LIFECYCLE ═══════

    void Awake()
    {
        cachedTransform = transform;

        CacheComponents();
        CacheEnemyUI();

        InitializeComponents();
        InitializeFSM();

        isInitialized = true;
    }

    void OnEnable()
    {
        if (!isInitialized) return;

        if (EnemyManager.Instance != null)
            EnemyManager.Instance.Register(this);

        if (randomizeTypeOnSpawn)
        {
            CannibalType randomType = GetRandomTypeByWeight();
            SetType(randomType);
        }
        else
        {
            ApplyTypeBehavior();
        }
    }

    void OnDisable()
    {
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.Unregister(this);
    }

    void Update()
    {
        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        // 🔴 SLEEP → DETENER COMPLETAMENTE
        if (currentLOD == EnemyLOD.Sleep)
            return;

        // 🟡 SEMI-ACTIVE → IA lenta
        if (currentLOD == EnemyLOD.SemiActive)
        {
            semiActiveTimer -= Time.deltaTime;
            if (semiActiveTimer <= 0f)
            {
                TickAI();
                semiActiveTimer = SEMI_ACTIVE_TICK_RATE;
            }
            return;
        }

        // 🟢 ACTIVE → IA completa
        TickAI();
    }

    // ═══════ INICIALIZACIÓN ═══════

    void CacheComponents()
    {
        Motor = GetComponent<EnemyMotor>();
        Perception = GetComponent<EnemyPerception>();
        Pathfinder = GetComponent<GridPathfinder>();
        LocalGrid = GetComponent<EnemyLocalGrid>();
        AnimatorBridge = GetComponent<AnimatorBridge>();
        Profession = GetComponent<ProfessionController>();
        Health = GetComponent<NonLethalHealthAdapted>();
        characterController = GetComponent<CharacterController>(); // 🔥 NUEVO

        if (Motor == null) Debug.LogError($"[{name}] Motor faltante!");
        if (Perception == null) Debug.LogError($"[{name}] Perception faltante!");
        if (LocalGrid == null) Debug.LogError($"[{name}] LocalGrid faltante!");
    }

    void InitializeComponents()
    {
        if (stats == null)
        {
            Debug.LogError($"[{name}] EnemyStats no asignado!");
            return;
        }

        Motor.stats = stats;
        Perception.stats = stats;
        LocalGrid.stats = stats;
    }

    void InitializeFSM()
    {
        FSM = new StateMachine<EnemyController>(this);
        FSM.ChangeState(new WanderState());
    }

    void CacheEnemyUI()
    {
        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas != null)
        {
            enemyUIRoot = canvas.gameObject;
        }
    }

    // ═══════ IA TICK ═══════

    void TickAI()
    {
        if (Health != null && Health.IsStunned())
        {
            FSM.Tick();
            return;
        }

        UpdateBehavior();
        FSM.Tick();
    }

    void UpdateBehavior()
    {
        if (FSM == null || stats == null) return;

        if (Health != null && Health.IsStunned())
            return;

        switch (currentType)
        {
            case CannibalType.Aggressive:
                UpdateAggressiveBehavior();
                break;

            case CannibalType.Passive:
                UpdatePassiveBehavior();
                break;

            case CannibalType.Neutral:
                UpdateNeutralBehavior();
                break;

            case CannibalType.Friendly:
                UpdateFriendlyBehavior();
                break;
        }
    }

    void UpdateAggressiveBehavior()
    {
        if (Perception.CurrentTarget != null)
        {
            float distSqr = (cachedTransform.position - Perception.CurrentTarget.position).sqrMagnitude;
            float attackRangeSqr = stats.attackRange * stats.attackRange;

            if (distSqr <= attackRangeSqr)
            {
                if (!FSM.IsInState<AttackState>())
                    FSM.ChangeState(new AttackState());
            }
            else
            {
                if (!FSM.IsInState<FollowingState>())
                    FSM.ChangeState(new FollowingState());
            }
        }
        else if (!FSM.IsInState<WanderState>())
        {
            FSM.ChangeState(new WanderState());
        }
    }

    void UpdatePassiveBehavior()
    {
        if (Perception.CurrentTarget != null)
        {
            if (!FSM.IsInState<ScaredState>())
                FSM.ChangeState(new ScaredState());
        }
        else if (!FSM.IsInState<WanderState>())
        {
            FSM.ChangeState(new WanderState());
        }
    }

    void UpdateNeutralBehavior()
    {
        if (!FSM.IsInState<WanderState>())
            FSM.ChangeState(new WanderState());
    }

    void UpdateFriendlyBehavior()
    {
        if (Perception.CurrentTarget != null)
        {
            if (!FSM.IsInState<FriendlyState>())
                FSM.ChangeState(new FriendlyState());
        }
        else if (!FSM.IsInState<WanderState>())
        {
            FSM.ChangeState(new WanderState());
        }
    }

    // ═══════ LOD SYSTEM - ARREGLADO ═══════

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
                EnableSemiActiveAI();
                break;

            case EnemyLOD.Sleep:
                EnableSleepMode();
                break;
        }
    }

    void EnableFullAI()
    {
        // Activar componentes críticos
        if (Motor != null) Motor.enabled = true;
        if (Perception != null) Perception.enabled = true;
        if (characterController != null) characterController.enabled = true;

        // Mostrar UI
        if (enemyUIRoot != null)
            enemyUIRoot.SetActive(true);

        enabled = true;
    }

    void EnableSemiActiveAI()
    {
        // Motor desactivado (no se mueve)
        if (Motor != null) Motor.enabled = false;
        if (characterController != null) characterController.enabled = false;

        // Perception activo (puede detectar)
        if (Perception != null) Perception.enabled = true;

        // Ocultar UI
        if (enemyUIRoot != null)
            enemyUIRoot.SetActive(false);

        enabled = true;
        semiActiveTimer = SEMI_ACTIVE_TICK_RATE;
    }

    void EnableSleepMode()
    {
        // 🔥 DESACTIVAR TODO - Completamente pausado
        if (Motor != null) Motor.enabled = false;
        if (Perception != null) Perception.enabled = false;
        if (characterController != null) characterController.enabled = false;

        // Ocultar UI
        if (enemyUIRoot != null)
            enemyUIRoot.SetActive(false);

        // Mantener controller activo solo para cambios de LOD
        enabled = true;
    }

    // ═══════ TIPO Y EQUIPO ═══════

    public void SetType(CannibalType newType)
    {
        if (!stats.canChangeType && isInitialized)
            return;

        currentType = newType;
        ApplyTypeBehavior();
    }

    public void SetTeam(string teamName)
    {
        currentTeam = teamName;
    }

    public void SetTypeAndTeam(CannibalType newType, string teamName)
    {
        currentType = newType;
        currentTeam = teamName;
        ApplyTypeBehavior();
    }

    void ApplyTypeBehavior()
    {
        if (FSM == null) return;

        switch (currentType)
        {
            case CannibalType.Aggressive:
            case CannibalType.Neutral:
                FSM.ChangeState(new WanderState());
                break;

            case CannibalType.Passive:
                FSM.ChangeState(new ScaredState());
                break;

            case CannibalType.Friendly:
                if (Perception.CurrentTarget != null)
                    FSM.ChangeState(new FriendlyState());
                else
                    FSM.ChangeState(new WanderState());
                break;
        }
    }

    CannibalType GetRandomTypeByWeight()
    {
        if (typeProbabilities == null || typeProbabilities.Count == 0)
            return CannibalType.Aggressive;

        float totalWeight = 0f;
        foreach (var entry in typeProbabilities)
        {
            if (entry.weight > 0f)
                totalWeight += entry.weight;
        }

        if (totalWeight <= 0f)
            return CannibalType.Aggressive;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var entry in typeProbabilities)
        {
            if (entry.weight <= 0f) continue;

            cumulative += entry.weight;
            if (roll <= cumulative)
                return entry.type;
        }

        return typeProbabilities[0].type;
    }

    // ═══════ RESET (Para Pool) ═══════

    public void ResetForPool()
    {
        attackCooldownTimer = 0f;
        currentLOD = EnemyLOD.Active;

        if (FSM != null)
            FSM.ChangeState(new WanderState());

        if (Motor != null)
            Motor.rotateTowardsMovement = true;

        if (Perception != null)
            Perception.SetExternalTarget(null);
    }
}