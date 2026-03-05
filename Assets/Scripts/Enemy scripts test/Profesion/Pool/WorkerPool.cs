// ============================================================
// WorkerPool.cs
// ============================================================
// Pool de objetos para trabajadores.
// Evita Instantiate/Destroy continuos al capturar caníbales.
//
// CÓMO FUNCIONA:
//   Get()    → saca un WorkerController del pool (o crea uno nuevo)
//   Return() → devuelve un trabajador al pool (desactiva, limpia)
//
// INTEGRACIÓN:
//   CommunityManager.RehabilitateCannibal() llama WorkerPool.Get()
//   CommunityManager.RemoveWorker()         llama WorkerPool.Return()
// ============================================================

using System.Collections.Generic;
using UnityEngine;

public class WorkerPool : MonoBehaviour
{
    // Singleton accesible globalmente
    public static WorkerPool Instance { get; private set; }

    [Header("Pool")]
    [Tooltip("Prefab base del trabajador. Debe tener WorkerController, WorkerNeeds, WorkerSkinController.")]
    [SerializeField] GameObject workerPrefab;

    [Tooltip("Cuántos trabajadores pre-instanciar al inicio.")]
    [SerializeField] int preloadCount = 10;

    [Tooltip("Máximo de trabajadores activos al mismo tiempo. " +
             "Puesto a 0 = sin límite (no recomendado).")]
    [SerializeField] int maxActive = 50;

    // ─── Estado ───────────────────────────────────────────────
    readonly Queue<WorkerController> _pool        = new Queue<WorkerController>();
    readonly List<WorkerController>  _active      = new List<WorkerController>();
    Transform                        _poolParent;

    // ─── Init ─────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _poolParent = new GameObject("WorkerPool_Inactive").transform;
        _poolParent.SetParent(transform);

        Preload();
    }

    void Preload()
    {
        for (int i = 0; i < preloadCount; i++)
            _pool.Enqueue(CreateNewWorker());
    }

    WorkerController CreateNewWorker()
    {
        if (workerPrefab == null)
        {
            Debug.LogError("[WorkerPool] workerPrefab no asignado.");
            return null;
        }

        var go = Instantiate(workerPrefab, _poolParent);
        go.SetActive(false);
        go.name = "Worker_Pooled";

        var ctrl = go.GetComponent<WorkerController>();
        if (ctrl == null)
            Debug.LogError("[WorkerPool] El prefab no tiene WorkerController.");

        return ctrl;
    }

    // ─── API pública ──────────────────────────────────────────

    /// <summary>
    /// Obtiene un trabajador del pool listo para usar.
    /// Devuelve null si se alcanzó el límite máximo.
    /// </summary>
    public WorkerController Get()
    {
        if (maxActive > 0 && _active.Count >= maxActive)
        {
            Debug.LogWarning("[WorkerPool] Límite máximo de trabajadores alcanzado.");
            return null;
        }

        WorkerController worker;

        if (_pool.Count > 0)
        {
            worker = _pool.Dequeue();
        }
        else
        {
            // Pool vacío → crear uno nuevo
            worker = CreateNewWorker();
        }

        if (worker == null) return null;

        worker.transform.SetParent(null);  // sacar del parent del pool
        _active.Add(worker);
        return worker;
    }

    /// <summary>
    /// Devuelve un trabajador al pool.
    /// Lo desactiva, limpia su estado y lo mueve al parent del pool.
    /// </summary>
    public void Return(WorkerController worker)
    {
        if (worker == null) return;

        _active.Remove(worker);
        worker.ResetForPool();

        worker.transform.SetParent(_poolParent);
        worker.transform.localPosition = Vector3.zero;

        _pool.Enqueue(worker);
    }

    // ─── Info ─────────────────────────────────────────────────
    public int ActiveCount => _active.Count;
    public int PooledCount => _pool.Count;
}
