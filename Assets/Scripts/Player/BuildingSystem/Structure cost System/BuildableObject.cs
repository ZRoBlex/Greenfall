using UnityEngine;

/// <summary>
/// Objeto construible optimizado
/// Compatible con PlayerStats optimizado
/// </summary>
public class BuildableObject : MonoBehaviour
{
    [Header("═══════ COSTOS ═══════")]
    public MaterialCost[] buildCosts;

    [Header("═══════ VISUALS ═══════")]
    public GameObject builtVersion;
    public GameObject ghostVersion;

    [Header("═══════ ESTADO ═══════")]
    [SerializeField] bool isBuilt = false;

    // ═══════ PROPIEDADES ═══════

    public bool IsBuilt => isBuilt;

    // ═══════ API PÚBLICA ═══════

    public bool CanBuild(PlayerStats stats)
    {
        if (isBuilt || stats == null)
            return false;

        if (buildCosts == null || buildCosts.Length == 0)
            return true;

        return stats.HasMaterials(buildCosts);
    }

    public bool TryBuild(PlayerStats stats)
    {
        if (isBuilt)
        {
            Debug.Log("⚠️ Ya está construido");
            return false;
        }

        if (stats == null)
        {
            Debug.LogError("❌ PlayerStats es null");
            return false;
        }

        // Si no tiene costos, construir directamente
        if (buildCosts == null || buildCosts.Length == 0)
        {
            Build();
            return true;
        }

        if (!stats.ConsumeMaterials(buildCosts))
        {
            Debug.Log("❌ No hay suficientes recursos");
            return false;
        }

        Build();
        return true;
    }

    void Build()
    {
        isBuilt = true;

        if (ghostVersion != null)
            ghostVersion.SetActive(false);

        if (builtVersion != null)
            builtVersion.SetActive(true);

        Debug.Log("✅ Construcción completada");
    }

    public void ResetToBuildable()
    {
        isBuilt = false;

        if (ghostVersion != null)
            ghostVersion.SetActive(true);

        if (builtVersion != null)
            builtVersion.SetActive(false);
    }

    // ═══════ HELPERS ═══════

    public string GetCostSummary()
    {
        if (buildCosts == null || buildCosts.Length == 0)
            return "Free";

        string result = "";
        foreach (var cost in buildCosts)
        {
            if (!cost.IsValid())
                continue;

            if (!string.IsNullOrEmpty(result))
                result += ", ";

            result += $"{cost.amount} {cost.materialId}";
        }

        return string.IsNullOrEmpty(result) ? "Free" : result;
    }
}