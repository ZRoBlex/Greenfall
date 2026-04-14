// ============================================================
//  CityPresetLibrary.cs
//  Standalone City Generator — Assets/CityGenerator/Scripts/
//
//  Sistema de Presets / "Aprendizaje":
//  - Guarda configuraciones de ciudades que funcionan bien
//  - Carga cualquier preset al instante
//  - Exporta/importa como JSON para intercambiar entre proyectos
//
//  CREAR: Click derecho → Create → CityGenerator → Preset Library
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "CityPresetLibrary",
                 menuName  = "CityGenerator/Preset Library",
                 order     = 1)]
public class CityPresetLibrary : ScriptableObject
{
    [Serializable]
    public class CityPreset
    {
        public string name         = "Preset";
        public string description  = "";
        public string createdDate  = "";

        // Parámetros numéricos (los que se pueden serializar sin referencias Unity)
        public int    seed;
        public float  mainRoadWidth;
        public float  secondaryRoadWidth;
        public float  localRoadWidth;
        public int    bspDepth;
        public float  minBlockSize;
        public float  splitVariance;
        public float  sidewalkWidth;
        public float  sidewalkHeight;
        public float  parkRatio;
        public int    objectsPerFrame;
        public float  cullingDistance;

        // Estadísticas guardadas (para comparar resultados)
        public int    buildingsPlaced;
        public int    roadsGenerated;
        public float  genTimeMs;

        // Calificación del usuario (0-5 estrellas)
        [Range(0, 5)]
        public int rating = 3;

        // Notas
        [TextArea(2, 4)]
        public string notes = "";
    }

    [Header("Presets guardados")]
    public List<CityPreset> presets = new();

    // ══════════════════════════════════════════════════════════════════════
    //  API
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Guarda los parámetros numéricos de un CityData como preset nuevo.
    /// </summary>
    public void SavePreset(CityData data, string presetName,
                            int buildings = 0, int roads = 0, float genMs = 0f)
    {
        var p = new CityPreset
        {
            name              = presetName,
            createdDate       = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            seed              = data.seed,
            mainRoadWidth     = data.mainRoadWidth,
            secondaryRoadWidth= data.secondaryRoadWidth,
            localRoadWidth    = data.localRoadWidth,
            bspDepth          = data.bspDepth,
            minBlockSize      = data.minBlockSize,
            splitVariance     = data.splitVariance,
            sidewalkWidth     = data.sidewalkWidth,
            sidewalkHeight    = data.sidewalkHeight,
            parkRatio         = data.parkRatio,
            objectsPerFrame   = data.objectsPerFrame,
            cullingDistance   = data.cullingDistance,
            buildingsPlaced   = buildings,
            roadsGenerated    = roads,
            genTimeMs         = genMs
        };
        presets.Add(p);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log($"[CityPresetLibrary] Preset '{presetName}' guardado.");
    }

    /// <summary>
    /// Aplica los parámetros numéricos de un preset a un CityData.
    /// NOTA: No sobreescribe referencias de prefabs ni materiales.
    /// </summary>
    public void ApplyPreset(int index, CityData data)
    {
        if (index < 0 || index >= presets.Count) return;
        var p = presets[index];

        data.seed               = p.seed;
        data.mainRoadWidth      = p.mainRoadWidth;
        data.secondaryRoadWidth = p.secondaryRoadWidth;
        data.localRoadWidth     = p.localRoadWidth;
        data.bspDepth           = p.bspDepth;
        data.minBlockSize       = p.minBlockSize;
        data.splitVariance      = p.splitVariance;
        data.sidewalkWidth      = p.sidewalkWidth;
        data.sidewalkHeight     = p.sidewalkHeight;
        data.parkRatio          = p.parkRatio;
        data.objectsPerFrame    = p.objectsPerFrame;
        data.cullingDistance    = p.cullingDistance;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(data);
#endif
        Debug.Log($"[CityPresetLibrary] Preset '{p.name}' aplicado.");
    }

    /// <summary>
    /// Exportar todos los presets a JSON en StreamingAssets.
    /// </summary>
    public void ExportToJSON(string filename = "CityPresets.json")
    {
        string dir  = Application.streamingAssetsPath;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, filename);
        string json = JsonUtility.ToJson(new SerializableList<CityPreset>(presets), true);
        File.WriteAllText(path, json);
        Debug.Log($"[CityPresetLibrary] Exportado a {path}");
    }

    /// <summary>
    /// Importar presets desde JSON.
    /// </summary>
    public void ImportFromJSON(string filename = "CityPresets.json")
    {
        string path = Path.Combine(Application.streamingAssetsPath, filename);
        if (!File.Exists(path)) { Debug.LogWarning($"No encontrado: {path}"); return; }

        var loaded = JsonUtility.FromJson<SerializableList<CityPreset>>(File.ReadAllText(path));
        if (loaded?.items != null) presets.AddRange(loaded.items);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log($"[CityPresetLibrary] {loaded?.items?.Count ?? 0} presets importados.");
    }

    /// <summary>
    /// Ordena los presets por rating (mayor primero) para priorizar los mejores.
    /// </summary>
    public void SortByRating()
    {
        presets.Sort((a, b) => b.rating.CompareTo(a.rating));
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    // Wrapper para serialización de lista con JsonUtility
    [Serializable]
    private class SerializableList<T> { public List<T> items; public SerializableList(List<T> l) { items = l; } }
}