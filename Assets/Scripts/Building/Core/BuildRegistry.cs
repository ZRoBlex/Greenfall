using UnityEngine;
using System.Collections.Generic;

public static class BuildRegistry
{
    public static Dictionary<Vector3Int, StructureInstance> occupied =
        new Dictionary<Vector3Int, StructureInstance>();

    public static bool IsCellFree(Vector3Int cell)
    {
        return !occupied.ContainsKey(cell);
    }

    public static void Register(StructureInstance inst)
    {
        foreach (var cell in inst.occupiedCells)
            occupied[cell] = inst;
    }

    public static void Unregister(StructureInstance inst)
    {
        foreach (var cell in inst.occupiedCells)
            occupied.Remove(cell);
    }
}
