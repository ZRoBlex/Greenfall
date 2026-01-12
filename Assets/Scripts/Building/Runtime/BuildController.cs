using UnityEngine;

public class BuildController : MonoBehaviour
{
    public Camera cam;
    public GridSettings grid;
    public StructureConfig current;

    GameObject preview;
    Quaternion rotation = Quaternion.identity;

    void Update()
    {
        Ray r = new Ray(cam.transform.position, cam.transform.forward);
        if (!Physics.Raycast(r, out RaycastHit hit, 6f)) return;

        Vector3 pos =
            GridMath.Snap(hit.point, grid.cellSize);

        if (!preview)
            preview = Instantiate(current.previewPrefab);

        preview.transform.SetPositionAndRotation(pos, rotation);

        bool canBuild = CanBuild(preview);
        preview.GetComponent<BuildPreview>().SetValid(canBuild);

        if (Input.GetKeyDown(KeyCode.R))
            rotation *= Quaternion.Euler(0, 90, 0);

        if (canBuild && Input.GetMouseButtonDown(0))
            Place(pos);
    }

    bool CanBuild(GameObject prev)
    {
        Collider[] cols = prev.GetComponentsInChildren<Collider>();

        foreach (var col in cols)
        {
            if (!col.isTrigger) continue;

            Collider[] hits =
                Physics.OverlapBox(
                    col.bounds.center,
                    col.bounds.extents,
                    col.transform.rotation
                );

            foreach (var h in hits)
                if (!h.transform.IsChildOf(prev.transform))
                    return false;
        }

        return true;
    }

    void Place(Vector3 pos)
    {
        GameObject obj =
            Instantiate(current.finalPrefab, pos, rotation);

        obj.GetComponent<StructureInstance>()
           .Initialize(grid);
    }
}
