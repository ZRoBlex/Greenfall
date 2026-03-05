// ============================================================
// ProfessionController.cs
// ============================================================
// REEMPLAZA el ProfessionController anterior.
//
// Este script es ahora una capa de integración delgada entre
// el sistema antiguo (EnemyController) y el nuevo
// (WorkerController + WorkerSkinController).
//
// Cuando un caníbal es capturado y llevado a la base,
// EnemyController llama BecomeWorker() en este script, que:
//   1. Notifica a CommunityManager para crear el WorkerController
//   2. El WorkerController toma el control desde ese momento
//   3. Este script queda como referencia pública para el jugador
//
// JERARQUÍA DEL PREFAB CANÍBAL:
//   CannibalRoot
//   ├── EnemyController
//   ├── ProfessionController   ← este script
//   ├── WorkerController       ← activado al rehabilitar
//   ├── WorkerNeeds            ← activado al rehabilitar
//   └── WorkerSkinController   ← activo siempre
// ============================================================

using UnityEngine;

[DisallowMultipleComponent]
public class ProfessionController : MonoBehaviour
{
    // ─── Datos visibles en Inspector ─────────────────────────
    [Header("Estado")]
    [SerializeField] ProfessionSO    _assignedProfession;
    [SerializeField] ProfessionState _professionState = ProfessionState.Wild;

    // ─── Referencias internas ─────────────────────────────────
    WorkerController     _workerCtrl;
    WorkerSkinController _skinCtrl;

    // ─── Propiedades ─────────────────────────────────────────
    public ProfessionSO   AssignedProfession => _assignedProfession;
    public ProfessionState State            => _professionState;
    public bool           IsWorker          => _professionState == ProfessionState.Worker;

    void Awake()
    {
        _workerCtrl = GetComponent<WorkerController>();
        _skinCtrl   = GetComponent<WorkerSkinController>();

        // WorkerController empieza desactivado hasta rehabilitación
        if (_workerCtrl != null) _workerCtrl.enabled = false;
    }

    // ─── Ciclo de vida del GDD ────────────────────────────────

    /// <summary>
    /// Llamado cuando el caníbal pasa de Stunned a Friendly
    /// y el jugador lo lleva a la base.
    /// </summary>
    public void BecomeWorker()
    {
        if (_professionState == ProfessionState.Worker) return;

        _professionState = ProfessionState.Worker;

        // Activar WorkerController
        if (_workerCtrl != null)
        {
            _workerCtrl.enabled = true;
        }

        // Registrar en la comunidad
        CommunityManager.Instance?.RehabilitateCannibal(transform.position);

        Debug.Log($"[ProfessionController:{name}] Caníbal rehabilitado como trabajador.");
    }

    /// <summary>
    /// Asignar profesión al trabajador.
    /// Llamado por el jugador desde la UI de gestión de comunidad.
    /// </summary>
    public void AssignProfession(ProfessionSO profession)
    {
        if (_professionState != ProfessionState.Worker)
        {
            Debug.LogWarning("[ProfessionController] Solo los trabajadores pueden tener profesión.");
            return;
        }

        _assignedProfession = profession;

        // WorkerController hace el trabajo real
        if (_workerCtrl != null)
            _workerCtrl.AssignProfession(profession);
        else if (_skinCtrl != null)
            _skinCtrl.ApplySkin(profession); // fallback si no hay WorkerController
    }

    /// <summary>
    /// El caníbal escapó o fue eliminado. Vuelve a estado salvaje.
    /// </summary>
    public void BecomeWild()
    {
        _professionState    = ProfessionState.Wild;
        _assignedProfession = null;

        if (_workerCtrl != null) _workerCtrl.enabled = false;
        _skinCtrl?.ApplySkin(null);
    }
}

// ─── Estado de la profesión ──────────────────────────────────

public enum ProfessionState
{
    Wild,       // caníbal salvaje, aún no capturado
    Stunned,    // aturdido, listo para ser llevado a la base
    Worker,     // integrado en la comunidad
}
