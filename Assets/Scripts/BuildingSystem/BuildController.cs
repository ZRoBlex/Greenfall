using UnityEngine;

public class BuildController : MonoBehaviour
{
    [Header("General")]
    public Camera cam;
    public float range = 6f;

    [Header("Vertical Snap")]
    public LayerMask snapSurfaceMask;
    public float verticalSnapHeight = 5f;

    [Header("Raycast")]
    public LayerMask buildRayMask;
    public bool ignoreTriggers = true;

    [Header("References")]
    public BuildSelector selector;
    public GridSystem grid;
    public BuildPreview preview;
    public BuildValidator validator;
    public WorldOccupancy occupancy;

    [Header("Rotation")]
    public float rotationStep = 90f;
    float manualRotationOffset = 0f;   // Solo lo que agrega la tecla R

    [Header("Visual")]
    public float previewSmoothSpeed = 25f;

    [SerializeField] private PlayerStats playerStats;

    StructureData Current => selector.Current;

    Vector3 visualPos;

    void Update()
    {
        // =========================
        // 1. INPUT DE ROTACIÓN (R)
        // =========================
        if (Input.GetKeyDown(KeyCode.R))
        {
            manualRotationOffset += rotationStep;
            if (manualRotationOffset >= 360f)
                manualRotationOffset = 0f;
        }

        // =========================
        // 2. RAYCAST PRINCIPAL
        // =========================
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        QueryTriggerInteraction triggerMode =
            ignoreTriggers ? QueryTriggerInteraction.Ignore
                           : QueryTriggerInteraction.Collide;

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                range,
                buildRayMask,
                triggerMode))
        {
            preview.Hide();
            return;
        }

        StructureData data = Current;

        // =========================
        // 3. ROTACIÓN BASE POR CÁMARA
        // =========================

        // Snap de la cámara a 90°
        float camYaw = cam.transform.eulerAngles.y;
        float snappedCamYaw = Mathf.Round(camYaw / 90f) * 90f;

        // Rotación final = cámara + offset manual con R
        float finalYaw = snappedCamYaw + manualRotationOffset;
        Quaternion rot = Quaternion.Euler(0f, finalYaw, 0f);

        // =========================
        // 4. POSICIÓN BASE POR GRID
        // =========================

        Vector3Int cell = grid.WorldToCellStable(
            hit.point,
            ray.direction
        );

        Vector3 targetPos = grid.CellToWorld(cell);

        // =========================
        // 5. SNAP VERTICAL
        // =========================

        targetPos = ApplyVerticalSnap(targetPos, rot, data);

        // =========================
        // 6. AJUSTE DE PIVOTE
        // =========================

        targetPos += BuildSnapUtility.GetBottomOffset(
            data.finalPrefab,
            rot
        );

        // =========================
        // 7. VALIDACIÓN
        // =========================

        bool valid = validator.CanPlace(data, targetPos, rot, preview);

        // =========================
        // 8. SUAVIZADO VISUAL
        // =========================

        visualPos = Vector3.Lerp(
            visualPos == Vector3.zero ? targetPos : visualPos,
            targetPos,
            Time.deltaTime * previewSmoothSpeed
        );

        preview.Show(data, visualPos, rot, valid);

        // =========================
        // 9. COLOCAR
        // =========================

        if (valid && Input.GetMouseButtonDown(0))
            Place(targetPos, rot);
    }

    // =====================================================
    // COLOCAR ESTRUCTURA
    // =====================================================

    void Place(Vector3 pos, Quaternion rot)
    {
        StructureData data = Current;

        // 🔒 1) Verificar materiales
        if (data.materialCosts != null && data.materialCosts.Length > 0)
        {
            if (!playerStats.HasMaterials(data.materialCosts))
            {
                Debug.Log("❌ No tienes suficientes materiales");
                return;
            }
        }

        // 🔨 2) Consumir materiales
        if (data.materialCosts != null && data.materialCosts.Length > 0)
        {
            playerStats.ConsumeMaterials(data.materialCosts);
        }

        // 🏗️ 3) Spawnear estructura
        GameObject obj =
            StructurePool.Instance.Get(data.finalPrefab, pos, rot);

        var instance = obj.GetComponent<StructureInstance>();
        instance.prefab = data.finalPrefab;

        var health = obj.GetComponent<StructureHealth>();
        health.data = data;
        health.ResetHealth();
    }


    public void ForceHidePreview()
    {
        preview.Hide();
        visualPos = Vector3.zero;
    }

    // =====================================================
    // SNAP VERTICAL LIMPIO
    // =====================================================

    Vector3 ApplyVerticalSnap(Vector3 basePos, Quaternion rot, StructureData data)
    {
        Vector3 origin = basePos + Vector3.up * verticalSnapHeight;

        if (Physics.Raycast(
                origin,
                Vector3.down,
                out RaycastHit hit,
                verticalSnapHeight * 2f,
                snapSurfaceMask,
                QueryTriggerInteraction.Ignore))
        {
            float surfaceY = hit.point.y;

            Vector3 offset = BuildSnapUtility.GetBottomOffset(
                data.finalPrefab,
                rot
            );

            Vector3 snapped = new Vector3(
                basePos.x,
                surfaceY,
                basePos.z
            );

            snapped += offset;
            return snapped;
        }

        return basePos;
    }
}
