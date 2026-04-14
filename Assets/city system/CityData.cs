// ============================================================
//  CityData.cs  — v3  FIXED + ENHANCED
//  BSP City Generator — Assets/CityGenerator/Scripts/
//
//  CAMBIOS vs v2:
//  ✔ BuildingPlacementRule → casas solo en bordes (con acceso a calle)
//  ✔ PropEntry con rotación por eje + escala aleatoria
//  ✔ EntryPoint settings por tipo de edificio (estilo GTA)
//  ✔ parkPropMargin → árboles no salen del parque
//  ✔ Campos de material de banqueta/parque preservados
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CityData_New",
                 menuName  = "CityGenerator/City Data",
                 order     = 0)]
public class CityData : ScriptableObject
{
    // ══════════════════════════════════════════════════════════════════════
    //  SEMILLA
    // ══════════════════════════════════════════════════════════════════════
    [Header("Semilla")]
    [Tooltip("0 = aleatoria. Otro número = ciudad reproducible.")]
    public int seed = 0;

    // ══════════════════════════════════════════════════════════════════════
    //  RED VIAL (BSP)
    // ══════════════════════════════════════════════════════════════════════
    [Header("Red Vial — BSP")]
    [Min(4f)]  public float mainRoadWidth      = 14f;
    [Min(2f)]  public float secondaryRoadWidth = 8f;
    [Min(1f)]  public float localRoadWidth     = 5f;
    [Range(2,8)] public int bspDepth           = 5;
    [Min(10f)] public float minBlockSize       = 30f;
    [Range(0f,0.45f)] public float splitVariance = 0.25f;
    public Material roadMaterial;

    // ══════════════════════════════════════════════════════════════════════
    //  BANQUETAS
    // ══════════════════════════════════════════════════════════════════════
    [Header("Banquetas")]
    [Min(0.5f)]  public float sidewalkWidth  = 2f;
    [Min(0.01f)] public float sidewalkHeight = 0.15f;
    public Material sidewalkMaterial;

    // ══════════════════════════════════════════════════════════════════════
    //  TIPOS DE EDIFICIO
    // ══════════════════════════════════════════════════════════════════════
    [Header("Tipos de Edificio")]
    public List<BuildingType> buildingTypes = new();

    // ══════════════════════════════════════════════════════════════════════
    //  PARQUES
    // ══════════════════════════════════════════════════════════════════════
    [Header("Parques")]
    [Range(0f,0.5f)] public float parkRatio = 0.10f;

    [Tooltip("Margen interno del parque para que los props no sobresalgan del borde. " +
             "1.5 = los props se colocan mínimo 1.5u lejos del borde del parque.")]
    [Min(0f)] public float parkPropMargin = 1.5f;

    public Material parkGroundMaterial;

    [Tooltip("Props del parque (árboles, bancas, etc.) con reglas de rotación/escala.")]
    public List<PropEntry> parkProps = new();

    // ══════════════════════════════════════════════════════════════════════
    //  CALLEJONES
    // ══════════════════════════════════════════════════════════════════════
    [Header("Callejones")]
    [Range(0f,1f)] public float alleyChance = 0.18f;
    [Min(0.5f)]    public float alleyWidth  = 2.0f;
    public List<AlleyProp> alleyProps = new();

    // ══════════════════════════════════════════════════════════════════════
    //  PROPS DE CALLE
    // ══════════════════════════════════════════════════════════════════════
    [Header("Props de Calle (banquetas)")]
    public List<StreetProp> streetProps = new();

    // ══════════════════════════════════════════════════════════════════════
    //  PUNTOS DE ENTRADA (estilo GTA)
    // ══════════════════════════════════════════════════════════════════════
    [Header("Sistema de Entradas (estilo GTA)")]
    [Tooltip("Prefab del efecto visual del punto de entrada (cilindro, partículas, etc.).")]
    public GameObject entryPointEffectPrefab;

    [Tooltip("Color del efecto de entrada.")]
    public Color entryPointColor = new Color(1f, 0.8f, 0f, 0.9f);

    // ══════════════════════════════════════════════════════════════════════
    //  RENDIMIENTO
    // ══════════════════════════════════════════════════════════════════════
    [Header("Rendimiento")]
    [Range(1,50)] public int   objectsPerFrame  = 5;
    [Min(0f)]     public float cullingDistance  = 500f;
    public bool useGPUInstancing = true;
}

// ══════════════════════════════════════════════════════════════════════════
//  REGLA DE COLOCACIÓN DE EDIFICIO
//  Define en qué zona del bloque puede colocarse un tipo de edificio.
// ══════════════════════════════════════════════════════════════════════════
public enum BuildingPlacementRule
{
    [Tooltip("Cualquier posición en el bloque. Para comercios, industria, etc.")]
    AnyPosition,

    [Tooltip("Solo bordes del bloque (filas/columnas exteriores). " +
             "OBLIGATORIO para casas/residencial: garantiza acceso a la calle.")]
    EdgeOnly,

    [Tooltip("Solo esquinas del bloque. Ideal para rascacielos.")]
    CornersOnly,

    [Tooltip("Bordes y esquinas. Para apartamentos y edificios medianos.")]
    EdgeAndCorner
}

// ══════════════════════════════════════════════════════════════════════════
//  TIPO DE EDIFICIO
// ══════════════════════════════════════════════════════════════════════════
[Serializable]
public class BuildingType
{
    [Tooltip("Nombre descriptivo para el Inspector.")]
    public string name = "Residencial";

    [Tooltip("Color en gizmos para identificar este tipo de zona.")]
    public Color gizmoColor = new Color(0.3f,0.7f,0.4f,0.6f);

    // ── Pisos ──────────────────────────────────────────────────────────
    [Header("Pisos")]
    [Min(1)] public int minFloors = 1;
    [Min(1)] public int maxFloors = 4;
    public List<GameObject> floorPrefabs = new();
    public List<GameObject> roofPrefabs  = new();

    // ── REGLA DE COLOCACIÓN (FIX PRINCIPAL) ───────────────────────────
    [Header("Regla de Colocación")]
    [Tooltip("¿Dónde puede colocarse este edificio dentro de un bloque?\n\n" +
             "EdgeOnly = OBLIGATORIO para casas/residencial.\n" +
             "AnyPosition = para comercios, industria, almacenes.\n" +
             "CornersOnly = para rascacielos.\n" +
             "EdgeAndCorner = para apartamentos.")]
    public BuildingPlacementRule placementRule = BuildingPlacementRule.EdgeOnly;

    // ── Distribución en el lote ────────────────────────────────────────
    [Header("Distribución en Lote")]
    [Min(0f)] public float setbackMin = 1f;
    [Min(0f)] public float setbackMax = 3f;

    // ── PUNTO DE ENTRADA (estilo GTA) ─────────────────────────────────
    [Header("Punto de Entrada")]
    [Tooltip("¿Este tipo de edificio puede tener una entrada interactiva?")]
    public bool canHaveEntryPoint = false;

    [Range(0f,1f)]
    [Tooltip("Probabilidad de que UN edificio de este tipo tenga entrada activa. " +
             "0.3 = 30% tendrán entrada.")]
    public float entryPointChance = 0.25f;

    [Tooltip("Posición local del punto de entrada relativa al centro del edificio.")]
    public Vector3 entryPointOffset = new Vector3(0f, 0f, -5f);

    [Tooltip("Texto del prompt al acercarse. Ej: 'Presiona [E] para entrar'")]
    public string entryPromptText = "Presiona [E] para entrar";

    // ── Peso ───────────────────────────────────────────────────────────
    [Header("Peso")]
    [Min(0f)] public float weight = 1f;

    // ── API ────────────────────────────────────────────────────────────
    public int GetRandomFloorCount() =>
        UnityEngine.Random.Range(minFloors, maxFloors + 1);

    public GameObject GetRandomFloorPrefab()
    {
        var valid = new List<GameObject>();
        foreach (var pf in floorPrefabs)
            if (pf != null && pf.GetComponent<BuildingFloor>() != null) valid.Add(pf);
        if (valid.Count == 0) return null;
        return valid[UnityEngine.Random.Range(0, valid.Count)];
    }

    public GameObject GetRandomRoofPrefab()
    {
        var valid = new List<GameObject>();
        foreach (var pf in roofPrefabs) if (pf != null) valid.Add(pf);
        if (valid.Count == 0) return null;
        return valid[UnityEngine.Random.Range(0, valid.Count)];
    }

    private void OnValidate() { if (maxFloors < minFloors) maxFloors = minFloors; }
}

// ══════════════════════════════════════════════════════════════════════════
//  PROP DE PARQUE  (con rotación y escala aleatoria por eje)
// ══════════════════════════════════════════════════════════════════════════
[Serializable]
public class PropEntry
{
    [Tooltip("Prefab del prop (árbol, banca, farola, etc.).")]
    public GameObject prefab;

    [Tooltip("Densidad: cuántos props se intentan por m² de parque.")]
    [Range(0f, 0.02f)] public float density = 0.004f;

    // ── ROTACIÓN ALEATORIA POR EJE ─────────────────────────────────────
    [Header("Rotación Aleatoria")]

    [Tooltip("Si true, el prop rota aleatoriamente en Y (horizontal). " +
             "Actívalo para árboles, bancas, decoraciones.")]
    public bool randomRotY = true;

    [Tooltip("Modo de rotación en Y:\n" +
             "FreeRange = cualquier ángulo entre los valores min/max.\n" +
             "Stepped = múltiplos del paso (ej: 0/90/180/270 con paso 90).")]
    public PropRotMode rotYMode = PropRotMode.FreeRange;

    [Tooltip("Paso de rotación en grados si el modo es Stepped. Ej: 90 = cardinal.")]
    [Min(1f)] public float rotYStep = 90f;

    [Tooltip("Rango de rotación Y si el modo es FreeRange. (-180,180) = libre.")]
    public Vector2 rotYRange = new Vector2(-180f, 180f);

    [Tooltip("Si true, inclina el prop en X (adelante/atrás). " +
             "Útil para plantas o rocas con inclinación sutil.")]
    public bool randomRotX = false;
    public Vector2 rotXRange = new Vector2(-8f, 8f);

    [Tooltip("Si true, inclina el prop en Z (izquierda/derecha).")]
    public bool randomRotZ = false;
    public Vector2 rotZRange = new Vector2(-8f, 8f);

    // ── ESCALA ALEATORIA ──────────────────────────────────────────────
    [Header("Escala Aleatoria")]
    [Tooltip("Si true, el prop tendrá una escala uniforme aleatoria para dar variedad visual.")]
    public bool randomScale = false;

    [Tooltip("Rango de escala uniforme. (0.8, 1.2) = variación del ±20%.")]
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);

    // ── API ────────────────────────────────────────────────────────────

    /// <summary> Calcula la rotación completa según las reglas de este prop. </summary>
    public Quaternion GetRandomRotation()
    {
        float rx = 0f, ry = 0f, rz = 0f;

        if (randomRotY)
        {
            ry = rotYMode == PropRotMode.Stepped
                ? UnityEngine.Random.Range(0, Mathf.RoundToInt(360f / rotYStep)) * rotYStep
                : UnityEngine.Random.Range(rotYRange.x, rotYRange.y);
        }

        if (randomRotX) rx = UnityEngine.Random.Range(rotXRange.x, rotXRange.y);
        if (randomRotZ) rz = UnityEngine.Random.Range(rotZRange.x, rotZRange.y);

        return Quaternion.Euler(rx, ry, rz);
    }

    /// <summary> Devuelve la escala calculada. </summary>
    public Vector3 GetRandomScale()
    {
        if (!randomScale) return Vector3.one;
        float s = UnityEngine.Random.Range(scaleRange.x, scaleRange.y);
        return new Vector3(s, s, s);
    }
}

// ══════════════════════════════════════════════════════════════════════════
//  MODOS DE ROTACIÓN DE PROP
// ══════════════════════════════════════════════════════════════════════════
public enum PropRotMode
{
    FreeRange,  // Cualquier ángulo dentro del rango
    Stepped     // Múltiplos fijos (0°/90°/180°/270°)
}

// ══════════════════════════════════════════════════════════════════════════
//  PROP DE CALLE
// ══════════════════════════════════════════════════════════════════════════
[Serializable]
public class StreetProp
{
    public string     name          = "Poste";
    public GameObject prefab;
    [Min(1f)]       public float spacing       = 20f;
    public float    lateralOffset = 0.8f;
    [Range(0f,1f)]  public float chance        = 0.8f;
    public bool     mainRoadOnly  = false;

    // Rotación alineada a la dirección de la calle (automática)
    [Tooltip("Si true, el prop rota para alinearse con la dirección de la calle.")]
    public bool alignToRoad = true;

    // Rotación adicional aleatoria en Y (ruido visual)
    [Range(0f,180f)]
    [Tooltip("Variación aleatoria de rotación Y sobre la alineación base.")]
    public float rotationNoise = 0f;
}

// ══════════════════════════════════════════════════════════════════════════
//  PROP DE CALLEJÓN
// ══════════════════════════════════════════════════════════════════════════
[Serializable]
public class AlleyProp
{
    public string     name       = "Basura";
    public GameObject prefab;
    [Range(0f,1f)] public float chance      = 0.5f;
    [Min(1)]       public int   maxPerAlley = 3;
}