using UnityEngine;

public class BiomeMapRenderer : MonoBehaviour
{
    [Header("References")]
    public BiomeMap biomeMap;

    [Header("Map Size (World Units)")]
    public float worldWidth = 500f;
    public float worldHeight = 500f;

    [Header("Resolution")]
    public int textureResolution = 256;

    [Header("UI")]
    public bool showMiniMap = true;
    public bool expanded = false;

    public Rect miniMapRect = new Rect(20, 20, 200, 200);
    public Rect expandedRect = new Rect(20, 20, 500, 500);

    Texture2D biomeTexture;

    void Start()
    {
        GenerateTexture();
    }

    void GenerateTexture()
    {
        if (biomeMap == null || biomeMap.settings == null)
            return;

        biomeTexture = new Texture2D(textureResolution, textureResolution);
        biomeTexture.filterMode = FilterMode.Point;

        for (int x = 0; x < textureResolution; x++)
        {
            for (int y = 0; y < textureResolution; y++)
            {
                float wx = Mathf.Lerp(-worldWidth * 0.5f, worldWidth * 0.5f, (float)x / textureResolution);
                float wz = Mathf.Lerp(-worldHeight * 0.5f, worldHeight * 0.5f, (float)y / textureResolution);

                Vector3 worldPos = biomeMap.transform.position + new Vector3(wx, 0f, wz);

                var biome = biomeMap.GetBiome(worldPos);
                Color col = GetBiomeColor(biome);

                biomeTexture.SetPixel(x, y, col);
            }
        }

        biomeTexture.Apply();
    }

    void OnGUI()
    {
        if (!showMiniMap || biomeTexture == null)
            return;

        Rect r = expanded ? expandedRect : miniMapRect;

        GUI.DrawTexture(r, biomeTexture);

        if (GUI.Button(new Rect(r.x, r.yMax + 5, 120, 25),
            expanded ? "Close Map" : "Open Map"))
        {
            expanded = !expanded;
        }
    }

    Color GetBiomeColor(BiomeType biome)
    {
        switch (biome)
        {
            case BiomeType.Desert: return new Color(1f, 0.9f, 0.4f);
            case BiomeType.Plains: return new Color(0.4f, 0.8f, 0.3f);
            case BiomeType.Forest: return new Color(0.1f, 0.5f, 0.15f);
            case BiomeType.Mountains: return Color.gray;
            case BiomeType.Snow: return Color.white;
            default: return Color.magenta;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying)
            GenerateTexture();
    }
#endif
}
