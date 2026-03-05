// ============================================================
// CommunityManager.cs
// ============================================================
// Singleton que gestiona TODA la comunidad de Greenfall:
//
//   TRABAJADORES
//     RehabilitateCannibal()  → caníbal stunned → trabajador
//     RemoveWorker()          → elimina / mata un trabajador
//     AssignProfession()      → cambia la profesión de un trabajador
//     AssignToWorkArea()      → envía trabajador a un área
//
//   RECURSOS
//     AddResource()     → la comunidad recibe un recurso
//     ConsumeResource() → la comunidad consume un recurso
//     HasResource()     → verificar disponibilidad
//
//   ALIMENTACIÓN
//     FeedCycle()       → distribuye comida disponible entre trabajadores
//
//   DESBLOQUEOS
//     OnProfessionAssigned() → verifica si se activó algún UnlockTag
//
//   ESTADÍSTICAS
//     WorkersByProfession() → cuántos trabajadores hay de cada tipo
//
// INTEGRACIÓN:
//   WorkArea.OnResourceProduced → llama AddResource()
//   PlayerInteractor (captura) → llama RehabilitateCannibal()
// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommunityManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────
    public static CommunityManager Instance { get; private set; }

    // ─── Datos ────────────────────────────────────────────────
    [Header("Prefabs / Pools")]
    [Tooltip("Referencia al WorkerPool. Se auto-detecta si está en la escena.")]
    [SerializeField] WorkerPool workerPool;

    [Header("Alimentación automática")]
    [Tooltip("Cada cuántos segundos de juego el sistema alimenta a los trabajadores.")]
    [SerializeField] float feedCycleSeconds = 60f;

    [Tooltip("Raciones por trabajador por ciclo de alimentación.")]
    [SerializeField] float feedAmountPerWorker = 0.5f;

    [Header("Nombres procedurales (pool de nombres del GDD)")]
    [SerializeField] List<string> workerNamePool = new List<string>
    {
        "Rael", "Morda", "Twick", "Shen", "Fara", "Brus", "Lenne",
        "Dox", "Vira", "Colt", "Nas", "Herk", "Yana", "Tor", "Mex",
    };

    // ─── Eventos ──────────────────────────────────────────────
    public event Action<WorkerController>          OnWorkerAdded;
    public event Action<WorkerController>          OnWorkerRemoved;
    public event Action<WorkerController>          OnWorkerDied;
    public event Action<ResourceTypeProfesion, float>       OnResourceAdded;
    public event Action<string>                    OnSystemUnlocked;
    public event Action<WorkerController>          OnRebellionWarning;

    // ─── Estado ───────────────────────────────────────────────
    readonly List<WorkerController>           _workers         = new List<WorkerController>();
    readonly List<WorkArea>                   _workAreas       = new List<WorkArea>();
    readonly Dictionary<ResourceTypeProfesion, float>  _resources       = new Dictionary<ResourceTypeProfesion, float>();
    readonly HashSet<string>                  _unlockedSystems = new HashSet<string>();

    int _nameIndex;
    float _feedTimer;

    // ─── Init ─────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (workerPool == null)
            workerPool = FindObjectOfType<WorkerPool>();

        // Inicializar todos los recursos a 0
        foreach (ResourceTypeProfesion rt in System.Enum.GetValues(typeof(ResourceTypeProfesion)))
            _resources[rt] = 0f;

        // Conectarse a WorkAreas ya en escena
        foreach (var area in FindObjectsOfType<WorkArea>())
            RegisterWorkArea(area);
    }

    void Update()
    {
        _feedTimer += Time.deltaTime;
        if (_feedTimer >= feedCycleSeconds)
        {
            _feedTimer = 0f;
            RunFeedCycle();
        }
    }

    // ─────────────────────────────────────────────────────────
    // GESTIÓN DE TRABAJADORES
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Convierte un caníbal capturado (Stunned) en trabajador.
    /// Llamar cuando el jugador lleva el caníbal a la base.
    /// </summary>
    public WorkerController RehabilitateCannibal(Vector3 spawnPosition)
    {
        if (workerPool == null)
        {
            Debug.LogError("[Community] No hay WorkerPool en escena.");
            return null;
        }

        WorkerController worker = workerPool.Get();
        if (worker == null) return null;

        string name = GetNextWorkerName();
        worker.transform.position = spawnPosition;
        worker.InitializeAsNewWorker(name);

        // Suscribirse a sus eventos
        worker.OnWorkerDied        += HandleWorkerDied;
        worker.OnProfessionChanged += HandleProfessionChanged;

        _workers.Add(worker);
        OnWorkerAdded?.Invoke(worker);

        Debug.Log($"[Community] Nuevo trabajador rehabilitado: {name}. Total: {_workers.Count}");
        return worker;
    }

    /// <summary>
    /// Elimina un trabajador (muerte, expulsión, etc.).
    /// </summary>
    public void RemoveWorker(WorkerController worker)
    {
        if (!_workers.Remove(worker)) return;

        worker.OnWorkerDied        -= HandleWorkerDied;
        worker.OnProfessionChanged -= HandleProfessionChanged;

        OnWorkerRemoved?.Invoke(worker);
        workerPool?.Return(worker);
    }

    /// <summary>
    /// Asigna una profesión a un trabajador.
    /// Punto de entrada principal para el jugador.
    /// </summary>
    public void AssignProfession(WorkerController worker, ProfessionSO profession)
    {
        if (worker == null || profession == null) return;
        worker.AssignProfession(profession);
    }

    /// <summary>
    /// Envía un trabajador a un área de trabajo.
    /// Verifica compatibilidad de profesión automáticamente.
    /// </summary>
    public bool AssignToWorkArea(WorkerController worker, WorkArea area)
    {
        if (worker == null || area == null) return false;

        // Si ya está en un área, sacarlo primero
        if (worker.AssignedArea != null)
            worker.AssignedArea.RemoveWorker(worker);

        return area.AssignWorker(worker);
    }

    // ─── WorkAreas ────────────────────────────────────────────

    public void RegisterWorkArea(WorkArea area)
    {
        if (_workAreas.Contains(area)) return;
        area.OnResourceProduced += HandleResourceProduced;
        _workAreas.Add(area);
    }

    public void UnregisterWorkArea(WorkArea area)
    {
        if (!_workAreas.Remove(area)) return;
        area.OnResourceProduced -= HandleResourceProduced;
    }

    // ─────────────────────────────────────────────────────────
    // RECURSOS
    // ─────────────────────────────────────────────────────────

    public void AddResource(ResourceTypeProfesion type, float amount)
    {
        if (amount <= 0f) return;
        _resources[type] = _resources[type] + amount;
        OnResourceAdded?.Invoke(type, amount);
    }

    public bool ConsumeResource(ResourceTypeProfesion type, float amount)
    {
        if (!HasResource(type, amount)) return false;
        _resources[type] = Mathf.Max(0f, _resources[type] - amount);
        return true;
    }

    public bool HasResource(ResourceTypeProfesion type, float amount) =>
        _resources.TryGetValue(type, out float have) && have >= amount;

    public float GetResourceAmount(ResourceTypeProfesion type) =>
        _resources.TryGetValue(type, out float v) ? v : 0f;

    // ─────────────────────────────────────────────────────────
    // ALIMENTACIÓN
    // ─────────────────────────────────────────────────────────

    void RunFeedCycle()
    {
        if (_workers.Count == 0) return;

        ResourceTypeProfesion foodType = ResourceTypeProfesion.Food;
        float totalNeeded = _workers.Count * feedAmountPerWorker;

        if (HasResource(foodType, totalNeeded))
        {
            // Comida suficiente para todos
            ConsumeResource(foodType, totalNeeded);
            foreach (var w in _workers)
                w.Feed(feedAmountPerWorker);
        }
        else
        {
            // Repartir lo que hay equitativamente
            float available = GetResourceAmount(foodType);
            float perWorker = available / _workers.Count;
            ConsumeResource(foodType, available);
            foreach (var w in _workers)
                w.Feed(perWorker);

            if (available < totalNeeded * 0.5f)
                Debug.LogWarning("[Community] ¡COMIDA CRÍTICA! Muchos trabajadores están pasando hambre.");
        }
    }

    // ─────────────────────────────────────────────────────────
    // DESBLOQUEOS
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Llamado cada vez que se asigna una profesión.
    /// Verifica si algún UnlockTag se activa.
    /// </summary>
    public void OnProfessionAssigned(ProfessionSO profession)
    {
        if (profession == null) return;
        CheckUnlocks(profession);
    }

    void CheckUnlocks(ProfessionSO profession)
    {
        foreach (ProfessionUnlock unlock in profession.unlocks)
        {
            if (_unlockedSystems.Contains(unlock.systemTag)) continue;

            int count = CountWorkersOfType(profession.type);
            if (count >= unlock.requiredCount)
            {
                _unlockedSystems.Add(unlock.systemTag);
                OnSystemUnlocked?.Invoke(unlock.systemTag);
                Debug.Log($"[Community] DESBLOQUEO: {unlock.systemTag} — {unlock.description}");
            }
        }
    }

    // use IsSystemUnlocked instead
    // Sobrecarga más limpia:
    public bool IsSystemUnlocked(string systemTag) => _unlockedSystems.Contains(systemTag);

    // ─────────────────────────────────────────────────────────
    // ESTADÍSTICAS
    // ─────────────────────────────────────────────────────────

    public int TotalWorkers => _workers.Count;

    public int CountWorkersOfType(ProfessionType type)
    {
        int n = 0;
        foreach (var w in _workers)
            if (w.ProfessionType == type) n++;
        return n;
    }

    public Dictionary<ProfessionType, int> GetWorkersByProfession()
    {
        var result = new Dictionary<ProfessionType, int>();
        foreach (ProfessionType pt in System.Enum.GetValues(typeof(ProfessionType)))
            result[pt] = 0;
        foreach (var w in _workers)
            result[w.ProfessionType]++;
        return result;
    }

    public IReadOnlyList<WorkerController> AllWorkers => _workers;

    // ─────────────────────────────────────────────────────────
    // HANDLERS
    // ─────────────────────────────────────────────────────────

    void HandleResourceProduced(WorkArea area, ResourceTypeProfesion type, float amount)
    {
        AddResource(type, amount);
        Debug.Log($"[Community] +{amount:F1} {type} (de {area.data?.workAreaName})");
    }

    void HandleWorkerDied(WorkerController worker)
    {
        OnWorkerDied?.Invoke(worker);
        // Pequeño delay para que se vea la animación antes de devolver al pool
        StartCoroutine(ReturnDeadWorkerDelayed(worker, 3f));
    }

    IEnumerator ReturnDeadWorkerDelayed(WorkerController worker, float delay)
    {
        yield return new WaitForSeconds(delay);
        RemoveWorker(worker);
    }

    void HandleProfessionChanged(WorkerController worker, ProfessionSO profession)
    {
        if (profession != null) CheckUnlocks(profession);
    }

    // ─────────────────────────────────────────────────────────
    // NOMBRES
    // ─────────────────────────────────────────────────────────

    string GetNextWorkerName()
    {
        if (workerNamePool.Count == 0) return $"Trabajador_{_workers.Count + 1}";
        string name = workerNamePool[_nameIndex % workerNamePool.Count];
        _nameIndex++;
        return name;
    }
}
