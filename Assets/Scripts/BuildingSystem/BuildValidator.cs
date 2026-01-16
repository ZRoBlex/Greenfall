using UnityEngine;

public class BuildValidator : MonoBehaviour
{
    public GridSystem grid;
    public WorldOccupancy occupancy;

    public bool CanPlace(
        StructureData data,
        Vector3 pos,
        Quaternion rot,
        BuildPreview preview)
    {
        if (preview.Blocked())
            return false;

        if (data.allowOverlap)
            return true;

        GameObject ghost = Instantiate(data.finalPrefab, pos, rot);
        var volume = ghost.GetComponentInChildren<OccupancyVolume>();

        bool blocked = occupancy.IsOccupied(
            volume.GetWorldBounds(),
            grid.cellSize
        );



        Destroy(ghost);

        return !blocked;
    }
}
