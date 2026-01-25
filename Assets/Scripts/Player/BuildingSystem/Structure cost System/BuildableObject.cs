using UnityEngine;

public class BuildableObject : MonoBehaviour
{
    [Header("Build Cost")]
    public ResourceCost[] buildCosts;

    [Header("Visuals")]
    public GameObject builtVersion;
    public GameObject ghostVersion;

    private bool isBuilt = false;

    public bool CanBuild(PlayerStats stats)
    {
        return !isBuilt && stats.HasResources(buildCosts);
    }

    public void TryBuild(PlayerStats stats)
    {
        if (isBuilt) return;

        if (!stats.ConsumeResources(buildCosts))
        {
            Debug.Log("No hay suficientes recursos");
            return;
        }

        // 🔨 Construir
        isBuilt = true;

        if (ghostVersion != null) ghostVersion.SetActive(false);
        if (builtVersion != null) builtVersion.SetActive(true);

        Debug.Log("Construcción completada");
    }
}
