// ============================================================
//  CityGenerator.cs  — v5  FIXED + ENTRY POINTS
//  BSP City Generator — Assets/CityGenerator/Scripts/
//
//  FIXES vs v4:
//  ✔ Residencial SOLO en bordes del bloque (acceso a calle garantizado)
//  ✔ Props de parque NO salen del área del parque (rect + polygon)
//  ✔ Nada fuera del polígono de ciudad
//  ✔ Edificios no se generan encima de banquetas
//  ✔ Rotación y escala aleatoria por prop (según PropEntry)
//  ✔ Sistema de entradas estilo GTA (BuildingEntryPoint)
//  ✔ Props de calle alineados a la dirección de la calle
// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("CityGenerator/City Generator")]
[SelectionBase]
public class CityGenerator : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════════════════
    [Header("Datos")]
    public CityData data;

    [Header("Polígono del Área")]
    [Tooltip("Vértices en espacio LOCAL (XZ). Edita con la ventana de editor o handles.")]
    public List<Vector2> areaPolygon = new()
    {
        new(-100,-100), new(100,-100), new(100,100), new(-100,100)
    };

    [Header("Opciones")]
    public bool slowGeneration = false;
    public bool showGizmos     = true;
    [Range(0,3)] public int gizmoDetail = 2;

    // ── Estado (solo lectura) ─────────────────────────────────────────────
    [SerializeField, HideInInspector] private int   _buildings;
    [SerializeField, HideInInspector] private int   _roads;
    [SerializeField, HideInInspector] private bool  _generating;
    [SerializeField, HideInInspector] private float _genTime;

    public int   BuildingsPlaced => _buildings;
    public int   RoadsGenerated  => _roads;
    public bool  IsGenerating    => _generating;
    public float LastGenTime     => _genTime;

    // ── Nodo BSP ──────────────────────────────────────────────────────────
    private class BspNode
    {
        public Rect    rect;
        public BspNode left, right;
        public bool    isLeaf;
        public int     depth;
        public int     zoneIdx; // -1 = parque
    }

    // ── Datos para gizmos ─────────────────────────────────────────────────
    private BspNode                           _root;
    private List<Rect>                        _roadRects  = new();
    private List<(Rect, int)>                 _blockData  = new();
    private List<(Vector3, float, float, float)> _bldGizmos = new();

    // ── Contenedores de escena ────────────────────────────────────────────
    private Transform _tRoads, _tBldgs, _tSW, _tParks, _tProps, _tEntries;

    // ══════════════════════════════════════════════════════════════════════
    //  API PÚBLICA
    // ══════════════════════════════════════════════════════════════════════
    public void Generate()
    {
        if (_generating) return;
        Clear();
        if (data == null) { Debug.LogError("[CityGen] Falta CityData.", this); return; }

        int s = data.seed != 0 ? data.seed : UnityEngine.Random.Range(1, int.MaxValue);
        UnityEngine.Random.InitState(s);
        MakeContainers();

        if (slowGeneration && Application.isPlaying)
            StartCoroutine(CR_Generate());
        else
            RunImmediate();
    }

    public void Clear()
    {
        StopAllCoroutines();
        _generating = false;
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
            if (Application.isPlaying) Destroy(c.gameObject);
            else DestroyImmediate(c.gameObject);
        }
        _root = null;
        _roadRects = new(); _blockData = new(); _bldGizmos = new();
        _buildings = 0; _roads = 0;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  PIPELINE
    // ══════════════════════════════════════════════════════════════════════
    private void RunImmediate()
    {
        float t0 = Time.realtimeSinceStartup;
        _generating = true;
        BuildBsp();
        AssignZones(_root);
        GenRoads();
        WalkBlocks(_root, n => { if (n.isLeaf) ProcessLeaf(n); });
        GenStreetProps();
        _generating = false;
        _genTime = Time.realtimeSinceStartup - t0;
        Debug.Log($"[CityGen] {_genTime*1000:F0}ms | Edificios:{_buildings} Calles:{_roads}");
    }

    private IEnumerator CR_Generate()
    {
        _generating = true;
        float t0 = Time.realtimeSinceStartup;
        BuildBsp(); AssignZones(_root); yield return null;
        GenRoads(); yield return null;

        int frame = 0;
        foreach (var leaf in CollectLeaves(_root))
        {
            ProcessLeaf(leaf);
            if (++frame % data.objectsPerFrame == 0) yield return null;
        }

        GenStreetProps();
        _generating = false;
        _genTime = Time.realtimeSinceStartup - t0;
        Debug.Log($"[CityGen] {_genTime*1000:F0}ms | Edificios:{_buildings} Calles:{_roads}");
    }

    // ══════════════════════════════════════════════════════════════════════
    //  BSP — DIVIDIR CON CALLES
    // ══════════════════════════════════════════════════════════════════════
    private void BuildBsp()
    {
        _root = new BspNode { rect = GetAABB(), depth = 0 };
        SplitNode(_root);
    }

    private void SplitNode(BspNode n)
    {
        if (n.depth >= data.bspDepth) { n.isLeaf = true; return; }

        float w = n.rect.width, h = n.rect.height;
        float roadW = n.depth == 0 ? data.mainRoadWidth
                    : n.depth <= 2 ? data.secondaryRoadWidth
                    :                data.localRoadWidth;

        bool horizontal = h > w * 1.2f || (!(w > h * 1.2f) && n.depth % 2 == 0);

        float len  = horizontal ? h : w;
        float minS = data.minBlockSize + roadW;
        float maxS = len - data.minBlockSize - roadW;
        if (maxS <= minS) { n.isLeaf = true; return; }

        float center = len * 0.5f;
        float vary   = center * data.splitVariance;
        float split  = Mathf.Clamp(center + UnityEngine.Random.Range(-vary, vary), minS, maxS);
        float half   = roadW * 0.5f;

        Rect rL, rR, rRoad;
        if (horizontal)
        {
            rL    = new Rect(n.rect.x, n.rect.y,               w, split - half);
            rR    = new Rect(n.rect.x, n.rect.y + split + half, w, h - split - half);
            rRoad = new Rect(n.rect.x, n.rect.y + split - half, w, roadW);
        }
        else
        {
            rL    = new Rect(n.rect.x,               n.rect.y, split - half,     h);
            rR    = new Rect(n.rect.x + split + half, n.rect.y, w - split - half, h);
            rRoad = new Rect(n.rect.x + split - half, n.rect.y, roadW,            h);
        }

        _roadRects.Add(rRoad);
        n.left  = new BspNode { rect = rL,   depth = n.depth + 1 };
        n.right = new BspNode { rect = rR,   depth = n.depth + 1 };
        SplitNode(n.left);
        SplitNode(n.right);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ZONAS
    // ══════════════════════════════════════════════════════════════════════
    private void AssignZones(BspNode n)
    {
        if (!n.isLeaf) { WalkBoth(n, AssignZones); return; }
        n.zoneIdx = UnityEngine.Random.value < data.parkRatio ? -1 : PickZone();
    }

    private int PickZone()
    {
        if (data.buildingTypes == null || data.buildingTypes.Count == 0) return -1;
        float total = 0f;
        foreach (var bt in data.buildingTypes) total += Mathf.Max(0f, bt.weight);
        if (total <= 0f) return UnityEngine.Random.Range(0, data.buildingTypes.Count);

        float pick = UnityEngine.Random.value * total, acc = 0f;
        for (int i = 0; i < data.buildingTypes.Count; i++)
        {
            acc += data.buildingTypes[i].weight;
            if (pick <= acc) return i;
        }
        return data.buildingTypes.Count - 1;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  CALLES
    // ══════════════════════════════════════════════════════════════════════
    private void GenRoads()
    {
        var mat = data.roadMaterial ?? MakeMat(new Color(0.18f,0.18f,0.18f), "Road");
        foreach (var r in _roadRects)
        {
            if (!AnyInPolygon(r)) continue;
            SpawnFlatGO("Road", r, 0f, 0.02f, mat, true, _tRoads);
            _roads++;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  PROCESAR BLOQUE HOJA
    // ══════════════════════════════════════════════════════════════════════
    private void ProcessLeaf(BspNode n)
    {
        // ── FIX: verificar que el centro del bloque esté dentro del polígono ──
        if (!InPolygon(n.rect.center.x, n.rect.center.y)) return;

        _blockData.Add((n.rect, n.zoneIdx));

        if (n.zoneIdx == -1) { SpawnPark(n.rect); return; }

        // Suelo del bloque
        var mat = data.roadMaterial ?? MakeMat(new Color(0.22f, 0.20f, 0.18f), "BlockGnd");
        SpawnFlatGO("BlockGnd", n.rect, 0f, 0.01f, mat, false, _tBldgs);

        SpawnSidewalks(n.rect);
        FillBlockGrid(n);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  BANQUETAS
    // ══════════════════════════════════════════════════════════════════════
    private void SpawnSidewalks(Rect r)
    {
        float sw = data.sidewalkWidth, sh = data.sidewalkHeight;
        var   mat = data.sidewalkMaterial ?? MakeMat(new Color(0.72f,0.70f,0.68f), "SW");

        // Sur, Norte, Oeste, Este (sin esquinas dobles)
        SpawnSWBox(r.x,         r.y,         r.width,          sw, sh, mat);
        SpawnSWBox(r.x,         r.yMax - sw, r.width,          sw, sh, mat);
        SpawnSWBox(r.x,         r.y + sw,    sw, r.height-sw*2f, sh, mat);
        SpawnSWBox(r.xMax - sw, r.y + sw,    sw, r.height-sw*2f, sh, mat);
    }

    private void SpawnSWBox(float x, float z, float w, float d, float h, Material mat)
    {
        if (w <= 0f || d <= 0f) return;
        var go = new GameObject("SW");
        go.transform.SetParent(_tSW, false);
        go.transform.localPosition = new Vector3(x + w * 0.5f, h * 0.5f, z + d * 0.5f);
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial    = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows    = true;
        mf.sharedMesh        = MakeQuad(w, d);
        var bc  = go.AddComponent<BoxCollider>();
        bc.size = new Vector3(w, h, d);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  FILL BLOCK GRID — llenar bloque con edificios
    //
    //  FIX PRINCIPAL: respeta placementRule de cada BuildingType.
    //  - EdgeOnly: solo fila/columna exterior del grid → acceso garantizado a la calle.
    //  - CornersOnly: solo las 4 esquinas del grid.
    //  - EdgeAndCorner: toda la fila/columna exterior.
    //  - AnyPosition: cualquier celda.
    //
    //  Nada se coloca encima de banquetas (se resta sw del área usable).
    // ══════════════════════════════════════════════════════════════════════
    private void FillBlockGrid(BspNode n)
    {
        int zi = n.zoneIdx;
        if (zi < 0 || zi >= data.buildingTypes.Count) return;

        var bt      = data.buildingTypes[zi];
        var floorPf = bt.GetRandomFloorPrefab();

        if (floorPf == null) { FillFallback(n.rect, bt); return; }

        var fd = floorPf.GetComponent<BuildingFloor>();
        if (fd == null) { FillFallback(n.rect, bt); return; }

        float sw     = data.sidewalkWidth;
        float bw     = fd.Width;
        float bd     = fd.Depth;

        // Área interna sin banquetas
        float innerW = n.rect.width  - sw * 2f;
        float innerD = n.rect.height - sw * 2f;
        float innerX = n.rect.x + sw;
        float innerZ = n.rect.y + sw;

        // ── FIX: validar área interna mínima ──────────────────────────
        if (innerW <= bw * 0.5f || innerD <= bd * 0.5f) return;

        int cols = Mathf.Max(1, Mathf.FloorToInt(innerW / bw));
        int rows = Mathf.Max(1, Mathf.FloorToInt(innerD / bd));

        float usedW   = cols * bw;
        float usedD   = rows * bd;
        float marginX = (innerW - usedW) * 0.5f;
        float marginZ = (innerD - usedD) * 0.5f;

        float startX = innerX + marginX;
        float startZ = innerZ + marginZ;

        float blockMidZ = n.rect.center.y;
        float curZ = startZ;

        for (int row = 0; row < rows; row++)
        {
            float cz = curZ + bd * 0.5f;

            // Orientación de la puerta: sur o norte según posición en el bloque
            float targetWorldAngle = cz < blockMidZ ? 180f : 0f;
            float rotY             = fd.GetDoorRotationY(targetWorldAngle);

            float curX = startX;

            for (int col = 0; col < cols; col++)
            {
                float cx = curX + bw * 0.5f;

                // ── FIX: verificar regla de colocación ────────────────
                bool isEdgeRow = row == 0 || row == rows - 1;
                bool isEdgeCol = col == 0 || col == cols - 1;
                bool isEdge    = isEdgeRow || isEdgeCol;
                bool isCorner  = isEdgeRow && isEdgeCol;

                bool canPlace = bt.placementRule switch
                {
                    BuildingPlacementRule.EdgeOnly      => isEdge,
                    BuildingPlacementRule.CornersOnly   => isCorner,
                    BuildingPlacementRule.EdgeAndCorner => isEdge,
                    BuildingPlacementRule.AnyPosition   => true,
                    _                                   => true
                };

                if (!canPlace)
                {
                    curX += bw;
                    continue; // saltar posiciones interiores para residencial
                }

                // ── FIX: verificar que la posición esté dentro del polígono ──
                if (!InPolygon(cx, cz))
                {
                    curX += bw;
                    continue;
                }

                // ── FIX: verificar que el footprint no sobresalga de la banqueta ──
                // La posición ya viene del área innerW/innerD (descontadas las banquetas),
                // así que este check es una doble verificación.
                bool footprintOk =
                    cx - bw * 0.5f >= innerX &&
                    cx + bw * 0.5f <= innerX + innerW &&
                    cz - bd * 0.5f >= innerZ &&
                    cz + bd * 0.5f <= innerZ + innerD;

                if (!footprintOk)
                {
                    curX += bw;
                    continue;
                }

                var pos = new Vector3(cx, 0f, cz);
                SpawnBuilding(pos, bt, fd, rotY);
                curX += bw;

                // Callejón entre columnas
                if (col < cols - 1 && UnityEngine.Random.value < data.alleyChance)
                {
                    SpawnAlleyProps(curX, curZ, data.alleyWidth, bd);
                    curX += data.alleyWidth;
                }
            }

            curZ += bd;

            // Callejón entre filas
            if (row < rows - 1 && UnityEngine.Random.value < data.alleyChance)
            {
                SpawnAlleyProps(startX, curZ, usedW, data.alleyWidth);
                curZ += data.alleyWidth;
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  INSTANCIAR EDIFICIO
    // ══════════════════════════════════════════════════════════════════════
    private void SpawnBuilding(Vector3 lpos, BuildingType bt, BuildingFloor fd, float rotY)
    {
        var roofPf = bt.GetRandomRoofPrefab();
        int floors = bt.GetRandomFloorCount();

        var root = new GameObject($"Bld_{bt.name}_{floors}p");
        root.transform.SetParent(_tBldgs, false);
        root.transform.localPosition = lpos;
        root.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);

        float curY = 0f, totalH = 0f;

        for (int i = 0; i < floors; i++)
        {
            var  pf = fd.GetPrefabForFloor(i);
            float fh = fd.GetFloorHeight(i);

            if (pf != null)
            {
                var go = Instantiate(pf, root.transform);
                go.transform.localPosition = new Vector3(0f, curY, 0f);
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale    = Vector3.one;
                go.name = i == 0 ? "PlantaBaja" : $"Piso_{i}";
                foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
            }

            curY   += fh;
            totalH  = curY;
        }

        // Techo
        if (roofPf != null)
        {
            var rgo = Instantiate(roofPf, root.transform);
            rgo.transform.localPosition = new Vector3(0f, curY, 0f);
            rgo.transform.localRotation = Quaternion.identity;
            rgo.transform.localScale    = Vector3.one;
            rgo.name = "Techo";
            foreach (var c in rgo.GetComponentsInChildren<Collider>()) c.enabled = false;
            var rfd = roofPf.GetComponent<BuildingFloor>();
            totalH += rfd != null ? rfd.Height : 0.3f;
        }

        // BoxCollider único
        var bc    = root.AddComponent<BoxCollider>();
        bc.size   = new Vector3(fd.Width, totalH, fd.Depth);
        bc.center = new Vector3(0f, totalH * 0.5f, 0f);

        // Gizmo data
        Vector3 worldPos = transform.TransformPoint(lpos);
        _bldGizmos.Add((worldPos, fd.Width, fd.Depth, root.transform.eulerAngles.y));

        // ── PUNTO DE ENTRADA (estilo GTA) ─────────────────────────────
        if (bt.canHaveEntryPoint &&
            data.entryPointEffectPrefab != null &&
            UnityEngine.Random.value <= bt.entryPointChance)
        {
            SpawnEntryPoint(bt, root.transform, fd);
        }

        AddCulling(root);
        _buildings++;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  PUNTO DE ENTRADA — estilo GTA
    // ══════════════════════════════════════════════════════════════════════
    private void SpawnEntryPoint(BuildingType bt, Transform buildingRoot, BuildingFloor fd)
    {
        // Instanciar el prefab del efecto
        var epGO = Instantiate(data.entryPointEffectPrefab, _tEntries);

        // Posición: offset local → mundo
        Vector3 worldOffset = buildingRoot.TransformPoint(bt.entryPointOffset);
        epGO.transform.position = worldOffset;
        epGO.transform.rotation = buildingRoot.rotation;
        epGO.name = $"EntryPoint_{bt.name}";

        // Inicializar el componente si existe
        var ep = epGO.GetComponent<CityBuildingEntryPoint>();
        if (ep != null)
            ep.Initialize(bt.entryPromptText, data.entryPointColor);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  PROPS DE CALLEJÓN
    // ══════════════════════════════════════════════════════════════════════
    private void SpawnAlleyProps(float x, float z, float w, float d)
    {
        if (data.alleyProps == null) return;
        foreach (var ap in data.alleyProps)
        {
            if (ap.prefab == null || UnityEngine.Random.value > ap.chance) continue;
            int count = UnityEngine.Random.Range(1, ap.maxPerAlley + 1);
            for (int i = 0; i < count; i++)
            {
                float px = x + UnityEngine.Random.value * w;
                float pz = z + UnityEngine.Random.value * d;
                if (!InPolygon(px, pz)) continue;  // FIX: verificar polígono
                var go = Instantiate(ap.prefab, _tProps);
                go.transform.localPosition = new Vector3(px, 0f, pz);
                go.transform.localRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f,360f), 0f);
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  FALLBACK (sin prefabs válidos)
    // ══════════════════════════════════════════════════════════════════════
    private void FillFallback(Rect r, BuildingType bt)
    {
        float sw = data.sidewalkWidth;
        float bw = 10f, bd = 10f;
        Rect inner = new Rect(r.x+sw, r.y+sw, r.width-sw*2f, r.height-sw*2f);

        if (inner.width <= 0f || inner.height <= 0f) return;

        int cols    = Mathf.Max(1, Mathf.FloorToInt(inner.width  / bw));
        int rows    = Mathf.Max(1, Mathf.FloorToInt(inner.height / bd));
        float marginX = (inner.width  - cols * bw) * 0.5f;
        float marginZ = (inner.height - rows * bd) * 0.5f;
        Color col = bt.gizmoColor; col.a = 1f;

        for (int row = 0; row < rows; row++)
        for (int col2 = 0; col2 < cols; col2++)
        {
            // FIX: respetar placementRule también en el fallback
            bool isEdgeRow = row == 0 || row == rows - 1;
            bool isEdgeCol = col2 == 0 || col2 == cols - 1;
            bool isEdge    = isEdgeRow || isEdgeCol;
            bool isCorner  = isEdgeRow && isEdgeCol;

            bool canPlace = bt.placementRule switch
            {
                BuildingPlacementRule.EdgeOnly      => isEdge,
                BuildingPlacementRule.CornersOnly   => isCorner,
                BuildingPlacementRule.EdgeAndCorner => isEdge,
                _                                   => true
            };
            if (!canPlace) continue;

            float cx = inner.x + marginX + col2 * bw + bw * 0.5f;
            float cz = inner.y + marginZ + row  * bd + bd * 0.5f;

            if (!InPolygon(cx, cz)) continue;

            int   floors = bt.GetRandomFloorCount();
            float bh     = 3f;

            var root = new GameObject($"Bld_FB_{bt.name}");
            root.transform.SetParent(_tBldgs, false);
            root.transform.localPosition = new Vector3(cx, 0f, cz);

            for (int i = 0; i < floors; i++)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.SetParent(root.transform, false);
                c.transform.localPosition = new Vector3(0f, i * bh + bh * 0.5f, 0f);
                c.transform.localScale    = new Vector3(bw, bh * 0.97f, bd);
                c.name = i == 0 ? "PlantaBaja_FB" : $"Piso_{i}_FB";
                float v = UnityEngine.Random.Range(0.85f, 1.05f);
                var mpb = new MaterialPropertyBlock();
                mpb.SetColor("_Color", new Color(
                    Mathf.Clamp01(col.r*v), Mathf.Clamp01(col.g*v), Mathf.Clamp01(col.b*v)));
                c.GetComponent<MeshRenderer>().SetPropertyBlock(mpb);
                Destroy(c.GetComponent<Collider>());
            }

            var bc = root.AddComponent<BoxCollider>();
            bc.size   = new Vector3(bw, bh * floors, bd);
            bc.center = new Vector3(0f, bh * floors * 0.5f, 0f);
            AddCulling(root);
            _buildings++;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  PARQUE — FIX: props dentro del rect del parque + polígono
    // ══════════════════════════════════════════════════════════════════════
    private void SpawnPark(Rect r)
    {
        var mat = data.parkGroundMaterial ?? MakeMat(new Color(0.22f,0.50f,0.18f), "Park");
        SpawnFlatGO("Park", r, 0.003f, 0.02f, mat, true, _tParks);

        if (data.parkProps == null || data.parkProps.Count == 0) return;

        // ── FIX: Calcular el área SEGURA del parque ──────────────────
        // Restamos el parkPropMargin para que los props no sobresalgan del borde
        float margin = Mathf.Max(0f, data.parkPropMargin);
        Rect safeRect = new Rect(
            r.x      + margin,
            r.y      + margin,
            r.width  - margin * 2f,
            r.height - margin * 2f
        );

        // Si el margen es demasiado grande y no queda área, no colocamos nada
        if (safeRect.width <= 0f || safeRect.height <= 0f) return;

        foreach (var pe in data.parkProps)
        {
            if (pe.prefab == null) continue;

            // Calcular cuántos props de este tipo colocar según densidad y área
            int n = Mathf.Max(1,
                Mathf.FloorToInt(safeRect.width * safeRect.height * pe.density));

            for (int i = 0; i < n; i++)
            {
                // Posición dentro del área SEGURA del parque
                float px = safeRect.x + UnityEngine.Random.value * safeRect.width;
                float pz = safeRect.y + UnityEngine.Random.value * safeRect.height;

                // ── FIX 1: verificar que está dentro del polígono de ciudad ──
                if (!InPolygon(px, pz)) continue;

                // ── FIX 2: verificar que NO salga del rect del parque ──
                // (doble verificación por si el prop tiene un offset de pivote)
                if (!r.Contains(new Vector2(px, pz))) continue;

                var p = Instantiate(pe.prefab, _tParks);
                p.transform.localPosition = new Vector3(px, 0f, pz);

                // ── FIX 3: aplicar rotación y escala según PropEntry ──
                p.transform.localRotation = pe.GetRandomRotation();
                p.transform.localScale    = pe.GetRandomScale();
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  PROPS DE CALLE — alineados a la dirección de la calle
    // ══════════════════════════════════════════════════════════════════════
    private void GenStreetProps()
    {
        if (data.streetProps == null) return;
        foreach (var sp in data.streetProps)
        {
            if (sp.prefab == null) continue;
            foreach (var r in _roadRects)
            {
                bool hz  = r.width > r.height;
                float len = hz ? r.width : r.height;
                int   cnt = Mathf.Max(1, Mathf.FloorToInt(len / sp.spacing));

                // Ángulo base de la calle para alinear el prop
                float roadAngle = hz ? 90f : 0f;

                for (int i = 0; i < cnt; i++)
                {
                    if (UnityEngine.Random.value > sp.chance) continue;

                    float t   = (i + 0.5f) / cnt;
                    float lat = sp.lateralOffset * (UnityEngine.Random.value > 0.5f ? 1f : -1f);
                    float px  = hz ? r.x + t*r.width  : r.center.x + lat;
                    float pz  = hz ? r.center.y + lat : r.y + t*r.height;

                    // FIX: verificar polígono
                    if (!InPolygon(px, pz)) continue;

                    var p = Instantiate(sp.prefab, _tProps);
                    p.transform.localPosition = new Vector3(px, 0f, pz);

                    // Rotación: alineada a calle + ruido opcional
                    float finalRot = sp.alignToRoad
                        ? roadAngle + UnityEngine.Random.Range(-sp.rotationNoise, sp.rotationNoise)
                        : UnityEngine.Random.Range(0f, 360f);
                    p.transform.localRotation = Quaternion.Euler(0f, finalRot, 0f);
                }
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════════════════
    private void SpawnFlatGO(string name, Rect r, float y, float boxH,
                              Material mat, bool addCollider, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(r.center.x, y, r.center.y);
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial    = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows    = true;
        mf.sharedMesh        = MakeQuad(r.width, r.height);
        if (addCollider)
        {
            var bc = go.AddComponent<BoxCollider>();
            bc.size = new Vector3(r.width, boxH, r.height);
        }
    }

    // Point-in-polygon (ray casting, coordenadas locales del generador)
    private bool InPolygon(float x, float z)
    {
        if (areaPolygon == null || areaPolygon.Count < 3) return true;
        int  n      = areaPolygon.Count;
        bool inside = false;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            float xi = areaPolygon[i].x, zi = areaPolygon[i].y;
            float xj = areaPolygon[j].x, zj = areaPolygon[j].y;
            if ((zi > z) != (zj > z) && x < (xj - xi) * (z - zi) / (zj - zi) + xi)
                inside = !inside;
        }
        return inside;
    }

    private bool AnyInPolygon(Rect r)
    {
        if (areaPolygon == null || areaPolygon.Count < 3) return true;
        return InPolygon(r.center.x, r.center.y)
            || InPolygon(r.xMin, r.yMin)
            || InPolygon(r.xMax, r.yMax);
    }

    private Rect GetAABB()
    {
        if (areaPolygon == null || areaPolygon.Count < 3) return new Rect(-100,-100,200,200);
        float minX=1e9f,minZ=1e9f,maxX=-1e9f,maxZ=-1e9f;
        foreach (var v in areaPolygon)
        { minX=Mathf.Min(minX,v.x); minZ=Mathf.Min(minZ,v.y); maxX=Mathf.Max(maxX,v.x); maxZ=Mathf.Max(maxZ,v.y); }
        return new Rect(minX, minZ, maxX-minX, maxZ-minZ);
    }

    private static Mesh MakeQuad(float w, float h)
    {
        float hw=w*0.5f, hh=h*0.5f;
        var m = new Mesh { name = "FlatQuad" };
        m.vertices  = new[]{ new Vector3(-hw,0,-hh), new Vector3(hw,0,-hh), new Vector3(hw,0,hh), new Vector3(-hw,0,hh) };
        m.uv        = new[]{ new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1) };
        m.triangles = new[]{ 0,2,1, 0,3,2 };
        m.RecalculateNormals();
        return m;
    }

    private Material MakeMat(Color c, string n)
    {
        var m = new Material(Shader.Find("Standard")) { name = n, color = c };
        if (data != null && data.useGPUInstancing) m.enableInstancing = true;
        return m;
    }

    private void AddCulling(GameObject go)
    {
        if (data == null || data.cullingDistance <= 0f) return;
        go.AddComponent<CityLODCulling>().CullingDistance = data.cullingDistance;
    }

    private void MakeContainers()
    {
        _tRoads   = MkT("Roads");
        _tBldgs   = MkT("Buildings");
        _tSW      = MkT("Sidewalks");
        _tParks   = MkT("Parks");
        _tProps   = MkT("Props");
        _tEntries = MkT("EntryPoints");
    }

    private Transform MkT(string n)
    {
        var go = new GameObject(n);
        go.transform.SetParent(transform, false);
        return go.transform;
    }

    private void WalkBlocks(BspNode n, Action<BspNode> fn)
    {
        if (n == null) return;
        fn(n);
        WalkBlocks(n.left, fn);
        WalkBlocks(n.right, fn);
    }

    private void WalkBoth(BspNode n, Action<BspNode> fn)
    { if (n.left != null) fn(n.left); if (n.right != null) fn(n.right); }

    private List<BspNode> CollectLeaves(BspNode root)
    {
        var list = new List<BspNode>();
        void Collect(BspNode n)
        { if (n == null) return; if (n.isLeaf) list.Add(n); else { Collect(n.left); Collect(n.right); } }
        Collect(root);
        return list;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  GIZMOS
    // ══════════════════════════════════════════════════════════════════════
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        DrawPolygonGizmo();
        if (gizmoDetail < 1) return;
        DrawRoadGizmos();
        if (gizmoDetail < 2) return;
        DrawBlockGizmos();
        if (gizmoDetail < 3) return;
        DrawBuildingGizmos();
    }

    private void DrawPolygonGizmo()
    {
        if (areaPolygon == null || areaPolygon.Count < 2) return;
        Gizmos.color = new Color(1f, 0.9f, 0.1f, 1f);
        for (int i = 0; i < areaPolygon.Count; i++)
        {
            Vector3 a = LXZ(areaPolygon[i]);
            Vector3 b = LXZ(areaPolygon[(i+1) % areaPolygon.Count]);
            Gizmos.DrawLine(a + Vector3.up*0.1f, b + Vector3.up*0.1f);
            Gizmos.DrawSphere(a + Vector3.up*0.1f, 0.8f);
        }
    }

    private void DrawRoadGizmos()
    {
        Gizmos.color = new Color(0.4f,0.4f,0.45f,0.5f);
        foreach (var r in _roadRects)
        {
            Gizmos.DrawCube(
                transform.TransformPoint(new Vector3(r.center.x,0.05f,r.center.y)),
                transform.TransformVector(new Vector3(r.width,0.02f,r.height)));
        }
    }

    private void DrawBlockGizmos()
    {
        foreach (var (r, zi) in _blockData)
        {
            Color c;
            string label;
            if (zi == -1)
            { c = new Color(0.2f,0.7f,0.2f,0.25f); label = "Parque"; }
            else if (zi < data?.buildingTypes?.Count)
            {
                var bt = data.buildingTypes[zi];
                c = new Color(bt.gizmoColor.r, bt.gizmoColor.g, bt.gizmoColor.b, 0.22f);
                label = bt.name;
            }
            else
            { c = new Color(0.5f,0.5f,0.5f,0.2f); label = "?"; }

            Gizmos.color = c;
            Vector3 center = transform.TransformPoint(new Vector3(r.center.x,0.02f,r.center.y));
            Gizmos.DrawCube(center, transform.TransformVector(new Vector3(r.width,0.01f,r.height)));

            Gizmos.color = new Color(c.r, c.g, c.b, 0.8f);
            DrawWireRect(r, 0.05f);
        }
    }

    private void DrawBuildingGizmos()
    {
        foreach (var (pos, w, d, doorAngle) in _bldGizmos)
        {
            Gizmos.color = new Color(0.8f,0.6f,0.2f,0.6f);
            Gizmos.DrawWireCube(pos + Vector3.up*0.5f, new Vector3(w,0.1f,d));
            Gizmos.color = Color.red;
            float rad    = doorAngle * Mathf.Deg2Rad;
            Vector3 dir  = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            Gizmos.DrawRay(pos + Vector3.up*0.5f, dir * (Mathf.Max(w,d)*0.6f));
        }
    }

    private void DrawWireRect(Rect r, float y)
    {
        var p0 = LXZ2(r.xMin,r.yMin,y); var p1 = LXZ2(r.xMax,r.yMin,y);
        var p2 = LXZ2(r.xMax,r.yMax,y); var p3 = LXZ2(r.xMin,r.yMax,y);
        Gizmos.DrawLine(p0,p1); Gizmos.DrawLine(p1,p2);
        Gizmos.DrawLine(p2,p3); Gizmos.DrawLine(p3,p0);
    }

    private Vector3 LXZ(Vector2 v)  => transform.TransformPoint(new Vector3(v.x,0f,v.y));
    private Vector3 LXZ2(float x, float z, float y) => transform.TransformPoint(new Vector3(x,y,z));
}



// ══════════════════════════════════════════════════════════════════════════
//  LOD CULLING LIGERO
// ══════════════════════════════════════════════════════════════════════════
public class CityLODCulling : MonoBehaviour
{
    // ── Configuración ─────────────────────────────────────────────────────

    [Tooltip("Distancia en metros a la que el edificio se oculta. " +
             "Se asigna desde CityGenerator vía CullingDistance property.")]
    public float CullingDistance { get; set; } = 500f;

    // ── Estado interno ────────────────────────────────────────────────────

    // Todos los renderers de este edificio y sus hijos
    private Renderer[] _renderers;

    // Distancia al cuadrado (evita sqrt en comparación, mucho más barato)
    private float _cullingDistanceSq;

    // MEJORA: Cacheamos la cámara principal aquí en vez de buscarla cada frame
    private Camera _mainCamera;

    // Offset de frame para que no todos los edificios chequen en el mismo frame
    // Distribuye la carga entre frames
    private int _frameOffset;

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Recolectamos todos los renderers de este edificio (incluyendo hijos)
        _renderers = GetComponentsInChildren<Renderer>(true);

        // Guardamos un offset aleatorio para distribuir las comprobaciones
        // entre diferentes frames (sin esto, 1000 edificios todos evalúan
        // en el frame 0, 8, 16... creando picos de CPU)
        _frameOffset = UnityEngine.Random.Range(0, 8);
    }

    // ─────────────────────────────────────────────────────────────────────
    private void Start()
    {
        // MEJORA: Calculamos la distancia al cuadrado UNA VEZ aquí
        // en vez de recalcularla cada 8 frames
        _cullingDistanceSq = CullingDistance * CullingDistance;

        // MEJORA: Cacheamos la cámara en Start() (no en Awake, porque
        // la cámara podría no estar inicializada aún en Awake)
        CacheCamera();
    }

    // ─────────────────────────────────────────────────────────────────────
    private void Update()
    {
        // Solo ejecutamos la lógica cada 8 frames para este edificio
        // (el offset garantiza que no todos checan en el mismo frame)
        if ((Time.frameCount + _frameOffset) % 8 != 0) return;

        // Validamos que tenemos renderers que gestionar
        if (_renderers == null || _renderers.Length == 0) return;

        // MEJORA: Si la cámara fue destruida/recargada, intentamos recuperarla
        // Esto cubre transiciones de escena sin crashear
        if (_mainCamera == null)
        {
            CacheCamera();
            if (_mainCamera == null) return; // Sin cámara, no hacemos nada
        }

        // Comparación de distancia al cuadrado (evita la costosa raíz cuadrada)
        float distanceSq = (transform.position - _mainCamera.transform.position).sqrMagnitude;
        bool  visible    = distanceSq <= _cullingDistanceSq;

        // Solo cambiamos el estado si realmente cambió (evita llamadas redundantes)
        foreach (var r in _renderers)
        {
            if (r != null && r.enabled != visible)
                r.enabled = visible;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────────────

    private void CacheCamera()
    {
        _mainCamera = Camera.main;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  LIMPIEZA (necesario si el edificio se destruye/regenera)
    // ─────────────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        _renderers  = null;
        _mainCamera = null;
    }
}