// ============================================================
//  GridCityData.cs  — Sistema Grid (INDEPENDIENTE)
//  Assets/GridCityGenerator/Scripts/
//
//  ScriptableObject maestro del sistema de ciudad en GRID.
//  100% independiente del sistema BSP. No comparte ninguna clase.
//  Crear: Click derecho → Create → GridCity → City Data
// ============================================================

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GridCityData_New",
                 menuName  = "GridCity/City Data",
                 order     = 0)]
public class GridCityData : ScriptableObject
{
    // ══════════════════════════════════════════════════════════════════════
    //  LÍMITES
    // ══════════════════════════════════════════════════════════════════════
    [Header("Límites de la Ciudad")]
    [Min(20f)] public float cityWidth  = 200f;
    [Min(20f)] public float cityDepth  = 200f;
    public Vector3 cityCenter = Vector3.zero;

    // ══════════════════════════════════════════════════════════════════════
    //  CALLES Y BANQUETAS
    // ══════════════════════════════════════════════════════════════════════
    [Header("Calles y Banquetas")]
    [Min(2f)]  public float roadWidth     = 8f;
    [Min(0f)]  public float sidewalkWidth = 2f;

    [Tooltip("Tamaño de cada bloque edificable entre calles.")]
    [Min(10f)] public float blockSize     = 40f;

    // ══════════════════════════════════════════════════════════════════════
    //  MATERIALES
    // ══════════════════════════════════════════════════════════════════════
    [Header("Materiales")]
    public Material roadMaterial;
    public Material sidewalkMaterial;
    public Material parkGroundMaterial;

    // ══════════════════════════════════════════════════════════════════════
    //  TIPOS DE EDIFICIO
    // ══════════════════════════════════════════════════════════════════════
    [Header("Tipos de Edificio")]
    public List<GridWeightedBuilding> buildingTypes = new();

    // ══════════════════════════════════════════════════════════════════════
    //  PARQUES
    // ══════════════════════════════════════════════════════════════════════
    [Header("Parques")]
    [Range(0f,0.5f)] public float parkDensity   = 0.10f;

    [Tooltip("Margen interno del parque (unidades). Props no se colocan más cerca que esto del borde.")]
    [Min(0f)]        public float parkPropMargin = 1.5f;

    public List<GridWeightedProp> parkProps = new();

    [Min(0)] public int propsPerPark = 12;

    // ══════════════════════════════════════════════════════════════════════
    //  PROPS DE BANQUETA
    // ══════════════════════════════════════════════════════════════════════
    [Header("Props de Banqueta")]
    public List<GridWeightedProp> sidewalkProps = new();
    [Min(1f)] public float sidewalkPropSpacing = 12f;

    // ══════════════════════════════════════════════════════════════════════
    //  PUNTOS DE ENTRADA (estilo GTA)
    // ══════════════════════════════════════════════════════════════════════
    [Header("Entradas de Edificios")]
    public GameObject entryPointPrefab;
    public Color      entryPointColor = new Color(1f, 0.8f, 0f, 0.9f);

    // ══════════════════════════════════════════════════════════════════════
    //  SEMILLA Y POOLING
    // ══════════════════════════════════════════════════════════════════════
    [Header("Semilla")]
    public int  seed          = 0;
    public bool useRandomSeed = false;

    [Header("Rendimiento")]
    [Min(1)] public int poolInitialSize = 5;

    // ══════════════════════════════════════════════════════════════════════
    //  PROPIEDADES CALCULADAS
    // ══════════════════════════════════════════════════════════════════════

    /// <summary> Anchura total de un slot (bloque + calle + 2 banquetas). </summary>
    public float SlotWidth => blockSize + roadWidth + sidewalkWidth * 2f;

    /// <summary> Rectángulo XZ de los límites de la ciudad. </summary>
    public Rect CityBounds => new Rect(
        cityCenter.x - cityWidth  * 0.5f,
        cityCenter.z - cityDepth  * 0.5f,
        cityWidth, cityDepth
    );

    // ══════════════════════════════════════════════════════════════════════
    //  API — SELECCIÓN PONDERADA
    // ══════════════════════════════════════════════════════════════════════

    public GridBuildingTypeData GetRandomBuildingType() =>
        PickWeighted(buildingTypes, b => b.weight)?.buildingType;

    public GridPropData GetRandomParkProp() =>
        PickWeighted(parkProps, p => p.weight)?.propData;

    public GridPropData GetRandomSidewalkProp() =>
        PickWeighted(sidewalkProps, p => p.weight)?.propData;

    // Helper genérico de selección ponderada
    private T PickWeighted<T>(List<T> list, System.Func<T, float> getWeight)
    {
        if (list == null || list.Count == 0) return default;
        float total = 0f;
        foreach (var item in list) total += getWeight(item);
        float roll = UnityEngine.Random.Range(0f, total), acc = 0f;
        foreach (var item in list)
        { acc += getWeight(item); if (roll <= acc) return item; }
        return list[list.Count - 1];
    }
}

// ══════════════════════════════════════════════════════════════════════════
//  TIPO DE EDIFICIO (Grid)
// ══════════════════════════════════════════════════════════════════════════
[System.Serializable]
public class GridBuildingTypeData
{
    [Tooltip("Nombre del tipo (solo informativo).")]
    public string typeName = "Residencial";

    [Tooltip("Color en gizmos para este tipo.")]
    public Color gizmoColor = new Color(0.4f, 0.7f, 0.4f, 0.6f);

    [Header("Prefabs")]
    [Tooltip("Lista de prefabs. Se elige uno al azar por edificio.")]
    public GameObject[] prefabs;

    [Header("Tamaño en Planta")]
    public Vector2 widthRange = new Vector2(5f, 12f);
    public Vector2 depthRange = new Vector2(5f, 12f);

    // ── REGLA DE COLOCACIÓN ────────────────────────────────────────────
    [Header("Regla de Colocación")]
    [Tooltip("EdgeOnly = solo bordes → usa para casas/residencial.\n" +
             "AnyPosition = cualquier celda → comercios, industria.\n" +
             "CornersOnly = solo esquinas → rascacielos.\n" +
             "EdgeAndCorner = bordes y esquinas → apartamentos.")]
    public GridPlacementRule placementRule = GridPlacementRule.EdgeOnly;

    [Header("Cantidad y Spacing")]
    [Min(0)]  public int   maxPerBlock            = 2;
    [Min(0f)] public float minSpacingBetweenSame  = 0.5f;

    // ── RETRANQUEO (setback) ───────────────────────────────────────────
    [Header("Retranqueo (distancia al borde)")]
    [Min(0f)] public float setbackMin = 0f;
    [Min(0f)] public float setbackMax = 1f;

    // ── ROTACIÓN ALEATORIA ─────────────────────────────────────────────
    [Header("Rotación")]
    public bool  randomRotY    = true;
    [Min(1f)] public float rotYStep = 90f;
    public bool  randomRotX    = false;
    public Vector2 rotXRange   = new Vector2(-5f, 5f);
    public bool  randomRotZ    = false;
    public Vector2 rotZRange   = new Vector2(-5f, 5f);

    // ── PUNTO DE ENTRADA ──────────────────────────────────────────────
    [Header("Entrada al Edificio")]
    public bool    canHaveEntryPoint = false;
    [Range(0f,1f)] public float entryChance = 0.3f;
    public Vector3 entryLocalOffset = new Vector3(0f, 0f, -4f);
    public string  entryPromptText  = "Presiona [E] para entrar";

    // ── API ────────────────────────────────────────────────────────────
    public GameObject GetRandomPrefab()
    {
        if (prefabs == null || prefabs.Length == 0) return null;
        return prefabs[UnityEngine.Random.Range(0, prefabs.Length)];
    }

    public Quaternion GetRandomRotation()
    {
        float ry = 0f, rx = 0f, rz = 0f;
        if (randomRotY)
        {
            int steps = Mathf.RoundToInt(360f / Mathf.Max(1f, rotYStep));
            ry = UnityEngine.Random.Range(0, steps) * rotYStep;
        }
        if (randomRotX) rx = UnityEngine.Random.Range(rotXRange.x, rotXRange.y);
        if (randomRotZ) rz = UnityEngine.Random.Range(rotZRange.x, rotZRange.y);
        return Quaternion.Euler(rx, ry, rz);
    }

    public Vector2 GetRandomFootprint() => new Vector2(
        UnityEngine.Random.Range(widthRange.x, widthRange.y),
        UnityEngine.Random.Range(depthRange.x, depthRange.y));
}

// ══════════════════════════════════════════════════════════════════════════
//  PROP DATA (Grid)
// ══════════════════════════════════════════════════════════════════════════
[System.Serializable]
public class GridPropData
{
    public string     propName = "Prop";
    public GameObject[] prefabs;

    // Rotación por eje
    public bool    randomRotY    = true;
    public GridRotMode rotYMode  = GridRotMode.FreeRange;
    [Min(1f)] public float rotYStep = 1f;
    public Vector2 rotYRange     = new Vector2(-180f, 180f);
    public bool    randomRotX    = false;
    public Vector2 rotXRange     = new Vector2(-8f, 8f);
    public bool    randomRotZ    = false;
    public Vector2 rotZRange     = new Vector2(-8f, 8f);

    // Escala
    public bool    randomScale   = false;
    public Vector2 scaleRange    = new Vector2(0.8f, 1.2f);

    // Spacing
    [Min(0f)] public float minSpacingFromSelf   = 2f;
    [Min(0f)] public float minSpacingFromOthers = 0.5f;
    [Min(0f)] public float borderMargin         = 1f;

    public GameObject GetRandomPrefab()
    {
        if (prefabs == null || prefabs.Length == 0) return null;
        return prefabs[UnityEngine.Random.Range(0, prefabs.Length)];
    }

    public Quaternion GetRandomRotation()
    {
        float ry = CalcAxis(randomRotY, rotYMode, rotYStep, rotYRange);
        float rx = randomRotX ? UnityEngine.Random.Range(rotXRange.x, rotXRange.y) : 0f;
        float rz = randomRotZ ? UnityEngine.Random.Range(rotZRange.x, rotZRange.y) : 0f;
        return Quaternion.Euler(rx, ry, rz);
    }

    public Vector3 GetRandomScale()
    {
        if (!randomScale) return Vector3.one;
        float s = UnityEngine.Random.Range(scaleRange.x, scaleRange.y);
        return new Vector3(s, s, s);
    }

    private float CalcAxis(bool enabled, GridRotMode mode, float step, Vector2 range)
    {
        if (!enabled) return 0f;
        return mode == GridRotMode.Stepped
            ? UnityEngine.Random.Range(0, Mathf.RoundToInt(360f / Mathf.Max(1f, step))) * step
            : UnityEngine.Random.Range(range.x, range.y);
    }
}

// ══════════════════════════════════════════════════════════════════════════
//  ENUMS (Grid — prefijados para evitar colisiones con el sistema BSP)
// ══════════════════════════════════════════════════════════════════════════
public enum GridPlacementRule
{
    AnyPosition, EdgeOnly, CornersOnly, EdgeAndCorner
}

public enum GridRotMode
{
    FreeRange, Stepped
}

// ══════════════════════════════════════════════════════════════════════════
//  WRAPPERS PONDERADOS
// ══════════════════════════════════════════════════════════════════════════
[System.Serializable]
public class GridWeightedBuilding
{
    public GridBuildingTypeData buildingType;
    [Min(0.01f)] public float weight = 1f;
}

[System.Serializable]
public class GridWeightedProp
{
    public GridPropData propData;
    [Min(0.01f)] public float weight = 1f;
}