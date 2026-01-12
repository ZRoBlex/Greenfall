using UnityEngine;
using System.Collections.Generic;

public class WorldGridGizmos : MonoBehaviour
{
    public GridSettings gridSettings;
    public float viewRadius = 20f;
    public Color gridColor = new Color(1, 1, 1, 0.15f);
    public Color occupiedColor = new Color(1, 0, 0, 0.4f);

    void OnDrawGizmos()
    {
        if (gridSettings == null)
            return;

        float cellSize = gridSettings.cellSize;
        Vector3 center = transform.position;

        int cells = Mathf.CeilToInt(viewRadius / cellSize);

        Vector3Int centerCell = new Vector3Int(
            Mathf.FloorToInt(center.x / cellSize),
            Mathf.FloorToInt(center.y / cellSize),
            Mathf.FloorToInt(center.z / cellSize)
        );

        Gizmos.color = gridColor;

        for (int x = -cells; x <= cells; x++)
            for (int z = -cells; z <= cells; z++)
            {
                Vector3Int cell = new Vector3Int(
                    centerCell.x + x,
                    centerCell.y,
                    centerCell.z + z
                );

                Vector3 world = new Vector3(
                    cell.x * cellSize,
                    cell.y * cellSize,
                    cell.z * cellSize
                );

                Gizmos.DrawWireCube(
                    world + Vector3.one * cellSize * 0.5f,
                    Vector3.one * cellSize
                );
            }

        DrawOccupiedCells(cellSize);
    }

    void DrawOccupiedCells(float cellSize)
    {
        var field = typeof(BuildController)
            .GetField("occupiedCells",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Static);

        if (field == null)
            return;

        var dict = field.GetValue(null)
            as Dictionary<Vector3Int, GameObject>;

        if (dict == null)
            return;

        Gizmos.color = occupiedColor;

        foreach (var cell in dict.Keys)
        {
            Vector3 world = new Vector3(
                cell.x * cellSize,
                cell.y * cellSize,
                cell.z * cellSize
            );

            Gizmos.DrawCube(
                world + Vector3.one * cellSize * 0.5f,
                Vector3.one * cellSize
            );
        }
    }
}
