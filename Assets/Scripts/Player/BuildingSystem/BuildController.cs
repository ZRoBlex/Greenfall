using UnityEngine;

/// <summary>
/// Sistema de construcción ULTRA optimizado
/// - Cache agresivo de componentes
/// - Validaciones tempranas
/// - Sin allocations en Update
/// </summary>
public class BuildController : MonoBehaviour
{
    [Header("═══════ GENERAL ═══════")]
    [SerializeField] Camera cam;

    [Range(1f, 20f)]
    [SerializeField] float range = 6f;

    [Header("═══════ VERTICAL SNAP ═══════")]
    [SerializeField] LayerMask snapSurfaceMask;

    [Range(1f, 10f)]
    [SerializeField] float verticalSnapHeight = 5f;

    [Header("═══════ RAYCAST ═══════")]
    [SerializeField] LayerMask buildRayMask;
    [SerializeField] bool ignoreTriggers = true;

    [Header("═══════ REFERENCIAS ═══════")]
    [SerializeField] BuildSelector selector;
    [SerializeField] GridSystem grid;
    [SerializeField] BuildPreview preview;
    [SerializeField] BuildValidator validator;
    [SerializeField] WorldOccupancy occupancy;
    [SerializeField] PlayerStats playerStats;

    [Header("═══════ ROTACIÓN ═══════")]
    [Range(15f, 90f)]
    [SerializeField] float rotationStep = 90f;

    [Header("═══════ VISUAL ═══════")]
    [Range(5f, 50f)]
    [SerializeField] float previewSmoothSpeed = 25f;

    // ═══════ ESTADO ═══════

    Vector3 visualPos;
    Vector3 lastValidPos;
    Quaternion lastValidRot;
    bool hasValidPlacement;
    float manualRotationOffset;

    // ═══════ CACHE ═══════

    Transform camTransform;
    StructureData currentData;

    // Cache para raycast
    Ray currentRay;
    RaycastHit currentHit;
    bool hasHit;

    // ═══════ PROPIEDADES ═══════

    StructureData Current
    {
        get
        {
            if (selector == null)
                return null;

            // Cache simple
            var selected = selector.Current;
            if (selected != currentData)
                currentData = selected;

            return currentData;
        }
    }

    // ═══════ LIFECYCLE ═══════

    void Awake()
    {
        InitializeReferences();
    }

    void InitializeReferences()
    {
        if (cam == null)
            cam = Camera.main;

        if (cam != null)
            camTransform = cam.transform;

        if (playerStats == null)
            playerStats = GetComponentInParent<PlayerStats>();

        if (playerStats == null)
        {
            Debug.LogError("❌ BuildController: No se encontró PlayerStats");
            enabled = false;
        }
    }

    void Update()
    {
        HandleRotationInput();
        UpdatePlacement();
        HandlePlaceInput();
    }

    // ═══════ INPUT ═══════

    void HandleRotationInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            manualRotationOffset += rotationStep;

            if (manualRotationOffset >= 360f)
                manualRotationOffset = 0f;
        }
    }

    void HandlePlaceInput()
    {
        if (hasValidPlacement && Input.GetMouseButtonDown(0))
            Place(lastValidPos, lastValidRot);
    }

    // ═══════ PLACEMENT ═══════

    void UpdatePlacement()
    {
        hasValidPlacement = false;

        // Early exit si no hay estructura seleccionada
        if (Current == null)
        {
            HidePreview();
            return;
        }

        // Early exit si no hay raycast hit
        if (!PerformRaycast())
        {
            HidePreview();
            return;
        }

        // Calcular transformación
        Quaternion rotation = CalculateRotation();
        Vector3 position = CalculatePosition(currentHit, rotation, Current);

        // Validar
        bool valid = ValidatePlacement(Current, position, rotation);

        // Guardar si válido
        if (valid)
        {
            hasValidPlacement = true;
            lastValidPos = position;
            lastValidRot = rotation;
        }

        // Visual
        UpdateVisualPosition(position);

        // Mostrar preview
        ShowPreview(Current, visualPos, rotation, valid);
    }

    bool PerformRaycast()
    {
        if (camTransform == null)
            return false;

        currentRay.origin = camTransform.position;
        currentRay.direction = camTransform.forward;

        QueryTriggerInteraction triggerMode = ignoreTriggers
            ? QueryTriggerInteraction.Ignore
            : QueryTriggerInteraction.Collide;

        hasHit = Physics.Raycast(currentRay, out currentHit, range, buildRayMask, triggerMode);
        return hasHit;
    }

    Quaternion CalculateRotation()
    {
        if (camTransform == null)
            return Quaternion.identity;

        float camYaw = camTransform.eulerAngles.y;
        float snappedCamYaw = Mathf.Round(camYaw / 90f) * 90f;
        float finalYaw = snappedCamYaw + manualRotationOffset;

        return Quaternion.Euler(0f, finalYaw, 0f);
    }

    Vector3 CalculatePosition(RaycastHit hit, Quaternion rotation, StructureData data)
    {
        if (grid == null || data == null)
            return hit.point;

        // Grid snap
        Vector3Int cell = grid.WorldToCellStable(hit.point, hit.normal);
        Vector3 position = grid.CellToWorld(cell);

        // Vertical snap
        position = ApplyVerticalSnap(position, rotation, data);

        // Pivot offset
        if (data.finalPrefab != null)
            position += BuildSnapUtility.GetBottomOffset(data.finalPrefab, rotation);

        return position;
    }

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

            Vector3 offset = Vector3.zero;
            if (data != null && data.finalPrefab != null)
                offset = BuildSnapUtility.GetBottomOffset(data.finalPrefab, rot);

            return new Vector3(basePos.x, surfaceY, basePos.z) + offset;
        }

        return basePos;
    }

    bool ValidatePlacement(StructureData data, Vector3 position, Quaternion rotation)
    {
        if (data == null)
            return false;

        // Validación de reglas
        bool canPlaceByRules = validator == null || validator.CanPlace(data, position, rotation, preview);

        // Validación de materiales
        bool hasMaterials = ValidateMaterials(data);

        return canPlaceByRules && hasMaterials;
    }

    bool ValidateMaterials(StructureData data)
    {
        if (playerStats == null || data == null)
            return false;

        if (data.materialCosts == null || data.materialCosts.Length == 0)
            return true;

        return playerStats.HasMaterials(data.materialCosts);
    }

    void UpdateVisualPosition(Vector3 targetPos)
    {
        if (visualPos == Vector3.zero)
            visualPos = targetPos;

        visualPos = Vector3.Lerp(visualPos, targetPos, Time.deltaTime * previewSmoothSpeed);
    }

    // ═══════ PREVIEW ═══════

    void ShowPreview(StructureData data, Vector3 pos, Quaternion rot, bool valid)
    {
        if (preview != null)
            preview.Show(data, pos, rot, valid);
    }

    void HidePreview()
    {
        if (preview != null)
            preview.Hide();

        visualPos = Vector3.zero;
    }

    // ═══════ CONSTRUCCIÓN ═══════

    void Place(Vector3 pos, Quaternion rot)
    {
        StructureData data = Current;

        if (data == null)
        {
            Debug.LogWarning("⚠️ No hay estructura seleccionada");
            return;
        }

        // Validar materiales
        if (!ValidateMaterials(data))
        {
            Debug.Log("❌ No tienes suficientes materiales");
            return;
        }

        // Consumir materiales
        if (data.materialCosts != null && data.materialCosts.Length > 0)
        {
            if (!playerStats.ConsumeMaterials(data.materialCosts))
            {
                Debug.LogError("❌ Error consumiendo materiales");
                return;
            }
        }

        // Spawnear estructura
        if (!SpawnStructure(data, pos, rot))
        {
            // Devolver materiales si falla el spawn
            if (data.materialCosts != null && data.materialCosts.Length > 0)
            {
                foreach (var cost in data.materialCosts)
                {
                    if (cost != null && cost.IsValid())
                        playerStats.AddMaterials(cost.materialId, cost.amount);
                }
            }
            return;
        }

        Debug.Log($"✅ Estructura {data.structureName} construida");
    }

    bool SpawnStructure(StructureData data, Vector3 pos, Quaternion rot)
    {
        if (data.finalPrefab == null)
        {
            Debug.LogError("❌ finalPrefab es null en StructureData");
            return false;
        }

        GameObject obj = StructurePool.Instance != null
            ? StructurePool.Instance.Get(data.finalPrefab, pos, rot)
            : Instantiate(data.finalPrefab, pos, rot);

        if (obj == null)
        {
            Debug.LogError("❌ Error spawneando estructura");
            return false;
        }

        // Configurar instance
        var instance = obj.GetComponent<StructureInstance>();
        if (instance != null)
            instance.prefab = data.finalPrefab;

        // Configurar health
        var health = obj.GetComponent<StructureHealth>();
        if (health != null)
        {
            health.data = data;
            health.ResetHealth();
        }

        return true;
    }

    // ═══════ API PÚBLICA ═══════

    public void ForceHidePreview()
    {
        HidePreview();
        hasValidPlacement = false;
    }

    public bool HasValidPlacement() => hasValidPlacement;

    public Vector3 GetPlacementPosition() => lastValidPos;

    public Quaternion GetPlacementRotation() => lastValidRot;

    public void ResetRotation()
    {
        manualRotationOffset = 0f;
    }
}