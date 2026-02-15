using UnityEngine;

[CreateAssetMenu(menuName = "Building/Structure Data")]
public class StructureData : ScriptableObject
{
    [Header("═══════ PREFABS ═══════")]
    public GameObject previewPrefab;
    public GameObject finalPrefab;

    [Header("═══════ INFO ═══════")]
    public string structureName;

    [TextArea(2, 4)]
    public string description;

    public Sprite icon;

    [Header("═══════ PLACEMENT RULES ═══════")]
    public bool useGrid = true;
    public bool allowOverlap = false;
    public bool requiresSupport = true;
    public bool canFloat = false;

    [Header("═══════ GRID ═══════")]
    public Vector3 gridOffset;
    public Vector3Int gridSize = Vector3Int.one;

    [Header("═══════ STRUCTURE STATS ═══════")]
    [Range(1, 10000)]
    public int maxHealth = 300;

    public bool destructible = true;
    public bool canBeRepaired = true;

    [Tooltip("Resistencia al daño (0 = sin resistencia, 0.5 = 50% reducción)")]
    [Range(0f, 1f)]
    public float damageResistance = 0f;

    [Header("═══════ BUILD COSTS ═══════")]
    public MaterialCost[] materialCosts;

    [Header("═══════ CATEGORÍA ═══════")]
    public StructureCategory category = StructureCategory.Wall;

    [Header("═══════ REPAIR COSTS (Opcional) ═══════")]
    [Tooltip("Costo por punto de vida reparado. Si está vacío, usa materialCosts")]
    public MaterialCost[] repairCostsPerHP;

    // ═══════ VALIDATION ═══════

    void OnValidate()
    {
        // Validar que el nombre no esté vacío
        if (string.IsNullOrEmpty(structureName))
            structureName = name;

        // Validar health
        if (maxHealth < 1)
            maxHealth = 1;

        // Validar grid size
        if (gridSize.x < 1) gridSize.x = 1;
        if (gridSize.y < 1) gridSize.y = 1;
        if (gridSize.z < 1) gridSize.z = 1;
    }

    // ═══════ HELPERS ═══════

    public float GetRepairCost(string materialId, float hpToRepair)
    {
        if (string.IsNullOrEmpty(materialId) || hpToRepair <= 0f)
            return 0f;

        // Usar costos específicos de reparación si existen
        if (repairCostsPerHP != null && repairCostsPerHP.Length > 0)
        {
            foreach (var cost in repairCostsPerHP)
            {
                if (cost != null && cost.materialId == materialId)
                    return cost.amount * hpToRepair;
            }
        }

        // Fallback: usar costo de construcción proporcional
        if (materialCosts != null && maxHealth > 0)
        {
            foreach (var cost in materialCosts)
            {
                if (cost != null && cost.materialId == materialId)
                    return (cost.amount / maxHealth) * hpToRepair;
            }
        }

        return 0f;
    }

    public bool HasCost()
    {
        return materialCosts != null && materialCosts.Length > 0;
    }

    public string GetCostSummary()
    {
        if (!HasCost())
            return "Free";

        string result = "";
        foreach (var cost in materialCosts)
        {
            if (cost == null || !cost.IsValid())
                continue;

            if (!string.IsNullOrEmpty(result))
                result += ", ";

            result += $"{cost.amount} {cost.materialId}";
        }

        return string.IsNullOrEmpty(result) ? "Free" : result;
    }
}

/// <summary>
/// Categorías de estructuras
/// </summary>
public enum StructureCategory
{
    Foundation,
    Wall,
    Floor,
    Ceiling,
    Roof,
    Door,
    Window,
    Stairs,
    Ramp,
    Pillar,
    Defense,
    Furniture,
    Utility,
    Decoration
}