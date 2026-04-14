// ============================================================
//  GridCityGeneratorEditor.cs  — Sistema Grid (INDEPENDIENTE)
//  Assets/GridCityGenerator/Editor/
//
//  Custom editor con:
//  - Botones de generar/limpiar
//  - Handles para redimensionar los límites en la SceneView
//  - Vista previa de proporciones
//  - Validación de configuración
// ============================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// ══════════════════════════════════════════════════════════════════════════
//  EDITOR PARA GridCityGenerator
// ══════════════════════════════════════════════════════════════════════════
[CustomEditor(typeof(GridCityGenerator))]
public class GridCityGeneratorInspector : Editor
{
    private bool _showStats = true;

    public override void OnInspectorGUI()
    {
        GridCityGenerator gen = (GridCityGenerator)target;
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Acciones", EditorStyles.boldLabel);

        // ── Botones ───────────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.3f, 0.9f, 0.4f);
        if (GUILayout.Button("▶ Generar", GUILayout.Height(32)))
        {
            if (Application.isPlaying)
                gen.GenerateCity();
            else
                Debug.LogWarning("[GridCity] Entra en Play Mode para generar.");
        }

        GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
        if (GUILayout.Button("✕ Limpiar", GUILayout.Height(32)))
        {
            if (Application.isPlaying) gen.ClearCity();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        // ── Estadísticas ──────────────────────────────────────────────
        EditorGUILayout.Space(4);
        _showStats = EditorGUILayout.Foldout(_showStats, "Estadísticas");
        if (_showStats)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField($"Edificios: {gen.BuildingCount}");
                EditorGUILayout.LabelField($"Tiempo: {gen.LastGenTime*1000f:F1} ms");
                EditorGUILayout.LabelField($"Bloques: {gen.blockInfos?.Count ?? 0}");

                if (gen.cityData != null)
                {
                    EditorGUILayout.LabelField(
                        $"Ciudad: {gen.cityData.cityWidth:F0}×{gen.cityData.cityDepth:F0}u");
                    EditorGUILayout.LabelField(
                        $"Slot: {gen.cityData.SlotWidth:F1}u  " +
                        $"Bloques X: {Mathf.FloorToInt(gen.cityData.cityWidth / gen.cityData.SlotWidth)}  " +
                        $"Z: {Mathf.FloorToInt(gen.cityData.cityDepth / gen.cityData.SlotWidth)}");
                }
            }
        }

        // Validación rápida
        EditorGUILayout.Space(4);
        if (GUILayout.Button("✓ Validar Configuración"))
            ValidateGrid(gen);
    }

    private void OnSceneGUI()
    {
        GridCityGenerator gen = (GridCityGenerator)target;
        if (gen.cityData == null) return;

        GridCityData data = gen.cityData;

        // ── Handles para redimensionar los límites ────────────────────
        EditorGUI.BeginChangeCheck();
        Handles.color = Color.yellow;

        Vector3 c = data.cityCenter;
        float hw = data.cityWidth  * 0.5f;
        float hd = data.cityDepth  * 0.5f;

        // Handle Norte
        Vector3 hN  = c + new Vector3(0, 0,  hd);
        Vector3 nhN = Handles.Slider(hN, Vector3.forward,
                                     HandleUtility.GetHandleSize(hN) * 0.5f,
                                     Handles.ArrowHandleCap, 1f);

        // Handle Sur
        Vector3 hS  = c + new Vector3(0, 0, -hd);
        Vector3 nhS = Handles.Slider(hS, Vector3.back,
                                     HandleUtility.GetHandleSize(hS) * 0.5f,
                                     Handles.ArrowHandleCap, 1f);

        // Handle Este
        Vector3 hE  = c + new Vector3( hw, 0, 0);
        Vector3 nhE = Handles.Slider(hE, Vector3.right,
                                     HandleUtility.GetHandleSize(hE) * 0.5f,
                                     Handles.ArrowHandleCap, 1f);

        // Handle Oeste
        Vector3 hW  = c + new Vector3(-hw, 0, 0);
        Vector3 nhW = Handles.Slider(hW, Vector3.left,
                                     HandleUtility.GetHandleSize(hW) * 0.5f,
                                     Handles.ArrowHandleCap, 1f);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(data, "Resize Grid City");
            data.cityDepth = Mathf.Max(20f, nhN.z - nhS.z);
            data.cityWidth = Mathf.Max(20f, nhE.x - nhW.x);
            EditorUtility.SetDirty(data);
        }

        // Etiqueta de dimensiones
        Handles.Label(c + new Vector3(0, 3f, 0),
                      $"Grid: {data.cityWidth:F0} × {data.cityDepth:F0}u\n" +
                      $"Slot: {data.SlotWidth:F1}u",
                      EditorStyles.boldLabel);
    }

    private void ValidateGrid(GridCityGenerator gen)
    {
        if (gen.cityData == null)
        {
            EditorUtility.DisplayDialog("Error", "No hay GridCityData asignado.", "OK");
            return;
        }

        var data  = gen.cityData;
        bool ok   = true;
        string msg = "";

        if (data.buildingTypes.Count == 0)
        { msg += "⚠ No hay tipos de edificio.\n"; ok = false; }

        foreach (var wb in data.buildingTypes)
        {
            if (wb.buildingType == null)
            { msg += "⚠ Un tipo de edificio es null.\n"; ok = false; }
            else if (wb.buildingType.prefabs == null || wb.buildingType.prefabs.Length == 0)
            { msg += $"⚠ '{wb.buildingType.typeName}' no tiene prefabs.\n"; ok = false; }
        }

        float slot = data.SlotWidth;
        if (data.cityWidth < slot * 2f || data.cityDepth < slot * 2f)
        { msg += "⚠ La ciudad es demasiado pequeña para el tamaño de slot.\n"; ok = false; }

        EditorUtility.DisplayDialog(
            ok ? "Configuración OK ✔" : "Problemas encontrados",
            ok ? "Todo parece correcto." : msg,
            "OK");
    }
}

// ══════════════════════════════════════════════════════════════════════════
//  EDITOR PARA GridCityData  — Preview de proporciones
// ══════════════════════════════════════════════════════════════════════════
[CustomEditor(typeof(GridCityData))]
public class GridCityDataInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GridCityData data = (GridCityData)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Preview de Proporciones", EditorStyles.boldLabel);

        float totalW = data.roadWidth + data.sidewalkWidth * 2f + data.blockSize;
        if (totalW <= 0f)
            totalW = 1f;
        float bar    = Mathf.Min(EditorGUIUtility.currentViewWidth - 30f, 350f);

        // Calzada | Banqueta | Bloque | Banqueta
        float rRatio  = data.roadWidth     / totalW;
        float swRatio = data.sidewalkWidth / totalW;
        float bRatio  = data.blockSize     / totalW;

        EditorGUILayout.BeginHorizontal();
        DrawBar(bar * rRatio,  20f, new Color(0.2f,0.2f,0.2f), "Calle");
        DrawBar(bar * swRatio, 20f, new Color(0.6f,0.6f,0.6f), "SW");
        DrawBar(bar * bRatio,  20f, new Color(0.3f,0.5f,0.9f), "Bloque");
        DrawBar(bar * swRatio, 20f, new Color(0.6f,0.6f,0.6f), "SW");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField(
            $"Slot total: {totalW:F1}u  |  " +
            $"Calle: {data.roadWidth}u  |  Banqueta: {data.sidewalkWidth}u  |  " +
            $"Bloque: {data.blockSize}u",
            EditorStyles.miniLabel);
    }

    private void DrawBar(float w, float h, Color color, string label)
    {
        Rect r = GUILayoutUtility.GetRect(w, h);
        EditorGUI.DrawRect(r, color);
        if (r.width > 25f)
            GUI.Label(r, label, new GUIStyle(EditorStyles.miniLabel)
            { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } });
    }
}

// ══════════════════════════════════════════════════════════════════════════
//  MEJORAS AL EDITOR BSP EXISTENTE (CityGenerator)
//  Añade validación de placementRule en el Inspector del CityData
// ══════════════════════════════════════════════════════════════════════════
[CustomEditor(typeof(CityData))]
public class CityDataInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CityData data = (CityData)target;

        EditorGUILayout.Space(8);

        // ── Resumen de tipos de edificio y sus reglas ─────────────────
        if (data.buildingTypes != null && data.buildingTypes.Count > 0)
        {
            EditorGUILayout.LabelField("Resumen de tipos de edificio:", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var bt in data.buildingTypes)
                {
                    if (bt == null) continue;

                    string ruleIcon = bt.placementRule switch
                    {
                        BuildingPlacementRule.EdgeOnly      => "🏠 BORDE",
                        BuildingPlacementRule.CornersOnly   => "🏢 ESQUINA",
                        BuildingPlacementRule.EdgeAndCorner => "🏬 BORDE+ESQ.",
                        BuildingPlacementRule.AnyPosition   => "🏭 CUALQUIER",
                        _                                   => "?"
                    };

                    Color prev = GUI.color;
                    GUI.color = bt.gizmoColor;
                    EditorGUILayout.LabelField($"■ {bt.name}", $"{ruleIcon}  (w:{bt.weight})");
                    GUI.color = prev;
                }
            }

            EditorGUILayout.Space(4);

            // Advertencia si hay residencial con AnyPosition
            bool badConfig = false;
            foreach (var bt in data.buildingTypes)
            {
                if (bt != null &&
                    bt.name.ToLower().Contains("resid") &&
                    bt.placementRule == BuildingPlacementRule.AnyPosition)
                {
                    badConfig = true;
                    break;
                }
            }

            if (badConfig)
                EditorGUILayout.HelpBox(
                    "⚠ Un tipo con 'resid' en el nombre usa AnyPosition.\n" +
                    "Considera cambiar a EdgeOnly para que las casas tengan acceso a la calle.",
                    MessageType.Warning);
        }

        EditorGUILayout.Space(4);
        if (GUILayout.Button("✓ Validar Configuración"))
            ValidateBSP(data);
    }

    private void ValidateBSP(CityData data)
    {
        bool ok  = true;
        string m = "";

        if (data.buildingTypes == null || data.buildingTypes.Count == 0)
        { m += "⚠ No hay tipos de edificio.\n"; ok = false; }

        foreach (var bt in data.buildingTypes ?? new System.Collections.Generic.List<BuildingType>())
        {
            if (bt == null) { m += "⚠ Un BuildingType es null.\n"; ok = false; continue; }
            if (bt.floorPrefabs.Count == 0)
            { m += $"⚠ '{bt.name}' no tiene floor prefabs (se usará fallback de cubos).\n"; }
        }

        if (data.minBlockSize <= data.sidewalkWidth * 4f)
        { m += "⚠ blockSize muy pequeño comparado con las banquetas.\n"; ok = false; }

        EditorUtility.DisplayDialog(
            ok ? "BSP City OK ✔" : "Atención",
            string.IsNullOrEmpty(m) ? "Configuración válida." : m,
            "OK");
    }
}
#endif