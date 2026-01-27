using UnityEngine;

public class PlayerFarmingTool : MonoBehaviour
{
    public float interactRange = 3f;
    public LayerMask plantSpotLayer;

    public SeedItem selectedSeed; // lo que el jugador tiene en la mano

    void Update()
    {
        //if (Input.GetMouseButtonDown(0))
        if(Input.GetKeyDown(KeyCode.F))
            {
            TryPlant();
        }

        //if (Input.GetMouseButtonDown(1))
        if(Input.GetKeyDown(KeyCode.H))
        {
            TryHarvest();
        }
    }

    void TryPlant()
    {
        if (selectedSeed == null) return;

        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward,
            out RaycastHit hit, interactRange, plantSpotLayer))
        {
            PlantSpot spot = hit.collider.GetComponent<PlantSpot>();
            if (spot != null && spot.CanPlant())
            {
                spot.PlantSeed(selectedSeed);
                Debug.Log("Seed planted!");
            }
        }
    }

    void TryHarvest()
    {
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward,
            out RaycastHit hit, interactRange))
        {
            Debug.Log("Hit: " + hit.collider.name);
            PlantInstance plant = hit.collider.GetComponentInParent<PlantInstance>();
            if (plant != null && plant.CanHarvest())
            {
                plant.Harvest();
                Debug.Log("Plant harvested!");
            }
        }
    }
}
