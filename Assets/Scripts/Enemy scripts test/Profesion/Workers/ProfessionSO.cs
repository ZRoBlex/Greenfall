// ============================================================
// ProfessionSO.cs
// Create: Click derecho → Greenfall / Workers / Profession
// ============================================================
// Define UNA profesión: apariencia, trabajo compatible,
// producción, necesidades y desbloqueos comunitarios.
//
// PROFESIONES DEL GDD:
//  Farmer    → cultiva comida
//  Builder   → construye / repara
//  Guard     → defiende la base
//  Medic     → cura trabajadores
//  Scavenger → produce materiales (chatarra, madera...)
//  Crafter   → fabrica objetos y munición
//  Cook      → mejora eficiencia de la comida
//  Scout     → amplía visión / desbloquea biomas
// ============================================================

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Greenfall/Workers/Profession", fileName = "NewProfession")]
public class ProfessionSO : ScriptableObject
{
    // ─── Identidad ────────────────────────────────────────────
    [Header("Identidad")]
    public ProfessionType type;
    public string         displayName = "Sin Nombre";
    [TextArea(2, 4)]
    public string         description = "";
    public Sprite         icon;

    // ─── Skin ─────────────────────────────────────────────────
    [Header("Skin")]
    [Tooltip("Prefab con el modelo 3D que reemplaza el skin base del caníbal.")]
    public GameObject skinPrefab;

    [Tooltip("Animator Controller de esta profesión.")]
    public RuntimeAnimatorController animatorController;

    [Tooltip("Tinte de color sobre el primer Renderer del skin. " +
             "Requiere shader con _BaseColor o _Color.")]
    public Color skinTint = Color.white;

    // ─── Parámetros del Animator ──────────────────────────────
    [Header("Animator — nombres de parámetros")]
    public string workingParam  = "IsWorking";  // bool
    public string walkingParam  = "IsWalking";  // bool
    public string attackTrigger = "Attack";
    public string hurtTrigger   = "Hurt";
    public string dieTrigger    = "Die";

    // ─── Trabajo ──────────────────────────────────────────────
    [Header("Trabajo")]
    [Tooltip("Tags de WorkArea que este trabajador puede ocupar.")]
    public List<string> compatibleWorkTags = new List<string>();

    [Range(0.1f, 5f)]
    [Tooltip("Multiplicador de velocidad de producción. 1 = normal.")]
    public float productionMultiplier = 1f;

    [Range(0.1f, 5f)]
    [Tooltip("Multiplica el decaimiento de hambre por segundo.")]
    public float foodConsumptionRate = 1f;

    // ─── Combate (Guard / Scout) ──────────────────────────────
    [Header("Combate — sólo Guard / Scout")]
    public float combatDamage = 5f;
    public float alertRadius  = 8f;

    // ─── Desbloqueos ──────────────────────────────────────────
    [Header("Desbloqueos comunitarios")]
    public List<ProfessionUnlock> unlocks = new List<ProfessionUnlock>();

    // ─── Helpers ──────────────────────────────────────────────
    public bool CanWorkAt(string workTag)
    {
        for (int i = 0; i < compatibleWorkTags.Count; i++)
            if (compatibleWorkTags[i] == workTag) return true;
        return false;
    }
}

// ─── Enum ─────────────────────────────────────────────────────

public enum ProfessionType2
{
    None,       // recién capturado
    Farmer,
    Builder,
    Guard,
    Medic,
    Scavenger,
    Crafter,
    Cook,
    Scout,
}

// ─── Desbloqueo por cantidad de trabajadores ──────────────────

[System.Serializable]
public class ProfessionUnlock
{
    [Tooltip("ID del sistema. Ej: 'auto_defense', 'advanced_crafting', 'crop_mutation'")]
    public string systemTag;
    public int    requiredCount = 1;
    [TextArea(1, 2)]
    public string description;
}
