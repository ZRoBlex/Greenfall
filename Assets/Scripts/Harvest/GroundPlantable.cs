using UnityEngine;
using System.Collections.Generic;

public class GroundPlantable : MonoBehaviour
{
    [Header("Planting")]
    public int maxPlants = 20;
    public float minDistanceBetweenPlants = 1f;

    List<Vector3> occupiedPositions = new List<Vector3>();

    public bool CanPlantAt(Vector3 position)
    {
        // 🟢 VALIDAR BIOMA
        if (BiomeMap.Instance != null)
        {
            BiomeDefinition biome = BiomeMap.Instance.GetBiomeDefinition(position);

            if (biome == null || !biome.allowPlanting)
                return false;
        }

        // 🟢 LIMITE DE PLANTAS
        if (occupiedPositions.Count >= maxPlants)
            return false;

        // 🟢 DISTANCIA ENTRE PLANTAS
        foreach (var pos in occupiedPositions)
        {
            if (Vector3.Distance(pos, position) < minDistanceBetweenPlants)
                return false;
        }

        return true;
    }


    public void RegisterPlant(Vector3 position)
    {
        occupiedPositions.Add(position);
    }

    public void UnregisterPlant(Vector3 position)
    {
        occupiedPositions.Remove(position);
    }
}
