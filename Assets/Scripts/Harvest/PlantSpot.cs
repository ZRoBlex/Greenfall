using UnityEngine;

public class PlantSpot : MonoBehaviour
{
    public Transform spawnPoint;

    PlantInstance currentPlant;

    public bool CanPlant()
    {
        return currentPlant == null;
    }

    public void PlantSeed(SeedItem seed)
    {
        if (currentPlant != null) return;
        if (seed == null || seed.plantPrefab == null) return;

        GameObject obj = Instantiate(seed.plantPrefab, spawnPoint.position, Quaternion.identity);
        currentPlant = obj.GetComponent<PlantInstance>();

        if (currentPlant == null)
        {
            Debug.LogError("El prefab no tiene PlantInstance.");
            Destroy(obj);
            return;
        }

        currentPlant.Initialize(seed, this);
    }

    public void ClearSpot()
    {
        currentPlant = null;
    }
}
