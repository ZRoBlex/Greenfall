// ============================================================
// WorkArea.cs
// ============================================================
// Instancia en escena de un área de trabajo.
// Gestiona sus slots (cuántos trabajadores hay), el ciclo
// de producción y la entrega de recursos al CommunityManager.
//
// CÓMO USARLO:
//   1. Crea un GameObject en la base (ej: "CropField")
//   2. Agrega el componente WorkArea
//   3. Asigna el WorkAreaSO correspondiente
//   4. Los trabajadores se asignan con AssignWorker()
//   5. El área produce automáticamente cada ciclo
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

public class WorkArea : MonoBehaviour
{
    [Header("Datos")]
    [Tooltip("ScriptableObject que define qué produce este área y quién puede trabajar aquí.")]
    public WorkAreaSO data;

    [Header("Puntos de trabajo (opcional)")]
    [Tooltip("Transforms donde se posicionan los trabajadores. " +
             "Si hay menos puntos que trabajadores, los extras usan el centro.")]
    public List<Transform> workerSlotPositions = new List<Transform>();

    // ─── Eventos ──────────────────────────────────────────────
    public event Action<WorkArea, ResourceTypeProfesion, float> OnResourceProduced;
    public event Action<WorkArea>                       OnWorkerAdded;
    public event Action<WorkArea>                       OnWorkerRemoved;

    // ─── Estado ───────────────────────────────────────────────
    readonly List<WorkerController> _workers = new List<WorkerController>();
    float _cycleTimer;
    bool  _isActive;

    // ─── Propiedades ──────────────────────────────────────────
    public int  WorkerCount => _workers.Count;
    public int  MaxWorkers  => data != null ? data.maxWorkers : 0;
    public bool IsFull      => data != null && _workers.Count >= data.maxWorkers;
    public bool IsActive    => _isActive;
    public string WorkTag   => data != null ? data.workTag : "";
    public IReadOnlyList<WorkerController> Workers => _workers;

    // ─── Update ───────────────────────────────────────────────

    void Update()
    {
        if (data == null) return;
        if (!_isActive)   return;

        _cycleTimer += Time.deltaTime;

        if (_cycleTimer >= data.productionCycleSeconds)
        {
            _cycleTimer = 0f;
            RunProductionCycle();
        }
    }

    // ─── Asignación de trabajadores ───────────────────────────

    /// <summary>
    /// Asigna un trabajador a este área.
    /// Retorna true si tuvo éxito.
    /// </summary>
    public bool AssignWorker(WorkerController worker)
    {
        if (data == null)              return false;
        if (IsFull)                    return false;
        if (_workers.Contains(worker)) return false;

        // Verificar que la profesión del trabajador es compatible
        if (!CanAccept(worker))
        {
            Debug.LogWarning($"[WorkArea] {worker.name} (profesión: " +
                             $"{worker.ProfessionType}) no puede trabajar en '{data.workAreaName}'");
            return false;
        }

        _workers.Add(worker);
        worker.AssignWorkArea(this);
        RefreshActiveState();

        // Mover al slot de trabajo
        PositionWorkerAtSlot(worker, _workers.Count - 1);

        OnWorkerAdded?.Invoke(this);
        return true;
    }

    /// <summary>Libera un trabajador de este área.</summary>
    public void RemoveWorker(WorkerController worker)
    {
        if (!_workers.Remove(worker)) return;
        worker.AssignWorkArea(null);
        RefreshActiveState();
        OnWorkerRemoved?.Invoke(this);
    }

    /// <summary>Libera todos los trabajadores (al destruir el área).</summary>
    public void RemoveAllWorkers()
    {
        for (int i = _workers.Count - 1; i >= 0; i--)
            RemoveWorker(_workers[i]);
    }

    public bool CanAccept(WorkerController worker)
    {
        if (worker.Profession == null) return false;
        return worker.Profession.CanWorkAt(data.workTag);
    }

    // ─── Ciclo de producción ──────────────────────────────────

    void RunProductionCycle()
    {
        int activeWorkers = CountActiveWorkers();
        if (activeWorkers < data.minWorkersToOperate) return;

        // Verificar inputs (recursos consumidos)
        if (!CheckAndConsumeInputs()) return;

        // Generar outputs
        foreach (WorkOutput output in data.outputs)
        {
            if (UnityEngine.Random.value > output.chance) continue;

            float amount = UnityEngine.Random.Range(output.minAmount, output.maxAmount);

            // Multiplicar por profesión y número de trabajadores
            amount *= GetProfessionMultiplier();
            if (data.scalesWithWorkerCount) amount *= activeWorkers;

            amount = Mathf.Max(0f, amount);
            OnResourceProduced?.Invoke(this, output.resourceType, amount);
        }
    }

    bool CheckAndConsumeInputs()
    {
        if (data.inputs == null || data.inputs.Count == 0) return true;

        // Delegar al CommunityManager
        var cm = CommunityManager.Instance;
        if (cm == null) return true;

        foreach (WorkInput input in data.inputs)
        {
            if (!cm.HasResource(input.resourceType, input.amountPerCycle))
                return false;
        }

        foreach (WorkInput input in data.inputs)
            cm.ConsumeResource(input.resourceType, input.amountPerCycle);

        return true;
    }

    float GetProfessionMultiplier()
    {
        if (_workers.Count == 0) return 1f;
        float total = 0f;
        int   count = 0;
        foreach (var w in _workers)
        {
            if (w.IsWorking && w.Profession != null)
            {
                total += w.Profession.productionMultiplier;
                count++;
            }
        }
        return count > 0 ? total / count : 1f;
    }

    int CountActiveWorkers()
    {
        int n = 0;
        foreach (var w in _workers)
            if (w.IsWorking) n++;
        return n;
    }

    void RefreshActiveState()
    {
        _isActive = data != null && _workers.Count >= data.minWorkersToOperate;
    }

    void PositionWorkerAtSlot(WorkerController worker, int slotIndex)
    {
        if (workerSlotPositions.Count > slotIndex && workerSlotPositions[slotIndex] != null)
        {
            worker.transform.position = workerSlotPositions[slotIndex].position;
            worker.transform.rotation = workerSlotPositions[slotIndex].rotation;
        }
        else
        {
            // Sin slot definido: se sitúa cerca del área
            worker.transform.position = transform.position;
        }
    }

    // ─── Gizmos ───────────────────────────────────────────────

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (data == null) return;

        Gizmos.color = IsFull ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 1.2f);

        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f,
            $"{data.workAreaName}\n{WorkerCount}/{MaxWorkers} workers\n" +
            $"Activa: {_isActive}");

        // Slots de trabajo
        Gizmos.color = Color.yellow;
        foreach (var slot in workerSlotPositions)
        {
            if (slot == null) continue;
            Gizmos.DrawWireCube(slot.position, Vector3.one * 0.5f);
            Gizmos.DrawLine(transform.position, slot.position);
        }
    }
#endif
}
