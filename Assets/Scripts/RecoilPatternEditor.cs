// ============================================================
// RecoilPatternEditor.cs
// DÓNDE PONER: carpeta "Editor" dentro de Assets
//   ej: Assets/Scripts/Editor/RecoilPatternEditor.cs
//
// QUÉ HACE:
//   Reemplaza el Inspector de RecoilPatternSO con un canvas
//   donde puedes DIBUJAR el patrón con el mouse.
//
// CÓMO USAR:
//   1. Selecciona el asset RecoilPatternSO en el Project
//   2. En el Inspector aparece el canvas dibujable
//   3. Click y arrastra para dibujar el trazo
//   4. El trazo se convierte automáticamente en puntos
//   5. Usa los botones para limpiar, suavizar, etc.
//
// LECTURA DEL CANVAS:
//   Centro del canvas = (0, 0) del patrón
//   Parte superior    = disparo sube (Y positivo)
//   Parte inferior    = disparo baja (Y negativo)
//   Izquierda/derecha = horizontal del patrón
//   Cada punto del trazo = un disparo
// ============================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RecoilPatternSO))]
public class RecoilPatternEditor : Editor
{
    // ── Constantes ────────────────────────────────────────────
    const float CANVAS_HEIGHT      = 300f;   // alto del canvas en pixels
    const float CANVAS_PADDING     = 20f;    // margen interior
    const float POINT_RADIUS       = 5f;     // radio del círculo de cada punto
    const float MIN_POINT_DISTANCE = 6f;     // dist mínima entre puntos al dibujar

    // Colores del canvas
    static readonly Color BG_COLOR       = new Color(0.15f, 0.15f, 0.15f, 1f);
    static readonly Color GRID_COLOR     = new Color(0.3f,  0.3f,  0.3f,  1f);
    static readonly Color AXIS_COLOR     = new Color(0.5f,  0.5f,  0.5f,  1f);
    static readonly Color LINE_COLOR     = new Color(1f,    0.4f,  0.2f,  1f);
    static readonly Color POINT_COLOR    = new Color(1f,    0.8f,  0f,    1f);
    static readonly Color FIRST_COLOR    = new Color(0.2f,  1f,    0.4f,  1f); // primer punto (verde)
    static readonly Color PREVIEW_COLOR  = new Color(0.6f,  0.8f,  1f,    0.5f);

    // ── Estado ────────────────────────────────────────────────
    bool    _isDrawing;
    Vector2 _lastAddedPoint;   // último punto del canvas (pixels) que se añadió
    Rect    _canvasRect;       // posición del canvas en la ventana
    bool    _initialized;

    // ── Inspector GUI ─────────────────────────────────────────

    public override void OnInspectorGUI()
    {
        RecoilPatternSO pattern = (RecoilPatternSO)target;
        serializedObject.Update();

        DrawTitle();
        DrawStats(pattern);
        EditorGUILayout.Space(4);
        DrawCanvas(pattern);
        EditorGUILayout.Space(6);
        DrawToolbar(pattern);
        EditorGUILayout.Space(8);
        DrawOptions(pattern);
        EditorGUILayout.Space(4);
        DrawPointsList(pattern);

        serializedObject.ApplyModifiedProperties();
    }

    // ── Título ────────────────────────────────────────────────

    void DrawTitle()
    {
        GUIStyle title = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 13,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("RECOIL PATTERN EDITOR", title);
        EditorGUILayout.LabelField("Dibuja el patrón con el mouse. Cada punto = un disparo.",
            EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(4);
    }

    void DrawStats(RecoilPatternSO p)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"Disparos: {p.points.Count}",
                EditorStyles.miniLabel, GUILayout.Width(90));
            EditorGUILayout.LabelField($"Escala: ±{p.canvasScale:F1}",
                EditorStyles.miniLabel, GUILayout.Width(80));
        }
    }

    // ── Canvas dibujable ──────────────────────────────────────

    void DrawCanvas(RecoilPatternSO pattern)
    {
        // Reservar el espacio del canvas
        _canvasRect = GUILayoutUtility.GetRect(
            GUIContent.none,
            GUIStyle.none,
            GUILayout.ExpandWidth(true),
            GUILayout.Height(CANVAS_HEIGHT));

        // Dibujar solo durante Repaint
        if (Event.current.type == EventType.Repaint)
            PaintCanvas(pattern);

        // Procesar eventos del mouse
        HandleMouseInput(pattern);

        // Forzar repaint cuando el mouse está sobre el canvas
        if (_canvasRect.Contains(Event.current.mousePosition))
            Repaint();
    }

    void PaintCanvas(RecoilPatternSO pattern)
    {
        Handles.BeginGUI();

        // Fondo
        EditorGUI.DrawRect(_canvasRect, BG_COLOR);

        // Grid y ejes
        DrawGrid(pattern.canvasScale);
        DrawAxes();

        // Trazo del patrón
        DrawPatternLines(pattern);

        // Puntos numerados
        DrawPatternPoints(pattern);

        // Preview del cursor si está sobre el canvas
        Vector2 mp = Event.current.mousePosition;
        if (_canvasRect.Contains(mp))
        {
            Vector2 gamePos = CanvasToGame(mp, pattern.canvasScale);
            Handles.color = PREVIEW_COLOR;
            Handles.DrawSolidDisc(new Vector3(mp.x, mp.y, 0), Vector3.forward, POINT_RADIUS * 0.7f);

            // Mostrar coordenadas del cursor
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = PREVIEW_COLOR } };
            GUI.Label(new Rect(mp.x + 8, mp.y - 16, 100, 18),
                $"({gamePos.x:F2}, {gamePos.y:F2})", labelStyle);
        }

        Handles.EndGUI();
    }

    void DrawGrid(float scale)
    {
        int divisions = 10;
        float w = _canvasRect.width;
        float h = _canvasRect.height;
        float cx = _canvasRect.x + w * 0.5f;
        float cy = _canvasRect.y + h * 0.5f;

        Handles.color = GRID_COLOR;

        for (int i = -divisions; i <= divisions; i++)
        {
            float fx = cx + (w * 0.5f - CANVAS_PADDING) * i / divisions;
            float fy = cy + (h * 0.5f - CANVAS_PADDING) * i / divisions;

            // Líneas verticales
            Handles.DrawLine(
                new Vector3(fx, _canvasRect.y + CANVAS_PADDING, 0),
                new Vector3(fx, _canvasRect.yMax - CANVAS_PADDING, 0));

            // Líneas horizontales
            Handles.DrawLine(
                new Vector3(_canvasRect.x + CANVAS_PADDING, fy, 0),
                new Vector3(_canvasRect.xMax - CANVAS_PADDING, fy, 0));
        }
    }

    void DrawAxes()
    {
        float w = _canvasRect.width;
        float h = _canvasRect.height;
        float cx = _canvasRect.x + w * 0.5f;
        float cy = _canvasRect.y + h * 0.5f;

        Handles.color = AXIS_COLOR;

        // Eje X (horizontal)
        Handles.DrawLine(
            new Vector3(_canvasRect.x + CANVAS_PADDING, cy, 0),
            new Vector3(_canvasRect.xMax - CANVAS_PADDING, cy, 0));

        // Eje Y (vertical)
        Handles.DrawLine(
            new Vector3(cx, _canvasRect.y + CANVAS_PADDING, 0),
            new Vector3(cx, _canvasRect.yMax - CANVAS_PADDING, 0));

        // Labels de los ejes
        GUIStyle axisStyle = new GUIStyle(EditorStyles.miniLabel)
        { normal = { textColor = AXIS_COLOR } };

        GUI.Label(new Rect(cx + 4, _canvasRect.y + 2, 60, 16), "SUBE", axisStyle);
        GUI.Label(new Rect(cx + 4, _canvasRect.yMax - 18, 60, 16), "BAJA", axisStyle);
        GUI.Label(new Rect(_canvasRect.x + 2, cy - 10, 50, 16), "IZQ", axisStyle);
        GUI.Label(new Rect(_canvasRect.xMax - 32, cy - 10, 50, 16), "DER", axisStyle);
    }

    void DrawPatternLines(RecoilPatternSO p)
    {
        if (p.points.Count < 2) return;

        for (int i = 0; i < p.points.Count - 1; i++)
        {
            Vector2 a = GameToCanvas(p.points[i], p.canvasScale);
            Vector2 b = GameToCanvas(p.points[i + 1], p.canvasScale);

            // Degradar el color hacia el final del patrón
            float t = (float)i / (p.points.Count - 1);
            Handles.color = Color.Lerp(LINE_COLOR, LINE_COLOR * 0.5f, t);

            Handles.DrawLine(new Vector3(a.x, a.y, 0), new Vector3(b.x, b.y, 0));
        }
    }

    void DrawPatternPoints(RecoilPatternSO p)
    {
        GUIStyle numStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            fontSize  = 8,
            alignment = TextAnchor.MiddleCenter
        };

        for (int i = 0; i < p.points.Count; i++)
        {
            Vector2 canvasPos = GameToCanvas(p.points[i], p.canvasScale);

            // Primer punto en verde, el resto amarillo
            Handles.color = (i == 0) ? FIRST_COLOR : POINT_COLOR;
            Handles.DrawSolidDisc(
                new Vector3(canvasPos.x, canvasPos.y, 0),
                Vector3.forward,
                POINT_RADIUS);

            // Número del disparo
            numStyle.normal.textColor = (i == 0) ? FIRST_COLOR : POINT_COLOR;
            GUI.Label(
                new Rect(canvasPos.x - 10, canvasPos.y - 18, 20, 14),
                (i + 1).ToString(),
                numStyle);
        }

        // Primer punto: mostrar "START" debajo
        if (p.points.Count > 0)
        {
            Vector2 startPos = GameToCanvas(p.points[0], p.canvasScale);
            numStyle.normal.textColor = FIRST_COLOR;
            GUI.Label(new Rect(startPos.x - 15, startPos.y + 6, 30, 14), "START", numStyle);
        }
    }

    // ── Input del mouse ───────────────────────────────────────

    void HandleMouseInput(RecoilPatternSO pattern)
    {
        Event e = Event.current;

        if (!_canvasRect.Contains(e.mousePosition)) return;

        switch (e.type)
        {
            case EventType.MouseDown when e.button == 0:
                _isDrawing = true;
                pattern.points.Clear();
                AddPoint(pattern, e.mousePosition);
                _lastAddedPoint = e.mousePosition;
                EditorUtility.SetDirty(target);
                e.Use();
                break;

            case EventType.MouseDrag when e.button == 0 && _isDrawing:
                // Solo añadir punto si se movió suficiente
                float dist = Vector2.Distance(e.mousePosition, _lastAddedPoint);
                if (dist >= MIN_POINT_DISTANCE)
                {
                    AddPoint(pattern, e.mousePosition);
                    _lastAddedPoint = e.mousePosition;
                    EditorUtility.SetDirty(target);
                }
                e.Use();
                Repaint();
                break;

            case EventType.MouseUp when e.button == 0:
                _isDrawing = false;
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
                e.Use();
                break;
        }
    }

    void AddPoint(RecoilPatternSO pattern, Vector2 canvasPos)
    {
        Vector2 gamePos = CanvasToGame(canvasPos, pattern.canvasScale);
        pattern.points.Add(gamePos);
    }

    // ── Botones de herramientas ────────────────────────────────

    void DrawToolbar(RecoilPatternSO pattern)
    {
        EditorGUILayout.LabelField("Herramientas", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("🗑 Limpiar", GUILayout.Height(28)))
            {
                Undo.RecordObject(target, "Clear Recoil Pattern");
                pattern.points.Clear();
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("〰 Suavizar", GUILayout.Height(28)))
            {
                Undo.RecordObject(target, "Smooth Recoil Pattern");
                SmoothPattern(pattern);
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("↕ Normalizar", GUILayout.Height(28)))
            {
                Undo.RecordObject(target, "Normalize Recoil Pattern");
                NormalizePattern(pattern);
                EditorUtility.SetDirty(target);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("← Preset: Rifle", GUILayout.Height(24)))
            {
                Undo.RecordObject(target, "Preset Rifle");
                ApplyPresetRifle(pattern);
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("← Preset: Pistola", GUILayout.Height(24)))
            {
                Undo.RecordObject(target, "Preset Pistol");
                ApplyPresetPistol(pattern);
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("← Preset: Escopeta", GUILayout.Height(24)))
            {
                Undo.RecordObject(target, "Preset Shotgun");
                ApplyPresetShotgun(pattern);
                EditorUtility.SetDirty(target);
            }
        }
    }

    void DrawOptions(RecoilPatternSO pattern)
    {
        EditorGUILayout.LabelField("Configuración", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        pattern.variation  = EditorGUILayout.Slider("Variación aleatoria", pattern.variation, 0f, 0.5f);
        pattern.loopPattern = EditorGUILayout.Toggle("Loop del patrón", pattern.loopPattern);
        pattern.canvasScale = EditorGUILayout.Slider("Escala del canvas", pattern.canvasScale, 1f, 20f);

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(target);
    }

    void DrawPointsList(RecoilPatternSO pattern)
    {
        EditorGUILayout.LabelField($"Puntos generados ({pattern.points.Count})", EditorStyles.boldLabel);

        // Mostrar tabla compacta de puntos
        int maxShow = Mathf.Min(pattern.points.Count, 20);
        for (int i = 0; i < maxShow; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"  {i + 1}.", GUILayout.Width(28));
                Vector2 old = pattern.points[i];
                Vector2 val = EditorGUILayout.Vector2Field("", pattern.points[i]);
                if (val != old)
                {
                    pattern.points[i] = val;
                    EditorUtility.SetDirty(target);
                }
            }
        }

        if (pattern.points.Count > 20)
            EditorGUILayout.LabelField($"  ... y {pattern.points.Count - 20} más.",
                EditorStyles.miniLabel);
    }

    // ── Conversión canvas ↔ game ──────────────────────────────

    /// <summary>Convierte posición del canvas (pixels) a coordenadas del patrón.</summary>
    Vector2 CanvasToGame(Vector2 canvasPos, float scale)
    {
        float w  = _canvasRect.width  - CANVAS_PADDING * 2f;
        float h  = _canvasRect.height - CANVAS_PADDING * 2f;
        float cx = _canvasRect.x + _canvasRect.width  * 0.5f;
        float cy = _canvasRect.y + _canvasRect.height * 0.5f;

        // X: izquierda = negativo, derecha = positivo
        float gx = (canvasPos.x - cx) / (w * 0.5f) * scale;

        // Y: arriba en canvas = Y positivo en juego (sube)
        // En Unity GUI, Y crece hacia abajo, así que invertimos
        float gy = -(canvasPos.y - cy) / (h * 0.5f) * scale;

        return new Vector2(gx, gy);
    }

    /// <summary>Convierte coordenadas del patrón a posición del canvas (pixels).</summary>
    Vector2 GameToCanvas(Vector2 gamePos, float scale)
    {
        float w  = _canvasRect.width  - CANVAS_PADDING * 2f;
        float h  = _canvasRect.height - CANVAS_PADDING * 2f;
        float cx = _canvasRect.x + _canvasRect.width  * 0.5f;
        float cy = _canvasRect.y + _canvasRect.height * 0.5f;

        float px = cx + gamePos.x / scale * (w * 0.5f);
        float py = cy - gamePos.y / scale * (h * 0.5f); // Y invertido

        return new Vector2(px, py);
    }

    // ── Suavizado ─────────────────────────────────────────────

    void SmoothPattern(RecoilPatternSO p)
    {
        if (p.points.Count < 3) return;

        List<Vector2> smoothed = new List<Vector2>(p.points);

        // Suavizado de Laplace: cada punto se mueve hacia la media de sus vecinos
        for (int iter = 0; iter < 3; iter++)
        {
            for (int i = 1; i < smoothed.Count - 1; i++)
            {
                smoothed[i] = (p.points[i - 1] + p.points[i] + p.points[i + 1]) / 3f;
            }
        }

        p.points = smoothed;
    }

    void NormalizePattern(RecoilPatternSO p)
    {
        if (p.points.Count == 0) return;

        // Encontrar el mayor valor
        float maxVal = 0f;
        foreach (var pt in p.points)
            maxVal = Mathf.Max(maxVal, Mathf.Abs(pt.x), Mathf.Abs(pt.y));

        if (maxVal < 0.001f) return;

        // Normalizar a rango [-1, 1]
        for (int i = 0; i < p.points.Count; i++)
            p.points[i] = p.points[i] / maxVal;
    }

    // ── Presets ───────────────────────────────────────────────

    void ApplyPresetRifle(RecoilPatternSO p)
    {
        p.points = new List<Vector2>
        {
            new Vector2( 0.00f,  0.80f),
            new Vector2( 0.05f,  0.85f),
            new Vector2(-0.10f,  0.80f),
            new Vector2( 0.15f,  0.75f),
            new Vector2(-0.20f,  0.70f),
            new Vector2( 0.20f,  0.65f),
            new Vector2(-0.15f,  0.60f),
            new Vector2( 0.10f,  0.55f),
            new Vector2(-0.10f,  0.50f),
            new Vector2( 0.05f,  0.50f),
        };
        p.variation = 0.12f;
    }

    void ApplyPresetPistol(RecoilPatternSO p)
    {
        p.points = new List<Vector2>
        {
            new Vector2( 0.00f,  1.20f),
            new Vector2( 0.10f,  0.90f),
            new Vector2(-0.15f,  0.70f),
            new Vector2( 0.20f,  0.50f),
            new Vector2(-0.10f,  0.40f),
            new Vector2( 0.05f,  0.35f),
        };
        p.variation = 0.18f;
    }

    void ApplyPresetShotgun(RecoilPatternSO p)
    {
        p.points = new List<Vector2>
        {
            new Vector2( 0.00f,  2.50f),
            new Vector2( 0.30f,  1.80f),
            new Vector2(-0.30f,  1.50f),
        };
        p.variation = 0.4f;
    }
}
#endif
