using UnityEngine;

public class PlayerFarmingTool : MonoBehaviour
{
    public float interactRange = 3f;

    [Header("Allowed Tags")]
    public string[] plantSpotTags = { "PlantSpot" };
    public string[] plantTags = { "Plant" };

    [Header("Ignored Tags")]
    public string[] ignoredTags = { "Untagged", "Decoration", "Grass" };

    public SeedItem selectedSeed; // lo que el jugador tiene en la mano

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryPlant();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            TryHarvest();
        }
    }

    void TryPlant()
    {
        if (selectedSeed == null) return;

        if (RaycastFiltered(out RaycastHit hit))
        {
            // 🟢 PARCELA CON MÚLTIPLES PUNTOS
            FarmPlot plot = hit.collider.GetComponent<FarmPlot>();
            if (plot != null)
            {
                PlantSpot freeSpot = plot.GetFreeSpot();
                if (freeSpot != null)
                {
                    freeSpot.PlantSeed(selectedSeed);
                    Debug.Log("Seed planted in plot!");
                }
                return;
            }

            // 🟢 PLANTAR DIRECTO EN UN SPOT
            PlantSpot spot = hit.collider.GetComponent<PlantSpot>();
            if (spot != null && spot.CanPlant())
            {
                spot.PlantSeed(selectedSeed);
                Debug.Log("Seed planted!");
            }
        }

        GroundPlantable ground = hit.collider.GetComponent<GroundPlantable>();
        if (ground != null)
        {
            Vector3 plantPos = hit.point;

            if (!ground.CanPlantAt(plantPos))
                return;

            GameObject plantGO = Instantiate(
                selectedSeed.plantPrefab,
                plantPos,
                Quaternion.identity
            );

            PlantInstance plant = plantGO.GetComponent<PlantInstance>();
            if (plant == null)
            {
                Destroy(plantGO);
                return;
            }

            ground.RegisterPlant(plantPos);

            plant.InitializeOnGround(
                selectedSeed,
                ground,
                plantPos
            );
        }
        


    }


    void TryHarvest()
    {
        if (RaycastFiltered(out RaycastHit hit))
        {
            Debug.Log("Hit: " + hit.collider.name + " | Tag: " + hit.collider.tag);

            PlantInstance plant = hit.collider.GetComponentInParent<PlantInstance>();
            if (plant == null)
                return;

            if (!HasAnyTag(plant.gameObject, plantTags))
                return;

            if (plant.CanHarvest())
            {
                plant.Harvest();
                Debug.Log("Plant harvested!");
            }
        }
    }

    // =========================
    // Raycast con filtro por tags
    // =========================
    bool RaycastFiltered(out RaycastHit finalHit)
    {
        Vector3 origin = Camera.main.transform.position;
        Vector3 direction = Camera.main.transform.forward;

        float remainingDistance = interactRange;
        Vector3 currentOrigin = origin;

        while (remainingDistance > 0f)
        {
            if (!Physics.Raycast(
                    currentOrigin,
                    direction,
                    out RaycastHit hit,
                    remainingDistance,
                    ~0,
                    QueryTriggerInteraction.Ignore))
            {
                break; // no golpeó nada más
            }

            // Si el tag está en la lista de ignorados → seguir de largo
            if (HasAnyTag(hit.collider, ignoredTags))
            {
                float traveled = hit.distance + 0.01f;
                remainingDistance -= traveled;
                currentOrigin = hit.point + direction * 0.01f;
                continue;
            }

            // Este hit ya es válido
            finalHit = hit;
            return true;
        }

        finalHit = default;
        return false;
    }

    // =========================
    // Helpers
    // =========================

    bool HasAnyTag(Collider col, string[] tags)
    {
        foreach (var t in tags)
        {
            if (col.CompareTag(t))
                return true;
        }
        return false;
    }

    bool HasAnyTag(GameObject go, string[] tags)
    {
        foreach (var t in tags)
        {
            if (go.CompareTag(t))
                return true;
        }
        return false;
    }
}
