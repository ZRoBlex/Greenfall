// ============================================================
// WorkAreaSO.cs
// Create: Click derecho → Greenfall / Workers / Work Area
// ============================================================
// Define un TIPO de área de trabajo: qué produce, qué profesiones
// acepta, cuántos trabajadores caben y a qué velocidad produce.
//
// EJEMPLOS:
//  "crop_field"   workTag  → Farmer  → produce Food
//  "workshop"     workTag  → Crafter → produce Components / AmmoPistol
//  "watchtower"   workTag  → Guard   → protege la base
//  "kitchen"      workTag  → Cook    → produce CookedRation
//  "infirmary"    workTag  → Medic   → produce MedicalSupplies
//  "lumber_yard"  workTag  → Scavenger → produce Wood
//  "scrap_heap"   workTag  → Scavenger → produce Metal / Scrap
//  "construction" workTag  → Builder → permite construir estructuras
// ============================================================

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Greenfall/Workers/Work Area", fileName = "NewWorkArea")]
public class WorkAreaSO : ScriptableObject
{
    // ─── Identidad ────────────────────────────────────────────
    [Header("Identidad")]
    public string workAreaName = "Nueva Área";

    [Tooltip("Tag único que coincide con compatibleWorkTags del ProfessionSO.")]
    public string workTag = "generic";

    public Sprite         icon;
    [TextArea(2, 3)]
    public string         description = "";

    // ─── Capacidad ────────────────────────────────────────────
    [Header("Capacidad")]
    [Range(1, 20)]
    [Tooltip("Cuántos trabajadores pueden trabajar aquí al mismo tiempo.")]
    public int maxWorkers = 2;

    // ─── Producción ───────────────────────────────────────────
    [Header("Producción")]
    [Tooltip("Lo que produce esta área cada ciclo.")]
    public List<WorkOutput> outputs = new List<WorkOutput>();

    [Range(5f, 300f)]
    [Tooltip("Segundos de juego entre cada ciclo de producción.")]
    public float productionCycleSeconds = 30f;

    [Tooltip("Si es true, la cantidad producida se multiplica por el número de trabajadores activos.")]
    public bool scalesWithWorkerCount = true;

    // ─── Requisitos ───────────────────────────────────────────
    [Header("Requisitos")]
    [Tooltip("Recursos consumidos por ciclo (p.ej. cocina consume RawFood).")]
    public List<WorkInput>  inputs = new List<WorkInput>();

    [Tooltip("Mínimo de trabajadores para que el área sea activa.")]
    public int minWorkersToOperate = 1;
}

// ─── Output ───────────────────────────────────────────────────

[System.Serializable]
public class WorkOutput
{
    public ResourceTypeProfesion resourceType;
    [Range(0f, 100f)] public float minAmount = 1f;
    [Range(0f, 100f)] public float maxAmount = 3f;
    [Range(0f, 1f)]
    [Tooltip("Probabilidad de que este output ocurra. 1 = siempre.")]
    public float chance = 1f;
}

// ─── Input (consume recursos) ─────────────────────────────────

[System.Serializable]
public class WorkInput
{
    public ResourceTypeProfesion resourceType;
    public float        amountPerCycle = 1f;
}
