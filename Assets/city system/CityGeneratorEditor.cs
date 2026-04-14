// ============================================================
//  CityGeneratorEditor.cs  — v2 mejorado
//  Assets/CityGenerator/Editor/CityGeneratorEditor.cs
//  Abrir: Menú → CityGenerator → Open Editor
// ============================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CityGeneratorEditor : EditorWindow
{
    // ── Refs ──────────────────────────────────────────────────────────────
    private CityGenerator     _gen;
    private CityData          _data;
    private CityPresetLibrary _lib;
    private SerializedObject  _genSO, _dataSO;

    // ── UI state ──────────────────────────────────────────────────────────
    private Vector2 _scroll;
    private bool _editPoly   = false;
    private int  _selVert    = -1;

    // Secciones colapsables
    private bool _secMain    = true;
    private bool _secSize    = true;
    private bool _secData    = false;
    private bool _secPoly    = false;
    private bool _secPresets = false;
    private bool _secStats   = true;
    private bool _secPreview = false;

    // Control de tamaño directo
    private float _sizeW = 200f, _sizeD = 200f;
    private bool  _sizeSymmetric = true;

    // Preset UI
    private string _presetName  = "Preset";
    private string _presetNotes = "";
    private int    _presetRating = 3;

    // Colores de la ventana
    private static readonly Color C_HEADER  = new Color(0.10f,0.12f,0.14f);
    private static readonly Color C_SEC     = new Color(0.12f,0.14f,0.17f);
    private static readonly Color C_GEN     = new Color(0.20f,0.65f,0.25f);
    private static readonly Color C_CLEAR   = new Color(0.70f,0.20f,0.18f);
    private static readonly Color C_YELLOW  = new Color(0.95f,0.80f,0.15f);
    private static readonly Color C_BLUE    = new Color(0.25f,0.55f,0.85f);

    // ══════════════════════════════════════════════════════════════════════
    [MenuItem("CityGenerator/Open Editor  _%#G")]
    public static void Open() => GetWindow<CityGeneratorEditor>("🏙 City Generator");

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        FindGenerator();
        titleContent = new GUIContent("🏙 City Generator");
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        _editPoly = false;
    }

    private void FindGenerator()
    {
        _gen = FindFirstObjectByType<CityGenerator>();
        RefreshSOs();
    }

    private void RefreshSOs()
    {
        if (_gen  != null) _genSO  = new SerializedObject(_gen);
        _data = _gen?.data;
        if (_data != null) _dataSO = new SerializedObject(_data);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  WINDOW GUI
    // ══════════════════════════════════════════════════════════════════════
    private void OnGUI()
    {
        DrawWindowHeader();

        // Selector de componente
        EditorGUI.BeginChangeCheck();
        _gen = (CityGenerator)EditorGUILayout.ObjectField("City Generator", _gen, typeof(CityGenerator), true);
        if (EditorGUI.EndChangeCheck()) RefreshSOs();

        if (_gen == null)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox("No hay CityGenerator en la escena.", MessageType.Info);
            if (BtnPrimary("Crear CityGenerator")) CreateGen();
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawSectionMain();
        DrawSectionSize();
        DrawSectionData();
        DrawSectionPolygon();
        DrawSectionStats();
        DrawSectionPresets();

        EditorGUILayout.Space(20);
        EditorGUILayout.EndScrollView();
    }

    // ──────────────────────────────────────────────────────────────────────
    private void DrawWindowHeader()
    {
        var r = EditorGUILayout.GetControlRect(false, 38f);
        EditorGUI.DrawRect(r, C_HEADER);
        GUI.Label(r, "    🏙  CITY GENERATOR",
            new GUIStyle(EditorStyles.boldLabel) {
                fontSize=16, alignment=TextAnchor.MiddleLeft,
                normal={ textColor = new Color(0.75f,0.90f,1f) }});
        EditorGUILayout.Space(2);
    }

    // ── Controles principales ─────────────────────────────────────────────
    private void DrawSectionMain()
    {
        if (!Foldout(ref _secMain, "▶  GENERACIÓN", C_GEN)) return;

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = C_GEN;
        if (GUILayout.Button("▶  GENERAR", GUILayout.Height(38)))
        {
            if (!_gen.IsGenerating) { Undo.RecordObject(_gen,"Gen"); _gen.Generate(); }
        }
        GUI.backgroundColor = C_CLEAR;
        if (GUILayout.Button("✕  LIMPIAR", GUILayout.Height(38)))
        {
            if (EditorUtility.DisplayDialog("Limpiar","¿Eliminar todo lo generado?","Sí","No"))
            { Undo.RecordObject(_gen,"Clear"); _gen.Clear(); }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (BtnSecondary("🎲 Nueva semilla + generar"))
        {
            if (_gen.data != null)
            {
                Undo.RecordObject(_gen.data,"Seed");
                _gen.data.seed = Random.Range(1,99999);
                _gen.Clear(); _gen.Generate();
            }
        }
        if (BtnSecondary("📷 Foco", 80))
            Selection.activeGameObject = _gen.gameObject;
        EditorGUILayout.EndHorizontal();

        if (_gen.IsGenerating)
        {
            EditorGUILayout.HelpBox("⏳ Generando… (generación lenta activa)", MessageType.Info);
            Repaint();
        }
    }

    // ── Tamaño del área ───────────────────────────────────────────────────
    private void DrawSectionSize()
    {
        if (!Foldout(ref _secSize, "📐  TAMAÑO DEL ÁREA", C_BLUE)) return;

        EditorGUILayout.HelpBox(
            "Escribe el tamaño deseado y aplica. " +
            "Esto sobrescribe el polígono con un rectángulo centrado en el origen.",
            MessageType.None);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Ancho (m)", GUILayout.Width(80));
        _sizeW = EditorGUILayout.FloatField(_sizeW, GUILayout.Width(70));
        EditorGUILayout.LabelField("Profundidad (m)", GUILayout.Width(100));
        _sizeD = EditorGUILayout.FloatField(_sizeD, GUILayout.Width(70));
        EditorGUILayout.EndHorizontal();

        _sizeSymmetric = EditorGUILayout.Toggle("Cuadrado (W=D)", _sizeSymmetric);
        if (_sizeSymmetric && _sizeD != _sizeW) _sizeD = _sizeW;

        EditorGUILayout.BeginHorizontal();
        if (BtnPrimary("Aplicar tamaño"))
        {
            Undo.RecordObject(_gen,"SetSize");
            float hw = _sizeW*0.5f, hd = _sizeD*0.5f;
            _gen.areaPolygon = new List<Vector2>
            { new(-hw,-hd), new(hw,-hd), new(hw,hd), new(-hw,hd) };
            SceneView.RepaintAll();
        }

        if (BtnSecondary("200×200"))  ApplySize(200,200);
        if (BtnSecondary("400×400"))  ApplySize(400,400);
        if (BtnSecondary("600×400"))  ApplySize(600,400);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (BtnSecondary("Forma orgánica")) ApplyOrganicShape();
        if (BtnSecondary("Forma en L"))     ApplyLShape();
        EditorGUILayout.EndHorizontal();
    }

    // ── CityData ──────────────────────────────────────────────────────────
    private void DrawSectionData()
    {
        if (!Foldout(ref _secData, "⚙  CITY DATA")) return;

        EditorGUI.BeginChangeCheck();
        _data = (CityData)EditorGUILayout.ObjectField("City Data", _data, typeof(CityData), false);
        if (EditorGUI.EndChangeCheck())
        {
            if (_gen != null) { Undo.RecordObject(_gen,"SetData"); _gen.data = _data; }
            if (_data != null) _dataSO = new SerializedObject(_data);
        }

        if (_data == null)
        {
            if (BtnPrimary("Crear nuevo CityData")) CreateCityData();
            return;
        }

        if (_dataSO == null) _dataSO = new SerializedObject(_data);
        _dataSO.Update();

        EditorGUI.indentLevel++;

        Field(_dataSO, "seed",               "Semilla");
        Field(_dataSO, "bspDepth",           "Profundidad BSP (detalle)");
        Field(_dataSO, "minBlockSize",        "Tamaño mínimo de bloque");
        Field(_dataSO, "splitVariance",       "Varianza de corte (0=regular)");
        GUILayout.Space(4);
        Field(_dataSO, "mainRoadWidth",       "Calle principal (m)");
        Field(_dataSO, "secondaryRoadWidth",  "Calle secundaria (m)");
        Field(_dataSO, "localRoadWidth",      "Calle local (m)");
        GUILayout.Space(4);
        Field(_dataSO, "sidewalkWidth",       "Ancho banqueta (m)");
        Field(_dataSO, "sidewalkHeight",      "Alto banqueta (m)");
        GUILayout.Space(4);
        Field(_dataSO, "parkRatio",           "Ratio de parques");
        Field(_dataSO, "alleyChance",         "Probabilidad de callejón");
        Field(_dataSO, "alleyWidth",          "Ancho de callejón (m)");
        GUILayout.Space(4);
        Field(_dataSO, "objectsPerFrame",     "Objetos/frame (generación lenta)");
        Field(_dataSO, "cullingDistance",     "Distancia de culling (m)");

        EditorGUI.indentLevel--;

        if (_dataSO.ApplyModifiedProperties()) EditorUtility.SetDirty(_data);

        if (BtnSecondary("Abrir Inspector completo")) Selection.activeObject = _data;
    }

    // ── Polígono ──────────────────────────────────────────────────────────
    private void DrawSectionPolygon()
    {
        if (!Foldout(ref _secPoly, "📍  POLÍGONO DEL ÁREA")) return;

        GUI.backgroundColor = _editPoly ? C_YELLOW : new Color(0.45f,0.45f,0.5f);
        string lbl = _editPoly ? "✏  Editando — clic para terminar" : "✏  Editar polígono en Scene";
        if (GUILayout.Button(lbl, GUILayout.Height(26)))
        {
            _editPoly = !_editPoly;
            if (_editPoly) Selection.activeGameObject = _gen.gameObject;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        if (_editPoly)
            EditorGUILayout.HelpBox(
                "• Arrastra puntos  • Shift+clic en arista = añadir  • Ctrl+clic = eliminar",
                MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Vértice")) { Undo.RecordObject(_gen,"AddV"); _gen.areaPolygon.Add(Vector2.zero); SceneView.RepaintAll(); }
        if (_gen.areaPolygon?.Count > 3 && GUILayout.Button("- Último")) { Undo.RecordObject(_gen,"RemV"); _gen.areaPolygon.RemoveAt(_gen.areaPolygon.Count-1); SceneView.RepaintAll(); }
        EditorGUILayout.EndHorizontal();
    }

    // ── Estadísticas ──────────────────────────────────────────────────────
    private void DrawSectionStats()
    {
        if (!Foldout(ref _secStats, "📊  ESTADÍSTICAS")) return;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            LabelPair("Edificios generados",  _gen.BuildingsPlaced.ToString());
            LabelPair("Calles generadas",     _gen.RoadsGenerated.ToString());
            LabelPair("Tiempo último gen.",   $"{_gen.LastGenTime*1000f:F1} ms");

            if (_gen.data != null)
            {
                var aabb = GetAABB(_gen.areaPolygon);
                LabelPair("Área del polígono", $"{aabb.width:F0} × {aabb.height:F0} m");
                LabelPair("Semilla usada", _gen.data.seed == 0 ? "Aleatoria" : _gen.data.seed.ToString());
            }
        }

        EditorGUILayout.BeginHorizontal();
        Field(_genSO, "showGizmos",  "Mostrar Gizmos");
        Field(_genSO, "gizmoDetail", "Nivel de detalle Gizmos");
        EditorGUILayout.EndHorizontal();
        if (_genSO != null && _genSO.ApplyModifiedProperties())
        { EditorUtility.SetDirty(_gen); SceneView.RepaintAll(); }

        Field(_genSO, "slowGeneration", "Generación lenta (async)");
        if (_genSO != null) _genSO.ApplyModifiedProperties();
    }

    // ── Presets ───────────────────────────────────────────────────────────
    private void DrawSectionPresets()
    {
        if (!Foldout(ref _secPresets, "💾  PRESETS")) return;

        _lib = (CityPresetLibrary)EditorGUILayout.ObjectField("Preset Library", _lib, typeof(CityPresetLibrary), false);

        if (_lib == null)
        {
            if (BtnPrimary("Crear Preset Library")) CreatePresetLib();
            return;
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Guardar configuración actual", EditorStyles.boldLabel);
            _presetName   = EditorGUILayout.TextField("Nombre",         _presetName);
            _presetNotes  = EditorGUILayout.TextField("Notas",          _presetNotes);
            _presetRating = EditorGUILayout.IntSlider("★ Calificación", _presetRating, 0, 5);

            if (BtnPrimary("💾  Guardar preset") && _gen?.data != null)
            {
                Undo.RecordObject(_lib,"SavePreset");
                _lib.SavePreset(_gen.data, _presetName, _gen.BuildingsPlaced,
                                 _gen.RoadsGenerated, _gen.LastGenTime*1000f);
                int last = _lib.presets.Count - 1;
                _lib.presets[last].rating = _presetRating;
                _lib.presets[last].notes  = _presetNotes;
                EditorUtility.SetDirty(_lib);
                AssetDatabase.SaveAssets();
            }
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"Presets guardados: {_lib.presets.Count}", EditorStyles.boldLabel);

        for (int i = 0; i < _lib.presets.Count; i++)
        {
            var p = _lib.presets[i];
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(
                        $"★{"★★★★★".Substring(0,Mathf.Clamp(p.rating,0,5))}  {p.name}",
                        EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(
                        $"{p.createdDate} | {p.buildingsPlaced} edif. | {p.genTimeMs:F0}ms",
                        EditorStyles.miniLabel);
                    if (!string.IsNullOrEmpty(p.notes))
                        EditorGUILayout.LabelField(p.notes, EditorStyles.wordWrappedMiniLabel);
                }

                using (new EditorGUILayout.VerticalScope(GUILayout.Width(64)))
                {
                    if (GUILayout.Button("Aplicar") && _gen?.data != null)
                    {
                        Undo.RecordObject(_gen.data,"ApplyPreset");
                        _lib.ApplyPreset(i, _gen.data);
                        EditorUtility.SetDirty(_gen.data);
                    }
                    GUI.backgroundColor = C_CLEAR;
                    if (GUILayout.Button("Borrar"))
                    {
                        if (EditorUtility.DisplayDialog("Borrar",$"¿Borrar '{p.name}'?","Sí","No"))
                        { Undo.RecordObject(_lib,"DelPreset"); _lib.presets.RemoveAt(i); EditorUtility.SetDirty(_lib); break; }
                    }
                    GUI.backgroundColor = Color.white;
                }
            }
        }

        EditorGUILayout.BeginHorizontal();
        if (BtnSecondary("Ordenar ★")) { Undo.RecordObject(_lib,"Sort"); _lib.SortByRating(); }
        if (BtnSecondary("Exportar JSON"))  _lib.ExportToJSON();
        if (BtnSecondary("Importar JSON")) { Undo.RecordObject(_lib,"Import"); _lib.ImportFromJSON(); }
        EditorGUILayout.EndHorizontal();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  SCENE VIEW — HANDLES DEL POLÍGONO
    // ══════════════════════════════════════════════════════════════════════
    private void OnSceneGUI(SceneView sv)
    {
        if (_gen == null || !_editPoly) return;
        if (_gen.areaPolygon == null || _gen.areaPolygon.Count < 3) return;

        Event e = Event.current;
        Handles.color = Color.yellow;

        for (int i = 0; i < _gen.areaPolygon.Count; i++)
        {
            Vector3 wp   = L2W(_gen.areaPolygon[i]);
            float   size = HandleUtility.GetHandleSize(wp) * 0.12f;

            // Ctrl + clic = eliminar
            if (e.type == EventType.MouseDown && e.button == 0 && e.control &&
                HandleUtility.DistanceToCircle(wp, size) < 5f && _gen.areaPolygon.Count > 3)
            {
                Undo.RecordObject(_gen,"RemV");
                _gen.areaPolygon.RemoveAt(i);
                e.Use(); SceneView.RepaintAll(); return;
            }

            // Mover vértice
            EditorGUI.BeginChangeCheck();
            Vector3 nwp = Handles.FreeMoveHandle(wp, size, Vector3.zero, Handles.SphereHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_gen,"MoveV");
                _gen.areaPolygon[i] = W2L(nwp);
                Repaint();
            }

            // Arista
            int next = (i+1) % _gen.areaPolygon.Count;
            Vector3 wpN = L2W(_gen.areaPolygon[next]);
            Handles.DrawLine(wp, wpN, 2f);

            // Shift+clic en punto medio = añadir vértice
            Vector3 mid     = (wp + wpN) * 0.5f;
            float   midSize = HandleUtility.GetHandleSize(mid) * 0.07f;
            Handles.color = new Color(1f,1f,0f,0.5f);
            if (e.type == EventType.MouseDown && e.button == 0 && e.shift &&
                HandleUtility.DistanceToCircle(mid, midSize) < 5f)
            {
                Undo.RecordObject(_gen,"AddV");
                _gen.areaPolygon.Insert(i+1, W2L(mid));
                e.Use(); SceneView.RepaintAll(); return;
            }
            Handles.SphereHandleCap(0, mid, Quaternion.identity, midSize, EventType.Repaint);
            Handles.color = Color.yellow;
        }

        // Instrucciones
        Handles.BeginGUI();
        var rect = new Rect(10, sv.position.height-90, 380, 72);
        EditorGUI.DrawRect(rect, new Color(0f,0f,0f,0.6f));
        GUI.Label(rect, "  ✏ Editando polígono\n  • Arrastra puntos\n  • Shift+clic en arista: añadir\n  • Ctrl+clic en vértice: eliminar",
            new GUIStyle { normal={ textColor=Color.white }, padding=new RectOffset(4,4,4,4), fontSize=11 });
        Handles.EndGUI();
    }

    private Vector3 L2W(Vector2 v) => _gen.transform.TransformPoint(new Vector3(v.x,0,v.y));
    private Vector2 W2L(Vector3 w) { var lp = _gen.transform.InverseTransformPoint(w); return new Vector2(lp.x,lp.z); }

    // ══════════════════════════════════════════════════════════════════════
    //  FORMAS PREDEFINIDAS
    // ══════════════════════════════════════════════════════════════════════
    private void ApplySize(float w, float d)
    {
        _sizeW = w; _sizeD = d;
        Undo.RecordObject(_gen,"SetSize");
        float hw=w*0.5f, hd=d*0.5f;
        _gen.areaPolygon = new List<Vector2>
        { new(-hw,-hd), new(hw,-hd), new(hw,hd), new(-hw,hd) };
        SceneView.RepaintAll();
    }

    private void ApplyOrganicShape()
    {
        Undo.RecordObject(_gen,"Organic");
        _gen.areaPolygon = new List<Vector2>
        {
            new(-120,-80), new(-60,-140), new(40,-130),
            new(130,-60),  new(140, 50),  new(70, 130),
            new(-50, 120), new(-130, 40)
        };
        SceneView.RepaintAll();
    }

    private void ApplyLShape()
    {
        Undo.RecordObject(_gen,"LShape");
        _gen.areaPolygon = new List<Vector2>
        {
            new(-100,-100), new(0,-100), new(0,0),
            new(100,0),     new(100,100),new(-100,100)
        };
        SceneView.RepaintAll();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  CREAR ASSETS
    // ══════════════════════════════════════════════════════════════════════
    private void CreateGen()
    {
        var go = new GameObject("CityGenerator");
        Undo.RegisterCreatedObjectUndo(go,"CreateGen");
        _gen = go.AddComponent<CityGenerator>();
        RefreshSOs();
        Selection.activeGameObject = go;
    }

    private void CreateCityData()
    {
        string path = EditorUtility.SaveFilePanelInProject("Crear CityData","CityData_New","asset","");
        if (string.IsNullOrEmpty(path)) return;
        var cd = CreateInstance<CityData>();
        AssetDatabase.CreateAsset(cd, path);
        AssetDatabase.SaveAssets();
        _data = cd;
        if (_gen != null) { Undo.RecordObject(_gen,"SetData"); _gen.data = cd; }
        _dataSO = new SerializedObject(_data);
    }

    private void CreatePresetLib()
    {
        string path = EditorUtility.SaveFilePanelInProject("Crear Preset Library","CityPresetLibrary","asset","");
        if (string.IsNullOrEmpty(path)) return;
        var lib = CreateInstance<CityPresetLibrary>();
        AssetDatabase.CreateAsset(lib, path);
        AssetDatabase.SaveAssets();
        _lib = lib;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  HELPERS DE UI
    // ══════════════════════════════════════════════════════════════════════
    private bool Foldout(ref bool state, string label, Color? color = null)
    {
        var rect = EditorGUILayout.GetControlRect(false, 22f);
        EditorGUI.DrawRect(rect, color.HasValue ? color.Value * 0.6f : C_SEC);
        state = EditorGUI.Foldout(rect, state,
            "  " + label,
            new GUIStyle(EditorStyles.foldout) {
                fontStyle  = FontStyle.Bold,
                normal     = { textColor = Color.white },
                onNormal   = { textColor = Color.white },
                focused    = { textColor = Color.white },
                onFocused  = { textColor = Color.white },
                active     = { textColor = Color.white },
                onActive   = { textColor = Color.white }
            });
        if (state) EditorGUILayout.Space(2);
        return state;
    }

    private bool BtnPrimary(string lbl, float h = 24)
    {
        GUI.backgroundColor = C_BLUE;
        bool r = GUILayout.Button(lbl, GUILayout.Height(h));
        GUI.backgroundColor = Color.white;
        return r;
    }

    private bool BtnSecondary(string lbl, float w = 0)
    {
        GUI.backgroundColor = new Color(0.35f,0.35f,0.40f);
        bool r = w > 0 ? GUILayout.Button(lbl, GUILayout.Width(w))
                       : GUILayout.Button(lbl);
        GUI.backgroundColor = Color.white;
        return r;
    }

    private void Field(SerializedObject so, string prop, string label)
    {
        if (so == null) return;
        var p = so.FindProperty(prop);
        if (p != null) EditorGUILayout.PropertyField(p, new GUIContent(label));
    }

    private void LabelPair(string key, string val)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(key, GUILayout.Width(160));
        EditorGUILayout.LabelField(val, EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();
    }

    private static Rect GetAABB(List<Vector2> poly)
    {
        if (poly == null || poly.Count < 2) return new Rect(0,0,0,0);
        float minX=1e9f,minZ=1e9f,maxX=-1e9f,maxZ=-1e9f;
        foreach (var v in poly) { minX=Mathf.Min(minX,v.x); minZ=Mathf.Min(minZ,v.y); maxX=Mathf.Max(maxX,v.x); maxZ=Mathf.Max(maxZ,v.y); }
        return new Rect(minX,minZ,maxX-minX,maxZ-minZ);
    }
}

// ══════════════════════════════════════════════════════════════════════════
//  CUSTOM INSPECTOR para CityGenerator
// ══════════════════════════════════════════════════════════════════════════
[CustomEditor(typeof(CityGenerator))]
public class CityGeneratorInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(8);
        var gen = (CityGenerator)target;

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.20f,0.65f,0.25f);
        if (GUILayout.Button("▶  Generar", GUILayout.Height(30)))
        { gen.Generate(); EditorUtility.SetDirty(gen); }
        GUI.backgroundColor = new Color(0.70f,0.20f,0.18f);
        if (GUILayout.Button("✕  Limpiar", GUILayout.Height(30)))
            gen.Clear();
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Abrir City Generator Window"))
            CityGeneratorEditor.Open();

        if (gen.IsGenerating)
            EditorGUILayout.HelpBox("⏳ Generando...", MessageType.Info);
        else if (gen.BuildingsPlaced > 0)
            EditorGUILayout.HelpBox(
                $"✓  {gen.BuildingsPlaced} edificios | {gen.RoadsGenerated} calles | {gen.LastGenTime*1000f:F0} ms",
                MessageType.None);

        if (gen.IsGenerating) Repaint();
    }
}
#endif