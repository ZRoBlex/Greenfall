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

        // 1️⃣ Base position: SIEMPRE hit.point
        Vector3 targetPos = hit.point;

        // 2️⃣ Rotación
        Quaternion rot = Quaternion.Euler(
            0,
            Mathf.Round(cam.transform.eulerAngles.y / 90f) * 90f,
            0
        );

        // 3️⃣ Normal estable
        Vector3 normal = hit.normal;
        if (Vector3.Angle(normal, Vector3.up) < 45f)
            normal = Vector3.up;

        // 4️⃣ Snap a superficie (corrige pivot centrado)
        targetPos = BuildSnapSurface.SnapToSurface(
            Current.finalPrefab,
            targetPos,
            normal,
            rot
        );

        // 5️⃣ Grid snap DETERMINISTA
        if (Current.useGrid)
        {
            targetPos = grid.Snap(targetPos, ray.direction);
        }

        // 6️⃣ Validación (lógica)
        bool valid = validator.CanPlace(Current, targetPos, rot, preview);

        // 7️⃣ Suavizado SOLO visual
        visualPos = Vector3.Lerp(
            visualPos == Vector3.zero ? targetPos : visualPos,
            targetPos,
            Time.deltaTime * previewSmoothSpeed
        );

        // 8️⃣ Mostrar preview
        preview.Show(Current, visualPos, rot, valid);

        // 9️⃣ Construir
        if (valid && Input.GetMouseButtonDown(0))
        {
            Place(targetPos, rot);
        }
    }

    void Place(Vector3 pos, Quaternion rot)
    {
        GameObject obj = Instantiate(Current.finalPrefab, pos, rot);
        var volume = obj.GetComponentInChildren<OccupancyVolume>();
        occupancy.Register(volume.GetWorldBounds(), grid.cellSize);
    }

    public void ForceHidePreview()
    {
        preview.Hide();
        visualPos = Vector3.zero;
    }
}
