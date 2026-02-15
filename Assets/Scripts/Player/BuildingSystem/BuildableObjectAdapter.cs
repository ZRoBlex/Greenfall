using UnityEngine;

/// <summary>
/// Adaptador para hacer BuildableObject interactuable
/// ULTRA OPTIMIZADO - Cache agresivo
/// </summary>
[RequireComponent(typeof(BuildableObject))]
public class BuildableObjectAdapter : MonoBehaviour, IInteractable
{
    // ═══════ CACHE ═══════

    BuildableObject buildable;
    PlayerStats cachedStats;
    string cachedCostText;
    bool costTextDirty = true;

    // ═══════ LIFECYCLE ═══════

    void Awake()
    {
        buildable = GetComponent<BuildableObject>();
    }

    void OnEnable()
    {
        costTextDirty = true;
    }

    // ═══════ IInteractable ═══════

    public string GetInteractText()
    {
        if (buildable == null)
            return "Build";

        if (buildable.IsBuilt)
            return "Already Built";

        // Cache del texto de costos
        if (costTextDirty)
        {
            cachedCostText = GetCostText();
            costTextDirty = false;
        }

        return string.IsNullOrEmpty(cachedCostText)
            ? "Build Structure"
            : $"Build Structure\n{cachedCostText}";
    }

    public bool CanInteract(GameObject interactor)
    {
        if (buildable == null || buildable.IsBuilt)
            return false;

        // Cache de PlayerStats
        if (cachedStats == null)
            cachedStats = interactor.GetComponent<PlayerStats>();

        return cachedStats != null && buildable.CanBuild(cachedStats);
    }

    public void Interact(GameObject interactor)
    {
        if (cachedStats == null)
            cachedStats = interactor.GetComponent<PlayerStats>();

        if (buildable != null && cachedStats != null)
        {
            if (buildable.TryBuild(cachedStats))
                costTextDirty = true; // Invalidar cache si se construyó
        }
    }

    public int GetPriority() => 8;

    // ═══════ HELPERS ═══════

    string GetCostText()
    {
        if (buildable == null)
            return "";

        string summary = buildable.GetCostSummary();

        return summary == "Free"
            ? ""
            : $"<size=70%>({summary})</size>";
    }
}