using UnityEngine;
using System.Collections.Generic;

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

    // EnemyController.cs

    [Header("Combat")]
    public float attackCooldownTimer = 0f;

    public CannibalType CurrentType => currentType;
    public string CurrentTeam => currentTeam;

    [Header("Random Type On Spawn")]
    [SerializeField] bool randomizeTypeOnSpawn = true;

    [SerializeField]
    List<CannibalTypeProbability> typeProbabilities =
        new List<CannibalTypeProbability>()
        {
        new CannibalTypeProbability { type = CannibalType.Aggressive, weight = 40f },
        new CannibalTypeProbability { type = CannibalType.Passive,    weight = 30f },
        new CannibalTypeProbability { type = CannibalType.Neutral,    weight = 20f },
        new CannibalTypeProbability { type = CannibalType.Friendly,   weight = 10f },
        };


    [System.Serializable]
    public class CannibalTypeProbability
    {
        public CannibalType type;
        [Range(0f, 100f)]
        public float weight;
    }


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
    public EnemyLOD CurrentLOD => currentLOD;


    // 🔒 Lista de scripts a controlar
    List<MonoBehaviour> controlledBehaviours = new List<MonoBehaviour>();

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

        // 🔥 Buscar automáticamente el UI del enemigo (hijo con Canvas)
        var canvas = GetComponentInChildren<Canvas>(true);
        if (canvas != null)
        {
            enemyUIRoot = canvas.gameObject;
        }


        // 🔥 Registrar TODOS los scripts que deben apagarse en Sleep
        CacheControlledBehaviours();
    }
    void OnEnable()
    {
        if (randomizeTypeOnSpawn)
        {
            CannibalType randomType = GetRandomTypeByWeight();
            SetType(randomType);
        }
        else
        {
            ApplyTypeBehavior();
        }

        //ResetForSpawn();
    }

    void OnDisable()
    {
        if (EnemySpawner.Instance != null)
            EnemySpawner.Instance.NotifyEnemyDespawned(this);

        if (EnemyPool.Instance != null)
            EnemyPool.Instance.Return(this);
    }



    void Start()
    {
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.Register(this);
    }



    void Update()
    {
        // 🔻 bajar cooldown global
        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        // 🔴 Sleep → no hacer absolutamente nada
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
        if (currentLOD == lod)
            return;

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

    // -----------------------
    // ACTIVACIÓN POR LOD
    // -----------------------

    void EnableFullAI()
    {
        foreach (var b in controlledBehaviours)
        {
            if (b != null)
                b.enabled = true;
        }

        if (enemyUIRoot != null)
            enemyUIRoot.SetActive(true);

        enabled = true;
    }


    // 🔹 UI del enemigo (barras de vida, texto, etc.)
    GameObject enemyUIRoot;


    void EnableCheapAI()
    {
        foreach (var b in controlledBehaviours)
        {
            if (b == null) continue;

            if (b == Motor)
                b.enabled = false;
            else
                b.enabled = true;

        }

        if (enemyUIRoot != null)
            enemyUIRoot.SetActive(false);

        enabled = true;
    }


    void EnableSleepAI()
    {
        foreach (var b in controlledBehaviours)
        {
            if (b != null)
                b.enabled = false;
        }

        if (enemyUIRoot != null)
            enemyUIRoot.SetActive(false);

        enabled = true;
    }


    // -----------------------
    // Cache automático de scripts
    // -----------------------
    void CacheControlledBehaviours()
    {
        controlledBehaviours.Clear();

        // Tomamos TODOS los MonoBehaviour del enemigo
        var all = GetComponents<MonoBehaviour>();

        foreach (var b in all)
        {
            // Nunca nos desactivamos a nosotros mismos
            if (b == this)
                continue;

            controlledBehaviours.Add(b);
        }
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

                    //if (dist <= stats.attackRange && !(FSM.CurrentState is AttackState))
                    //    FSM.ChangeState(new AttackState());
                    //else if (!(FSM.CurrentState is FollowingState))
                    //    FSM.ChangeState(new FollowingState());
                    if (dist <= stats.attackRange)
                    {
                        if (!(FSM.CurrentState is AttackState))
                            FSM.ChangeState(new AttackState());
                    }
                    else
                    {
                        if (!(FSM.CurrentState is FollowingState))
                            FSM.ChangeState(new FollowingState());
                    }

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

    //void OnDisable()
    //{
    //    if (EnemySpawner.Instance != null)
    //        EnemySpawner.Instance.NotifyEnemyDespawned(this);

    //    if (EnemyPool.Instance != null)
    //        EnemyPool.Instance.Return(this);
    //}

    CannibalType GetRandomTypeByWeight()
    {
        if (typeProbabilities == null || typeProbabilities.Count == 0)
        {
            Debug.LogWarning("[EnemyController] No hay probabilidades configuradas, usando Aggressive");
            return CannibalType.Aggressive;
        }

        float totalWeight = 0f;

        foreach (var entry in typeProbabilities)
        {
            if (entry.weight > 0f)
                totalWeight += entry.weight;
        }

        if (totalWeight <= 0f)
        {
            Debug.LogWarning("[EnemyController] Pesos inválidos, usando Aggressive");
            return CannibalType.Aggressive;
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var entry in typeProbabilities)
        {
            if (entry.weight <= 0f)
                continue;

            cumulative += entry.weight;
            if (roll <= cumulative)
                return entry.type;
        }

        // fallback ultra defensivo
        return typeProbabilities[0].type;
    }

    //void ResetForSpawn()
    //{
    //    attackCooldownTimer = 0f;

    //    if (Health != null)
    //        Health.ResetState(); // si tienes algo así

    //    if (Perception != null)
    //        Perception.ClearTarget(); // si tienes algo así

    //    if (FSM != null)
    //        FSM.ChangeState(new WanderState());
    //}

}
