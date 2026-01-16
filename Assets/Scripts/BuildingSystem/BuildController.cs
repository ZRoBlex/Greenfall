using UnityEngine;

public class BuildController : MonoBehaviour
{
    [Header("General")]
    public Camera cam;
    public float range = 6f;

    [Header("References")]
    public BuildSelector selector;
    public GridSystem grid;
    public BuildPreview preview;
    public BuildValidator validator;
    public WorldOccupancy occupancy;

    [Header("Visual")]
    public float previewSmoothSpeed = 25f;

    StructureData Current => selector.Current;

    Vector3 visualPos;

    void Update()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, range))
        {
            preview.Hide();
            return;
        }

        StructureData data = Current;

        // 🔒 1. Obtener celda DETERMINISTA (NO hit.point directo)
        Vector3Int cell = grid.WorldToCellStable(
            hit.point,
            ray.direction
        );

        // 🔒 2. Celda → mundo (estable)
        Vector3 targetPos = grid.CellToWorld(cell);

        // 🔒 3. Rotación snap 90°
        Quaternion rot = Quaternion.Euler(
            0,
            Mathf.Round(cam.transform.eulerAngles.y / 90f) * 90f,
            0
        );

        // 🔒 4. Ajuste de pivote (una sola vez, estable)
        targetPos += BuildSnapUtility.GetBottomOffset(
            data.finalPrefab,
            rot
        );

        bool valid = validator.CanPlace(data, targetPos, rot, preview);

        // 🔒 5. Suavizado SOLO visual
        visualPos = Vector3.Lerp(
            visualPos == Vector3.zero ? targetPos : visualPos,
            targetPos,
            Time.deltaTime * previewSmoothSpeed
        );

        preview.Show(data, visualPos, rot, valid);

        if (valid && Input.GetMouseButtonDown(0))
            Place(targetPos, rot);
    }


    void Place(Vector3 pos, Quaternion rot)
    {
        GameObject obj =
    StructurePool.Instance.Get(Current.finalPrefab, pos, rot);

        var instance = obj.GetComponent<StructureInstance>();
        instance.prefab = Current.finalPrefab;

        var health = obj.GetComponent<StructureHealth>();
        health.data = Current;
        health.ResetHealth();

        //GameObject obj = Instantiate(Current.finalPrefab, pos, rot);
        //var volume = obj.GetComponentInChildren<OccupancyVolume>();
        //occupancy.Register(volume.GetWorldBounds(), grid.cellSize);
    }

    public void ForceHidePreview()
    {
        preview.Hide();
        visualPos = Vector3.zero;
    }
}
