using UnityEngine;
using System.Collections.Generic;

public class StructureInstance : MonoBehaviour
{
    public List<Vector3Int> occupiedCells = new();

    public void Initialize(GridSettings grid)
    {
        Collider[] colliders =
            GetComponentsInChildren<Collider>();

        foreach (var col in colliders)
        {
            if (!col.isTrigger) continue;

            Bounds b = col.bounds;

            Vector3Int min =
                GridMath.WorldToCell(b.min, grid.cellSize);
            Vector3Int max =
                GridMath.WorldToCell(b.max, grid.cellSize);

            for (int x = min.x; x <= max.x; x++)
                for (int y = min.y; y <= max.y; y++)
                    for (int z = min.z; z <= max.z; z++)
                        occupiedCells.Add(new Vector3Int(x, y, z));
        }

        BuildRegistry.Register(this);
    }
}
