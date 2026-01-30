using UnityEngine;

public class PlantSpot : MonoBehaviour
{
    [Header("Spawn")]
    public Transform spawnPoint;

    [Header("Debug")]
    [SerializeField] PlantInstance currentPlant;

    void Awake()
    {
        // Si no se asigna spawnPoint, usamos este mismo transform
        if (spawnPoint == null)
            spawnPoint = transform;
    }

    // =========================
    // Estado
    // =========================
    public bool CanPlant()
    {
        return currentPlant == null;
    }

    public bool HasPlant()
    {
        return currentPlant != null;
    }

    // =========================
    // Plantar
    // =========================
    public void PlantSeed(SeedItem seed)
    {
        if (!CanPlant())
        {
            Debug.Log("PlantSpot ocupado");
            return;
        }

        if (seed == null || seed.plantPrefab == null)
        {
            Debug.LogWarning("Seed o prefab inválido");
            return;
        }

        GameObject plantGO = Instantiate(
            seed.plantPrefab,
            spawnPoint.position,
            Quaternion.identity
        );

        currentPlant = plantGO.GetComponent<PlantInstance>();

        if (currentPlant == null)
        {
            Debug.LogError("El prefab no tiene PlantInstance");
            Destroy(plantGO);
            return;
        }

        currentPlant.Initialize(seed, this);
    }

    // =========================
    // Limpieza
    // =========================
    public void ClearSpot()
    {
        currentPlant = null;
    }

    // =========================
    // Debug visual (Editor)
    // =========================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = CanPlant() ? Color.green : Color.red;
        Gizmos.DrawWireSphere(
            spawnPoint != null ? spawnPoint.position : transform.position,
            0.2f
        );
    }
}
