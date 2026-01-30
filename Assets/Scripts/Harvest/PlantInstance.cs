using UnityEngine;

public class PlantInstance : MonoBehaviour
{
    public SeedItem seedData;

    [Header("Growth")]
    public float growTimer;
    public bool isGrown;

    [Header("Visuals")]
    public bool useStageModels = false; // 👈 checkbox que pediste
    public GameObject sproutModel;
    public GameObject grownModel;
    public Transform visualRoot; // para modo escala

    GroundPlantable groundOwner;
    Vector3 registeredPosition;

    PlantSpot mySpot;

    void Update()
    {
        if (seedData == null || isGrown) return;

        growTimer += Time.deltaTime;
        float t = Mathf.Clamp01(growTimer / seedData.growTime);

        if (useStageModels)
        {
            if (sproutModel != null) sproutModel.SetActive(t < 1f);
            if (grownModel != null) grownModel.SetActive(t >= 1f);
        }
        else
        {
            if (visualRoot != null)
                visualRoot.localScale = Vector3.one * Mathf.Lerp(0.1f, 1f, t);
        }

        if (t >= 1f)
            isGrown = true;
    }

    public void Initialize(SeedItem seed, PlantSpot spot)
    {
        seedData = seed;
        mySpot = spot;

        growTimer = 0f;
        isGrown = false;

        if (useStageModels)
        {
            if (sproutModel != null) sproutModel.SetActive(true);
            if (grownModel != null) grownModel.SetActive(false);
        }
        else
        {
            if (visualRoot != null)
                visualRoot.localScale = Vector3.one * 0.1f;
        }

        ResetGrowth();
    }

    // 🟢 NUEVO: inicializar desde suelo
    public void InitializeOnGround(
    SeedItem seed,
    GroundPlantable ground,
    Vector3 position
)
    {
        seedData = seed;
        groundOwner = ground;
        registeredPosition = position;

        ResetGrowth();
    }


    // 👇 ESTO ARREGLA TU ERROR ACTUAL
    public bool CanHarvest()
    {
        return isGrown;
    }

    public void Harvest()
    {
        if (!isGrown) return;

        // Drops de cultivo
        if (seedData.cropPrefab != null)
        {
            int amount = Random.Range(seedData.minCropAmount, seedData.maxCropAmount + 1);
            for (int i = 0; i < amount; i++)
                Instantiate(seedData.cropPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }

        // Devolver semillas
        int seedReturn = Random.Range(seedData.minSeedReturn, seedData.maxSeedReturn + 1);
        if (seedReturn > 0 && seedData.seedPickupPrefab != null)
        {
            GameObject seedGO = Instantiate(
                seedData.seedPickupPrefab,
                transform.position + Vector3.up * 0.5f,
                Quaternion.identity
            );

            SeedPickup pickup = seedGO.GetComponent<SeedPickup>();
            if (pickup != null)
            {
                pickup.Initialize(seedData, seedReturn);
            }
        }

        GroundPlantable ground = GetComponentInParent<GroundPlantable>();
        if (ground != null)
        {
            ground.UnregisterPlant(transform.position);
        }



        // 🟢 liberar spot lógico
        if (mySpot != null)
            mySpot.ClearSpot();

        // 🟢 liberar suelo lógico
        if (groundOwner != null)
            groundOwner.UnregisterPlant(registeredPosition);

        Destroy(gameObject);

    }

    void ResetGrowth()
    {
        growTimer = 0f;
        isGrown = false;

        if (useStageModels)
        {
            if (sproutModel != null) sproutModel.SetActive(true);
            if (grownModel != null) grownModel.SetActive(false);
        }
        else
        {
            if (visualRoot != null)
                visualRoot.localScale = Vector3.one * 0.1f;
        }
    }

}
