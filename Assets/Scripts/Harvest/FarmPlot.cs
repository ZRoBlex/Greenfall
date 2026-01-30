using UnityEngine;
using System.Collections.Generic;

public class FarmPlot : MonoBehaviour
{
    public List<PlantSpot> spots = new List<PlantSpot>();

    public PlantSpot GetFreeSpot()
    {
        foreach (var spot in spots)
        {
            if (spot.CanPlant())
                return spot;
        }
        return null;
    }
}
