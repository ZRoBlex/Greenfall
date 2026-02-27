using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Optimized Enemy Controller:
/// - Efficient LOD system
/// - Minimal Update() overhead
/// - Cached components
/// - Smart state transitions
/// </summary>
[RequireComponent(typeof(OptimizedGridPathfinder))]
[RequireComponent(typeof(OptimizedEnemyMotor))]
[RequireComponent(typeof(EnemyLocalGrid))]
public class OptimizedEnemyController : MonoBehaviour
{
    [Header("Stats")]
    public OptimizedEnemyStats stats;

    [Header("Type & Team")]
    [SerializeField] private CannibalType currentType;
    [SerializeField] private string currentTeam = "Enemy";

    [Header("Random Type On Spawn")]
    [SerializeField] private bool randomizeTypeOnSpawn = true;

    [SerializeField]
    private List<CannibalTypeProbability> typeProbabilities = new List<CannibalTypeProbability>
    {
        new CannibalTypeProbability { type = CannibalType.Aggressive, weight = 40f },
        new CannibalTypeProbability { type = CannibalType.Passive, weight = 30f },
        new CannibalTypeProbability { type = CannibalType.Neutral, weight = 20f },
        new CannibalTypeProbability { type = CannibalType.Friendly, weight = 10f },
    };

    [System.Serializable]
    public class CannibalTypeProbability
    {
        public CannibalType type;
        [Range(0f, 100f)] public float weight;
    }

    // Combat
    public float attackCooldownTimer = 0f;

    // Properties
    public CannibalType CurrentType => currentType;
    public string CurrentTeam => currentTeam;

    // Cached components
    public OptimizedEnemyMotor Motor { get; private set; }
    public OptimizedEnemyPerception Perception { get; private set; }
    public OptimizedGridPathfinder Pathfinder { get; private set; }
    public EnemyLocalGrid LocalGrid { get; private set; }
    public AnimatorBridge AnimatorBridge { get; private set; }

    // State machine
    public StateMachine<OptimizedEnemyController> FSM { get; private set; }

    // LOD
    private EnemyLOD currentLOD = EnemyLOD.Active;
    private float semiActiveTimer;
    private const float SEMI_ACTIVE_INTERVAL = 0.5f;

    public EnemyLOD CurrentLOD => currentLOD;

    // UI
    private GameObject enemyUIRoot;

    // Controlled behaviours
    private List<MonoBehaviour> controlledBehaviours = new List<MonoBehaviour>();

    // State change flags (avoid redundant state changes)
    private CannibalType lastProcessedType;
    private bool hasTarget;

    void Awake()
    {
        // Cache all components once
        Motor = GetComponent<OptimizedEnemyMotor>();
        Perception = GetComponent<OptimizedEnemyPerception>();
        Pathfinder = GetComponent<OptimizedGridPathfinder>();
        LocalGrid = GetComponent<EnemyLocalGrid>();
        AnimatorBridge = GetComponent<AnimatorBridge>();

        // Set stats references
        if (stats != null)
        {
            stats.CacheExpensiveValues();
            Motor.stats = stats;
            Perception.stats = stats;
        }

        // Initialize FSM
        FSM = new StateMachine<OptimizedEnemyController>(this);

        // Find UI
        var canvas = GetComponentInChildren<Canvas>(true);
        if (canvas != null)
        {
            enemyUIRoot = canvas.gameObject;
        }

        // Cache controlled behaviours
        CacheControlledBehaviours();
    }

    void OnEnable()
    {
        // Register with manager
        if (OptimizedEnemyManager.Instance != null)
        {
            OptimizedEnemyManager.Instance.Register(this);
        }

        // Randomize type
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
        // Unregister from manager
        if (OptimizedEnemyManager.Instance != null)
        {
            OptimizedEnemyManager.Instance.Unregister(this);
        }
    }

    void Update()
    {
        // Tick attack cooldown
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        // LOD-based updates
        switch (currentLOD)
        {
            case EnemyLOD.Sleep:
                // Completely frozen
                return;

            case EnemyLOD.SemiActive:
                // Slow updates
                semiActiveTimer -= Time.deltaTime;
                if (semiActiveTimer <= 0f)
                {
                    TickAI();
                    semiActiveTimer = SEMI_ACTIVE_INTERVAL;
                }
                return;

            case EnemyLOD.Active:
                // Full updates
                TickAI();
                break;
        }
    }

    /// <summary>
    /// Main AI tick
    /// </summary>
    private void TickAI()
    {
        // Update behavior based on type
        UpdateBehavior();

        // Tick FSM
        FSM.Tick();
    }

    /// <summary>
    /// Update behavior based on type and perception
    /// </summary>
    private void UpdateBehavior()
    {
        bool currentHasTarget = Perception.CurrentTarget != null;

        // Early exit: no change in state
        if (currentType == lastProcessedType && currentHasTarget == hasTarget)
        {
            return;
        }

        lastProcessedType = currentType;
        hasTarget = currentHasTarget;

        // Determine new state based on type
        switch (currentType)
        {
            case CannibalType.Aggressive:
                UpdateAggressiveBehavior();
                break;

            case CannibalType.Passive:
                UpdatePassiveBehavior();
                break;

            case CannibalType.Neutral:
                if (!(FSM.CurrentState is OptimizedWanderState))
                {
                    FSM.ChangeState(new OptimizedWanderState());
                }
                break;

            case CannibalType.Friendly:
                UpdateFriendlyBehavior();
                break;
        }
    }

    /// <summary>
    /// Aggressive behavior logic
    /// </summary>
    private void UpdateAggressiveBehavior()
    {
        if (Perception.CurrentTarget != null)
        {
            float distSqr = (transform.position - Perception.CurrentTarget.position).sqrMagnitude;

            if (distSqr <= stats.attackRangeSqr)
            {
                if (!(FSM.CurrentState is OptimizedAttackState))
                {
                    FSM.ChangeState(new OptimizedAttackState());
                }
            }
            else
            {
                if (!(FSM.CurrentState is OptimizedFollowingState))
                {
                    FSM.ChangeState(new OptimizedFollowingState());
                }
            }
        }
        else
        {
            if (!(FSM.CurrentState is OptimizedWanderState))
            {
                FSM.ChangeState(new OptimizedWanderState());
            }
        }
    }

    /// <summary>
    /// Passive behavior logic
    /// </summary>
    private void UpdatePassiveBehavior()
    {
        if (Perception.CurrentTarget != null)
        {
            if (!(FSM.CurrentState is OptimizedScaredState))
            {
                FSM.ChangeState(new OptimizedScaredState());
            }
        }
        else
        {
            if (!(FSM.CurrentState is OptimizedWanderState))
            {
                FSM.ChangeState(new OptimizedWanderState());
            }
        }
    }

    /// <summary>
    /// Friendly behavior logic
    /// </summary>
    private void UpdateFriendlyBehavior()
    {
        if (Perception.CurrentTarget != null)
        {
            if (!(FSM.CurrentState is OptimizedFriendlyState))
            {
                FSM.ChangeState(new OptimizedFriendlyState());
            }
        }
        else
        {
            if (!(FSM.CurrentState is OptimizedWanderState))
            {
                FSM.ChangeState(new OptimizedWanderState());
            }
        }
    }

    /// <summary>
    /// Set LOD level
    /// </summary>
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

    /// <summary>
    /// Enable full AI (LOD Active)
    /// </summary>
    private void EnableFullAI()
    {
        foreach (var b in controlledBehaviours)
        {
            if (b != null) b.enabled = true;
        }

        if (enemyUIRoot != null)
        {
            enemyUIRoot.SetActive(true);
        }

        // Full update rate for perception
        if (Perception != null)
        {
            Perception.SetUpdateInterval(0.1f);
        }

        enabled = true;
    }

    /// <summary>
    /// Enable cheap AI (LOD SemiActive)
    /// </summary>
    private void EnableCheapAI()
    {
        foreach (var b in controlledBehaviours)
        {
            if (b == null) continue;

            // Disable motor, keep perception
            if (b == Motor)
            {
                b.enabled = false;
            }
            else
            {
                b.enabled = true;
            }
        }

        if (enemyUIRoot != null)
        {
            enemyUIRoot.SetActive(false);
        }

        // Slower perception updates
        if (Perception != null)
        {
            Perception.SetUpdateInterval(0.5f);
        }

        enabled = true;
    }

    /// <summary>
    /// Enable sleep (LOD Sleep)
    /// </summary>
    private void EnableSleepAI()
    {
        foreach (var b in controlledBehaviours)
        {
            if (b != null) b.enabled = false;
        }

        if (enemyUIRoot != null)
        {
            enemyUIRoot.SetActive(false);
        }

        enabled = true; // Keep controller enabled for LOD updates
    }

    /// <summary>
    /// Cache all controlled behaviours
    /// </summary>
    private void CacheControlledBehaviours()
    {
        controlledBehaviours.Clear();

        var all = GetComponents<MonoBehaviour>();

        foreach (var b in all)
        {
            if (b == this) continue; // Don't disable self
            controlledBehaviours.Add(b);
        }
    }

    /// <summary>
    /// Set enemy type
    /// </summary>
    public void SetType(CannibalType newType)
    {
        currentType = newType;
        lastProcessedType = (CannibalType)(-1); // Force update
        ApplyTypeBehavior();
    }

    /// <summary>
    /// Set team
    /// </summary>
    public void SetTeam(string teamName)
    {
        currentTeam = teamName;
    }

    /// <summary>
    /// Set type and team
    /// </summary>
    public void SetTypeAndTeam(CannibalType newType, string teamName)
    {
        currentType = newType;
        currentTeam = teamName;
        lastProcessedType = (CannibalType)(-1);
        ApplyTypeBehavior();
    }

    /// <summary>
    /// Apply initial behavior based on type
    /// </summary>
    private void ApplyTypeBehavior()
    {
        switch (currentType)
        {
            case CannibalType.Aggressive:
            case CannibalType.Neutral:
                FSM.ChangeState(new OptimizedWanderState());
                break;

            case CannibalType.Passive:
                FSM.ChangeState(new OptimizedScaredState());
                break;

            case CannibalType.Friendly:
                if (Perception.CurrentTarget != null)
                {
                    FSM.ChangeState(new OptimizedFriendlyState());
                }
                else
                {
                    FSM.ChangeState(new OptimizedWanderState());
                }
                break;
        }
    }

    /// <summary>
    /// Get random type by weight
    /// </summary>
    private CannibalType GetRandomTypeByWeight()
    {
        if (typeProbabilities == null || typeProbabilities.Count == 0)
        {
            return CannibalType.Aggressive;
        }

        float totalWeight = 0f;
        foreach (var entry in typeProbabilities)
        {
            if (entry.weight > 0f)
            {
                totalWeight += entry.weight;
            }
        }

        if (totalWeight <= 0f)
        {
            return CannibalType.Aggressive;
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var entry in typeProbabilities)
        {
            if (entry.weight <= 0f) continue;

            cumulative += entry.weight;
            if (roll <= cumulative)
            {
                return entry.type;
            }
        }

        return typeProbabilities[0].type;
    }
}
