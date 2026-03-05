// ============================================================
// WorkerController.cs
// ============================================================
// Cerebro de un trabajador (caníbal rehabilitado).
// Gestiona el estado, la profesión asignada, el área de trabajo
// y delega el skin a WorkerSkinController.
//
// ESTADOS:
//   Idle       → sin trabajo asignado, espera en la base
//   Walking    → caminando hacia el área de trabajo
//   Working    → produciendo recursos en el área
//   Resting    → descansando (cansancio > umbral)
//   Hungry     → buscando comida (hambre crítica)
//   Dead       → muerto (devuelto al pool o eliminado)
//
// FLUJO DE REHABILITACIÓN (del GDD):
//   Captured → Stunned → lleva a la base → se vuelve Friendly
//   → el jugador le asigna profesión → el trabajador trabaja
//   → produce recursos → la base crece
// ============================================================

using System;
using UnityEngine;

[RequireComponent(typeof(WorkerNeeds))]
[RequireComponent(typeof(WorkerSkinController))]
public class WorkerController : MonoBehaviour
{
    // ─── Datos ────────────────────────────────────────────────
    [Header("Profesión actual")]
    [SerializeField] ProfessionSO _profession;

    [Header("Debug")]
    [SerializeField] WorkerState _state = WorkerState.Idle;
    [SerializeField] string      _workerName = "Desconocido";

    // ─── Eventos ──────────────────────────────────────────────
    public event Action<WorkerController, ProfessionSO> OnProfessionChanged;
    public event Action<WorkerController>               OnWorkerDied;
    public event Action<WorkerController>               OnStateChanged;

    // ─── Referencias ──────────────────────────────────────────
    WorkerNeeds       _needs;
    WorkerSkinController _skin;
    WorkArea          _assignedArea;

    // Velocidad de desplazamiento (sin NavMesh por ahora — sin agentes)
    [Header("Movimiento")]
    [SerializeField] float moveSpeed = 2.5f;
    Vector3 _moveTarget;
    bool    _isMovingToWork;

    // ─── Propiedades ──────────────────────────────────────────
    public ProfessionSO   Profession     => _profession;
    public ProfessionType ProfessionType => _profession != null ? _profession.type : ProfessionType.None;
    public WorkerState    State          => _state;
    public WorkArea       AssignedArea   => _assignedArea;
    public WorkerNeeds    Needs          => _needs;
    public bool           IsWorking      => _state == WorkerState.Working;
    public string         WorkerName     => _workerName;

    // ─── Init ─────────────────────────────────────────────────

    void Awake()
    {
        _needs = GetComponent<WorkerNeeds>();
        _skin  = GetComponent<WorkerSkinController>();

        // Suscribir a necesidades
        _needs.OnHungerCritical += HandleHungerCritical;
        _needs.OnRestCritical   += HandleRestCritical;
        _needs.OnWorkerDied     += HandleDeath;
        _needs.OnRebellionRisk  += HandleRebellionRisk;
    }

    void OnDestroy()
    {
        if (_needs != null)
        {
            _needs.OnHungerCritical -= HandleHungerCritical;
            _needs.OnRestCritical   -= HandleRestCritical;
            _needs.OnWorkerDied     -= HandleDeath;
            _needs.OnRebellionRisk  -= HandleRebellionRisk;
        }
    }

    // ─── Update ───────────────────────────────────────────────

    void Update()
    {
        if (_state == WorkerState.Dead) return;

        _needs.Tick(Time.deltaTime);
        UpdateState();
        UpdateMovement();
        UpdateSkinAnimations();
    }

    void UpdateState()
    {
        switch (_state)
        {
            case WorkerState.Idle:
                // Si hay área asignada y puede trabajar → ir a trabajar
                if (_assignedArea != null && !_needs.IsTired && !_needs.IsHungry)
                    SetState(WorkerState.Walking);
                break;

            case WorkerState.Walking:
                if (_assignedArea == null)
                {
                    SetState(WorkerState.Idle);
                    break;
                }
                // Al llegar → trabajar
                if (Vector3.Distance(transform.position, _moveTarget) < 0.5f)
                    SetState(WorkerState.Working);
                break;

            case WorkerState.Working:
                // Si se cansa → descansar
                if (_needs.IsTired)  { SetState(WorkerState.Resting); break; }
                if (_needs.IsHungry) { SetState(WorkerState.Hungry);  break; }
                break;

            case WorkerState.Resting:
                if (!_needs.IsTired) SetState(_assignedArea != null ? WorkerState.Walking : WorkerState.Idle);
                break;

            case WorkerState.Hungry:
                if (!_needs.IsHungry) SetState(_assignedArea != null ? WorkerState.Walking : WorkerState.Idle);
                break;
        }
    }

    void UpdateMovement()
    {
        if (_state != WorkerState.Walking) return;

        transform.position = Vector3.MoveTowards(
            transform.position, _moveTarget, moveSpeed * Time.deltaTime);

        if (_moveTarget != transform.position)
        {
            Vector3 dir = (_moveTarget - transform.position).normalized;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
        }
    }

    void UpdateSkinAnimations()
    {
        _skin.SetWorking(_state == WorkerState.Working);
        _skin.SetWalking(_state == WorkerState.Walking);
    }

    // ─── Cambio de estado ─────────────────────────────────────

    void SetState(WorkerState newState)
    {
        if (_state == newState) return;
        _state = newState;

        _needs.SetWorking(newState == WorkerState.Working);

        if (newState == WorkerState.Walking && _assignedArea != null)
            _moveTarget = _assignedArea.transform.position;

        OnStateChanged?.Invoke(this);
    }

    // ─── API pública ──────────────────────────────────────────

    /// <summary>
    /// Asigna una profesión al trabajador.
    /// Cambia su skin, Animator y work tags automáticamente.
    /// </summary>
    public void AssignProfession(ProfessionSO profession)
    {
        // Liberar área anterior si la profesión no es compatible
        if (_assignedArea != null &&
            profession != null &&
            !profession.CanWorkAt(_assignedArea.WorkTag))
        {
            _assignedArea.RemoveWorker(this);
        }

        _profession = profession;

        // Aplicar skin
        _skin.ApplySkin(profession);

        // Actualizar multiplicador de comida
        float foodMult = profession != null ? profession.foodConsumptionRate : 1f;
        _needs.SetFoodMultiplier(foodMult);

        OnProfessionChanged?.Invoke(this, profession);

        // Notificar a CommunityManager para que actualice desbloqueos
        CommunityManager.Instance?.OnProfessionAssigned(profession);

        Debug.Log($"[Worker:{_workerName}] Profesión asignada: {profession?.displayName ?? "Ninguna"}");
    }

    /// <summary>Asignar un área de trabajo. Llamado por WorkArea.AssignWorker.</summary>
    public void AssignWorkArea(WorkArea area)
    {
        _assignedArea = area;

        if (area == null)
        {
            SetState(WorkerState.Idle);
        }
        else
        {
            _moveTarget = area.transform.position;
            SetState(WorkerState.Walking);
        }
    }

    /// <summary>Alimentar directamente desde el CommunityManager.</summary>
    public void Feed(float amount) => _needs.Feed(amount);

    /// <summary>Nombrar al trabajador (personalización del GDD).</summary>
    public void SetName(string workerName)
    {
        _workerName = workerName;
        gameObject.name = $"Worker_{workerName}";
    }

    // ─── Rehabilitación (del GDD) ────────────────────────────

    /// <summary>
    /// Inicializa el trabajador como caníbal recién capturado.
    /// Llamado por CommunityManager al rehabilitar un Stunned.
    /// </summary>
    public void InitializeAsNewWorker(string name)
    {
        SetName(name);
        _needs.ResetForNewWorker();
        _profession = null;
        _skin.ApplySkin(null);
        SetState(WorkerState.Idle);

        gameObject.SetActive(true);
    }

    // ─── Respuestas a necesidades ─────────────────────────────

    void HandleHungerCritical()
    {
        if (_state == WorkerState.Working)
            SetState(WorkerState.Hungry);
        Debug.Log($"[Worker:{_workerName}] ¡HAMBRE CRÍTICA!");
    }

    void HandleRestCritical()
    {
        if (_state == WorkerState.Working)
            SetState(WorkerState.Resting);
    }

    void HandleDeath()
    {
        SetState(WorkerState.Dead);
        _skin.TriggerDie();
        OnWorkerDied?.Invoke(this);
        Debug.Log($"[Worker:{_workerName}] Ha muerto de hambre.");
    }

    void HandleRebellionRisk()
    {
        // TODO: sistema de rebelión futuro del GDD
        Debug.LogWarning($"[Worker:{_workerName}] RIESGO DE REBELIÓN - Moral crítica");
    }

    // ─── Limpieza para pooling ────────────────────────────────

    public void ResetForPool()
    {
        if (_assignedArea != null)
        {
            _assignedArea.RemoveWorker(this);
            _assignedArea = null;
        }

        _profession = null;
        _skin.ResetForPool();
        _state = WorkerState.Idle;
        gameObject.SetActive(false);
    }
}

// ─── WorkerState ──────────────────────────────────────────────

public enum WorkerState
{
    Idle,
    Walking,
    Working,
    Resting,
    Hungry,
    Dead,
}
