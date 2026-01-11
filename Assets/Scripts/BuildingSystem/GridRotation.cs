using UnityEngine;

public static class GridRotation
{
    public static Vector3Int RotateCell(
        Vector3Int cell,
        Vector3Int gridSize,
        StructureRotation rotation
    )
    {
        return rotation switch
        {
            StructureRotation.Deg90 =>
                new Vector3Int(
                    gridSize.z - 1 - cell.z,
                    cell.y,
                    cell.x
                ),

            StructureRotation.Deg180 =>
                new Vector3Int(
                    gridSize.x - 1 - cell.x,
                    cell.y,
                    gridSize.z - 1 - cell.z
                ),

            StructureRotation.Deg270 =>
                new Vector3Int(
                    cell.z,
                    cell.y,
                    gridSize.x - 1 - cell.x
                ),

            _ => cell
        };
    }
}
