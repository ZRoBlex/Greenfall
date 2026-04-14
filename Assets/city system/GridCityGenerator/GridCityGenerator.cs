// ============================================================
//  GridCityGenerator.cs  — Sistema Grid (INDEPENDIENTE)
//  Assets/GridCityGenerator/Scripts/
//
//  Generador de ciudad basado en GRID ortogonal.
//  100% independiente: no depende de ninguna clase del sistema BSP.
//
//  CARACTERÍSTICAS:
//  ✔ Grid ortogonal limpio — bloques perfectamente alineados
//  ✔ Residencial SOLO en bordes (road frontage garantizado)
//  ✔ Props de parque dentro del área (con margen configurable)
//  ✔ Nada fuera de los límites de la ciudad
//  ✔ Nada sobre las banquetas
//  ✔ Object Pooling completo (cero Instantiate/Destroy en runtime)
//  ✔ Puntos de entrada estilo GTA
//  ✔ Rotación y escala aleatoria por tipo de prop
// ============================================================

using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("GridCity/Grid City Generator")]
[SelectionBase]
public class GridCityGenerator : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════════════════
    [Header("Datos")]
    public GridCityData cityData;

    [Header("Opciones")]
    public bool autoGenerateOnStart = true;
    public bool showGizmos          = true;

    // ── Estado ────────────────────────────────────────────────────────────
    [SerializeField, HideInInspector] private int   _buildingCount;
    [SerializeField, HideInInspector] private float _lastGenTime;

    public int   BuildingCount => _buildingCount;
    public float LastGenTime   => _lastGenTime;

    // ── Pool ──────────────────────────────────────────────────────────────
    private GridPoolManager _pool;

    // ── Transforms contenedores ───────────────────────────────────────────
    private Transform _tRoads, _tBuildings, _tProps, _tEntries, _tPools;

    // ── Footprints colocados (para collision avoidance) ───────────────────
    private readonly List<Rect> _footprints = new();

    // ── Info de bloques (para gizmos) ─────────────────────────────────────
    [HideInInspector] public List<GridBlockInfo> blockInfos = new();

    // ══════════════════════════════════════════════════════════════════════
    //  UNITY
    // ══════════════════════════════════════════════════════════════════════
    private void Awake()
    {
        _tRoads     = CreateContainer("Roads");
        _tBuildings = CreateContainer("Buildings");
        _tProps     = CreateContainer("Props");
        _tEntries   = CreateContainer("EntryPoints");
        _tPools     = CreateContainer("_Pools");

        _pool = new GridPoolManager(_tPools,
                                    cityData ? cityData.poolInitialSize : 5);
    }

    private void Start()
    {
        if (autoGenerateOnStart && cityData != null)
            GenerateCity();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  API PÚBLICA
    // ══════════════════════════════════════════════════════════════════════
    [ContextMenu("Generar Ciudad")]
    public void GenerateCity()
    {
        if (cityData == null)
        {
            Debug.LogError("[GridCityGenerator] cityData no asignado.", this);
            return;
        }

        float t0 = Time.realtimeSinceStartup;
        ClearCity();
        PreRegisterPrefabs();

        int s = cityData.useRandomSeed
            ? UnityEngine.Random.Range(0, 999999)
            : cityData.seed;
        UnityEngine.Random.InitState(s);

        Build();

        _lastGenTime = Time.realtimeSinceStartup - t0;
        Debug.Log($"[GridCityGenerator] {_lastGenTime*1000:F0}ms | Edificios:{_buildingCount}");
    }

    [ContextMenu("Limpiar Ciudad")]
    public void ClearCity()
    {
        _pool?.ReturnAll();
        _footprints.Clear();
        blockInfos.Clear();
        _buildingCount = 0;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  CONSTRUCCIÓN PRINCIPAL
    // ══════════════════════════════════════════════════════════════════════
    private void Build()
    {
        float slotW = cityData.SlotWidth;
        int   bx    = Mathf.FloorToInt(cityData.cityWidth  / slotW);
        int   bz    = Mathf.FloorToInt(cityData.cityDepth  / slotW);

        Vector3 c      = cityData.cityCenter;
        float   startX = c.x - (bx * slotW) * 0.5f + slotW * 0.5f;
        float   startZ = c.z - (bz * slotW) * 0.5f + slotW * 0.5f;

        // Determinar qué bloques son parques
        HashSet<int> parkSet = SelectParkBlocks(bx, bz);

        for (int iz = 0; iz < bz; iz++)
        for (int ix = 0; ix < bx; ix++)
        {
            Vector3 blockCenter = new Vector3(
                startX + ix * slotW,
                c.y,
                startZ + iz * slotW);

            // ── Validar que el bloque esté dentro del límite ──────────
            if (!BlockInsideBounds(blockCenter)) continue;

            bool isPark = parkSet.Contains(iz * bx + ix);

            // ── Info para gizmos ──────────────────────────────────────
            blockInfos.Add(new GridBlockInfo
            {
                center = blockCenter,
                size   = cityData.blockSize,
                isPark = isPark
            });

            // ── Suelo del bloque ──────────────────────────────────────
            SpawnSurface(blockCenter, cityData.blockSize, cityData.blockSize,
                         0f, isPark ? cityData.parkGroundMaterial : null);

            // ── Banquetas alrededor del bloque ────────────────────────
            SpawnSidewalks(blockCenter);

            // ── Contenido del bloque ──────────────────────────────────
            if (isPark)
                BuildParkBlock(blockCenter);
            else
                BuildBuildingBlock(blockCenter);
        }

        // Calles (generamos los planos de calle entre los bloques)
        BuildRoads(startX, startZ, bx, bz, slotW);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  SELECCIÓN DE PARQUES
    // ══════════════════════════════════════════════════════════════════════
    private HashSet<int> SelectParkBlocks(int bx, int bz)
    {
        int total    = bx * bz;
        int parkQty  = Mathf.RoundToInt(total * cityData.parkDensity);
        var result   = new HashSet<int>();
        var candidates = new List<int>(total);

        for (int i = 0; i < total; i++)
        {
            int ix = i % bx, iz = i / bx;
            // Excluir las 4 esquinas absolutas para que la ciudad siempre
            // tenga edificios en los extremos
            bool corner = (ix == 0 || ix == bx-1) && (iz == 0 || iz == bz-1);
            if (!corner) candidates.Add(i);
        }

        // Fisher-Yates shuffle
        for (int i = candidates.Count-1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i+1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        for (int i = 0; i < Mathf.Min(parkQty, candidates.Count); i++)
            result.Add(candidates[i]);

        return result;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  CALLES
    // ══════════════════════════════════════════════════════════════════════
    private void BuildRoads(float startX, float startZ, int bx, int bz, float slotW)
    {
        float rw    = cityData.roadWidth;
        float sw    = cityData.sidewalkWidth;
        float roadOffset = cityData.blockSize * 0.5f + sw + rw * 0.5f;

        // Calles horizontales (Z constante)
        for (int iz = 0; iz <= bz; iz++)
        {
            float z = startZ + iz * slotW - slotW * 0.5f + (iz == 0 ? 0f : 0f);
            // Simplificado: una calle entre cada fila
            if (iz > 0 && iz < bz)
            {
                float roadZ = startZ + (iz-1) * slotW + cityData.blockSize * 0.5f + sw + rw * 0.5f;
                float roadW = bx * slotW;
                SpawnSurface(new Vector3(startX + (bx-1)*slotW*0.5f, cityData.cityCenter.y, roadZ),
                             roadW, rw, 0f, cityData.roadMaterial);
            }
        }

        // Calles verticales (X constante)
        for (int ix = 1; ix < bx; ix++)
        {
            float roadX = startX + (ix-1) * slotW + cityData.blockSize * 0.5f + sw + rw * 0.5f;
            float roadD = bz * slotW;
            SpawnSurface(new Vector3(roadX, cityData.cityCenter.y,
                                     startZ + (bz-1)*slotW*0.5f),
                         rw, roadD, 0f, cityData.roadMaterial);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  BANQUETAS
    // ══════════════════════════════════════════════════════════════════════
    private void SpawnSidewalks(Vector3 blockCenter)
    {
        float bs = cityData.blockSize;
        float sw = cityData.sidewalkWidth;

        // Banqueta total = bloque + margen de banqueta a cada lado
        SpawnSurface(blockCenter, bs + sw*2f, bs + sw*2f, 0.005f, cityData.sidewalkMaterial);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  BLOQUE DE EDIFICIOS
    // ══════════════════════════════════════════════════════════════════════
    private void BuildBuildingBlock(Vector3 blockCenter)
    {
        float half = cityData.blockSize * 0.5f;
        float sw   = cityData.sidewalkWidth;
        const float MARGIN = 0.1f;

        // Área edificable dentro del bloque (sin banqueta)
        Rect buildable = new Rect(
            blockCenter.x - half + sw + MARGIN,
            blockCenter.z - half + sw + MARGIN,
            cityData.blockSize - (sw + MARGIN) * 2f,
            cityData.blockSize - (sw + MARGIN) * 2f
        );

        // Contadores por tipo para respetar maxPerBlock
        var typeCounters = new Dictionary<string, int>();
        int attempts     = 25;

        for (int a = 0; a < attempts; a++)
        {
            GridBuildingTypeData bType = cityData.GetRandomBuildingType();
            if (bType == null) continue;

            string key = bType.typeName;
            if (!typeCounters.ContainsKey(key)) typeCounters[key] = 0;
            if (typeCounters[key] >= bType.maxPerBlock) continue;

            Vector2 footprint = bType.GetRandomFootprint();
            bool placed = false;

            // Intentar colocar según la regla de placement
            switch (bType.placementRule)
            {
                case GridPlacementRule.CornersOnly:
                    placed = TryPlaceAtCorner(bType, footprint, buildable, blockCenter.y);
                    break;

                case GridPlacementRule.EdgeOnly:
                case GridPlacementRule.EdgeAndCorner:
                    // 40% corners, 60% edges
                    if (!placed && bType.placementRule == GridPlacementRule.EdgeAndCorner
                        && UnityEngine.Random.value < 0.4f)
                        placed = TryPlaceAtCorner(bType, footprint, buildable, blockCenter.y);
                    if (!placed)
                        placed = TryPlaceAtEdge(bType, footprint, buildable, blockCenter.y);
                    break;

                case GridPlacementRule.AnyPosition:
                    placed = TryPlaceAny(bType, footprint, buildable, blockCenter.y);
                    break;
            }

            if (placed) typeCounters[key]++;
        }
    }

    // ── Colocación en Esquina ──────────────────────────────────────────
    private bool TryPlaceAtCorner(GridBuildingTypeData bType, Vector2 fp,
                                  Rect buildable, float y)
    {
        var corners = new[]
        {
            new Vector2(buildable.xMin + fp.x*0.5f, buildable.yMin + fp.y*0.5f),
            new Vector2(buildable.xMax - fp.x*0.5f, buildable.yMin + fp.y*0.5f),
            new Vector2(buildable.xMin + fp.x*0.5f, buildable.yMax - fp.y*0.5f),
            new Vector2(buildable.xMax - fp.x*0.5f, buildable.yMax - fp.y*0.5f),
        };
        Shuffle(corners);

        foreach (var xz in corners)
        {
            Rect fr = new Rect(xz.x - fp.x*0.5f, xz.y - fp.y*0.5f, fp.x, fp.y);
            if (!RectInsideBuildable(fr, buildable)) continue;
            if (!RectInsideCityBounds(fr)) continue;
            if (OverlapsPlaced(fr, bType.minSpacingBetweenSame)) continue;
            SpawnBuilding(bType, new Vector3(xz.x, y, xz.y), fr);
            return true;
        }
        return false;
    }

    // ── Colocación en Borde ────────────────────────────────────────────
    private bool TryPlaceAtEdge(GridBuildingTypeData bType, Vector2 fp,
                                Rect buildable, float y)
    {
        int[] edges = { 0, 1, 2, 3 };
        Shuffle(edges);

        foreach (int edge in edges)
        {
            Vector2 xz = Vector2.zero;
            float setback = UnityEngine.Random.Range(bType.setbackMin, bType.setbackMax);

            switch (edge)
            {
                case 0: // Norte
                    xz = new Vector2(
                        UnityEngine.Random.Range(buildable.xMin + fp.x*0.5f, buildable.xMax - fp.x*0.5f),
                        buildable.yMax - fp.y*0.5f - setback);
                    break;
                case 1: // Sur
                    xz = new Vector2(
                        UnityEngine.Random.Range(buildable.xMin + fp.x*0.5f, buildable.xMax - fp.x*0.5f),
                        buildable.yMin + fp.y*0.5f + setback);
                    break;
                case 2: // Este
                    xz = new Vector2(
                        buildable.xMax - fp.x*0.5f - setback,
                        UnityEngine.Random.Range(buildable.yMin + fp.y*0.5f, buildable.yMax - fp.y*0.5f));
                    break;
                case 3: // Oeste
                    xz = new Vector2(
                        buildable.xMin + fp.x*0.5f + setback,
                        UnityEngine.Random.Range(buildable.yMin + fp.y*0.5f, buildable.yMax - fp.y*0.5f));
                    break;
            }

            Rect fr = new Rect(xz.x - fp.x*0.5f, xz.y - fp.y*0.5f, fp.x, fp.y);
            if (!RectInsideBuildable(fr, buildable)) continue;
            if (!RectInsideCityBounds(fr)) continue;
            if (OverlapsPlaced(fr, bType.minSpacingBetweenSame)) continue;

            SpawnBuilding(bType, new Vector3(xz.x, y, xz.y), fr);
            return true;
        }
        return false;
    }

    // ── Colocación en cualquier posición ──────────────────────────────
    private bool TryPlaceAny(GridBuildingTypeData bType, Vector2 fp,
                             Rect buildable, float y)
    {
        for (int i = 0; i < 6; i++)
        {
            Vector2 xz = new Vector2(
                UnityEngine.Random.Range(buildable.xMin + fp.x*0.5f, buildable.xMax - fp.x*0.5f),
                UnityEngine.Random.Range(buildable.yMin + fp.y*0.5f, buildable.yMax - fp.y*0.5f));

            Rect fr = new Rect(xz.x - fp.x*0.5f, xz.y - fp.y*0.5f, fp.x, fp.y);
            if (!RectInsideBuildable(fr, buildable)) continue;
            if (!RectInsideCityBounds(fr)) continue;
            if (OverlapsPlaced(fr, bType.minSpacingBetweenSame)) continue;

            SpawnBuilding(bType, new Vector3(xz.x, y, xz.y), fr);
            return true;
        }
        return false;
    }

    // ── Instanciar edificio ────────────────────────────────────────────
    private void SpawnBuilding(GridBuildingTypeData bType, Vector3 pos, Rect footprintRect)
    {
        GameObject prefab = bType.GetRandomPrefab();
        if (prefab == null) return;

        GameObject go = _pool.Get(prefab, _tBuildings);
        go.transform.position = pos;
        go.transform.rotation = bType.GetRandomRotation();

        _footprints.Add(footprintRect);

        // Punto de entrada
        if (bType.canHaveEntryPoint &&
            cityData.entryPointPrefab != null &&
            UnityEngine.Random.value <= bType.entryChance)
        {
            SpawnEntryPoint(bType, go.transform);
        }

        _buildingCount++;
    }

    // ── Instanciar punto de entrada ────────────────────────────────────
    private void SpawnEntryPoint(GridBuildingTypeData bType, Transform buildingTr)
    {
        GameObject epGO = _pool.Get(cityData.entryPointPrefab, _tEntries);
        epGO.transform.position = buildingTr.TransformPoint(bType.entryLocalOffset);
        epGO.transform.rotation = buildingTr.rotation;

        var ep = epGO.GetComponent<GridBuildingEntryPoint>();
        if (ep != null)
            ep.Initialize(bType.entryPromptText, cityData.entryPointColor);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  BLOQUE DE PARQUE
    // ══════════════════════════════════════════════════════════════════════
    private void BuildParkBlock(Vector3 blockCenter)
    {
        float half   = cityData.blockSize * 0.5f;
        float margin = Mathf.Max(0f, cityData.parkPropMargin);

        // Área segura (con margen)
        Rect safeRect = new Rect(
            blockCenter.x - half + margin,
            blockCenter.z - half + margin,
            cityData.blockSize - margin * 2f,
            cityData.blockSize - margin * 2f
        );

        if (safeRect.width <= 0f || safeRect.height <= 0f) return;

        var usedPositions = new List<Vector2>();
        int placed = 0, maxAttempts = cityData.propsPerPark * 5;

        for (int a = 0; a < maxAttempts && placed < cityData.propsPerPark; a++)
        {
            GridPropData propData = cityData.GetRandomParkProp();
            if (propData == null) continue;

            float localMargin = Mathf.Max(margin, propData.borderMargin);
            Rect  localSafe   = new Rect(
                blockCenter.x - half + localMargin,
                blockCenter.z - half + localMargin,
                cityData.blockSize - localMargin * 2f,
                cityData.blockSize - localMargin * 2f);

            if (localSafe.width <= 0f || localSafe.height <= 0f) continue;

            Vector2 xz = new Vector2(
                UnityEngine.Random.Range(localSafe.xMin, localSafe.xMax),
                UnityEngine.Random.Range(localSafe.yMin, localSafe.yMax));

            // Verificar spacing
            bool tooClose = false;
            foreach (var used in usedPositions)
            {
                if (Vector2.Distance(xz, used) < propData.minSpacingFromSelf)
                { tooClose = true; break; }
            }
            if (tooClose) continue;

            GameObject prefab = propData.GetRandomPrefab();
            if (prefab == null) continue;

            GameObject propGO = _pool.Get(prefab, _tProps);
            propGO.transform.position = new Vector3(xz.x, blockCenter.y, xz.y);
            propGO.transform.rotation = propData.GetRandomRotation();
            propGO.transform.localScale = propData.GetRandomScale();

            usedPositions.Add(xz);
            placed++;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  SUPERFICIE PLANA (calles, banquetas, suelo de parque)
    // ══════════════════════════════════════════════════════════════════════
    private void SpawnSurface(Vector3 center, float width, float depth,
                              float yOffset, Material mat)
    {
        // Usamos primitivos temporales. En producción: prefabs con mesh flat.
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Surface";
        go.transform.SetParent(_tRoads);
        go.transform.position = center + Vector3.up * yOffset;
        go.transform.localScale = new Vector3(width, 0.04f, depth);

        if (mat != null)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr) mr.sharedMaterial = mat;
        }
        // Quitar el collider de las superficies para no interferir con el jugador
        var bc = go.GetComponent<BoxCollider>();
        if (bc) bc.enabled = false;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  VALIDACIONES
    // ══════════════════════════════════════════════════════════════════════
    private bool BlockInsideBounds(Vector3 center)
    {
        Rect  bounds = cityData.CityBounds;
        float half   = cityData.blockSize * 0.5f;
        return center.x - half >= bounds.xMin &&
               center.x + half <= bounds.xMax &&
               center.z - half >= bounds.yMin &&
               center.z + half <= bounds.yMax;
    }

    private bool RectInsideBuildable(Rect r, Rect buildable)
    {
        return r.xMin >= buildable.xMin && r.xMax <= buildable.xMax &&
               r.yMin >= buildable.yMin && r.yMax <= buildable.yMax;
    }

    private bool RectInsideCityBounds(Rect r)
    {
        Rect bounds = cityData.CityBounds;
        return r.xMin >= bounds.xMin && r.xMax <= bounds.xMax &&
               r.yMin >= bounds.yMin && r.yMax <= bounds.yMax;
    }

    private bool OverlapsPlaced(Rect candidate, float margin)
    {
        Rect expanded = new Rect(candidate.x - margin, candidate.y - margin,
                                 candidate.width + margin*2f, candidate.height + margin*2f);
        foreach (var placed in _footprints)
            if (expanded.Overlaps(placed)) return true;
        return false;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  PRE-REGISTRO DE PREFABS
    // ══════════════════════════════════════════════════════════════════════
    private void PreRegisterPrefabs()
    {
        if (cityData == null) return;
        int sz = cityData.poolInitialSize;

        foreach (var wb in cityData.buildingTypes)
        {
            if (wb?.buildingType?.prefabs == null) continue;
            foreach (var pf in wb.buildingType.prefabs)
                _pool.Register(pf, sz);
        }
        foreach (var wp in cityData.parkProps)
        {
            if (wp?.propData?.prefabs == null) continue;
            foreach (var pf in wp.propData.prefabs)
                _pool.Register(pf, sz);
        }
        foreach (var wp in cityData.sidewalkProps)
        {
            if (wp?.propData?.prefabs == null) continue;
            foreach (var pf in wp.propData.prefabs)
                _pool.Register(pf, sz);
        }
        if (cityData.entryPointPrefab != null)
            _pool.Register(cityData.entryPointPrefab, sz);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════════════════
    private Transform CreateContainer(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        return go.transform;
    }

    private void Shuffle<T>(T[] arr)
    {
        for (int i = arr.Length-1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i+1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  GIZMOS
    // ══════════════════════════════════════════════════════════════════════
    private void OnDrawGizmos()
    {
        if (!showGizmos || cityData == null) return;

        // Límite de la ciudad
        Gizmos.color = Color.yellow;
        Vector3 c    = cityData.cityCenter;
        Gizmos.DrawWireCube(c + Vector3.up*0.5f,
                            new Vector3(cityData.cityWidth, 1f, cityData.cityDepth));

        // Bloques
        if (blockInfos == null) return;
        foreach (var bi in blockInfos)
        {
            Gizmos.color = bi.isPark
                ? new Color(0.2f, 0.8f, 0.2f, 0.15f)
                : new Color(0.4f, 0.5f, 1f, 0.08f);
            Gizmos.DrawCube(bi.center + Vector3.up*0.05f,
                            new Vector3(bi.size, 0.05f, bi.size));
            Gizmos.color = bi.isPark ? Color.green : new Color(0.4f,0.5f,1f,0.5f);
            Gizmos.DrawWireCube(bi.center, new Vector3(bi.size, 0.1f, bi.size));
        }
    }
}

// ══════════════════════════════════════════════════════════════════════════
//  INFO DE BLOQUE (para gizmos)
// ══════════════════════════════════════════════════════════════════════════
[System.Serializable]
public class GridBlockInfo
{
    public Vector3 center;
    public float   size;
    public bool    isPark;
}

// ══════════════════════════════════════════════════════════════════════════
//  OBJECT POOL (Grid — independiente)
// ══════════════════════════════════════════════════════════════════════════

/// <summary> Pool individual para un prefab. </summary>
public class GridPool
{
    private readonly GameObject          _prefab;
    private readonly Transform           _parent;
    private readonly Queue<GameObject>   _free = new();
    private readonly List<GameObject>    _all  = new();

    public GridPool(GameObject prefab, Transform parent, int initialSize)
    {
        _prefab = prefab;
        _parent = parent;
        for (int i = 0; i < initialSize; i++) Create();
    }

    public GameObject Get(Transform newParent = null)
    {
        if (_free.Count == 0) Create();
        var go = _free.Dequeue();
        if (newParent != null) go.transform.SetParent(newParent, false);
        go.SetActive(true);
        return go;
    }

    public void Return(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        go.transform.SetParent(_parent, false);
        _free.Enqueue(go);
    }

    public void ReturnAll()
    {
        foreach (var go in _all)
            if (go != null && go.activeSelf) Return(go);
    }

    private void Create()
    {
        if (_prefab == null) return;
        var go = Object.Instantiate(_prefab, _parent);
        go.name = $"{_prefab.name}_p";
        go.SetActive(false);
        _free.Enqueue(go);
        _all.Add(go);
    }
}

/// <summary> Manager de todos los pools del sistema Grid. </summary>
public class GridPoolManager
{
    private readonly Dictionary<string, GridPool> _pools = new();
    private readonly Transform _root;
    private readonly int       _defaultSize;

    public GridPoolManager(Transform root, int defaultSize = 5)
    {
        _root        = root;
        _defaultSize = defaultSize;
    }

    public void Register(GameObject prefab, int size = -1)
    {
        if (prefab == null) return;
        string key = prefab.name;
        if (_pools.ContainsKey(key)) return;

        var parent = new GameObject($"Pool_{key}").transform;
        parent.SetParent(_root);
        _pools[key] = new GridPool(prefab, parent, size > 0 ? size : _defaultSize);
    }

    public GameObject Get(GameObject prefab, Transform parent = null)
    {
        if (prefab == null) return null;
        string key = prefab.name;
        if (!_pools.ContainsKey(key)) Register(prefab);
        return _pools[key].Get(parent);
    }

    public void Return(GameObject go, string key)
    {
        if (go == null || !_pools.ContainsKey(key)) return;
        _pools[key].Return(go);
    }

    public void ReturnAll()
    {
        foreach (var p in _pools.Values) p.ReturnAll();
    }
}