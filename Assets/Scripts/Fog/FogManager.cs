using UnityEngine;

public class FogManager : MonoBehaviour
{
    public BiomeMap biomeMap;
    public Transform player;

    [Header("Transition")]
    public float transitionSpeed = 2f;

    [Header("Movement")]
    public float noiseSpeed = 0.2f;
    public float densityVariation = 0.003f;

    float noiseOffset;

    void Awake()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
    }

    void Update()
    {
        if (!player || !biomeMap) return;

        var biome = biomeMap.GetBiomeDefinition(player.position);
        if (biome == null) return;

        noiseOffset += Time.deltaTime * noiseSpeed;
        float noise = Mathf.PerlinNoise(noiseOffset, 0.5f);

        float baseDensity = biome.fogDensity;
        float dynamicDensity =
            baseDensity + (noise - 0.5f) * densityVariation;

        float zombieBoost = CalculateZombieFog();

        RenderSettings.fogDensity = Mathf.Lerp(
            RenderSettings.fogDensity,
            dynamicDensity + zombieBoost,
            Time.deltaTime * transitionSpeed
        );

        RenderSettings.fogColor = Color.Lerp(
            RenderSettings.fogColor,
            biome.fogColor,
            Time.deltaTime * transitionSpeed
        );
    }

    float CalculateZombieFog()
    {
        float boost = 0f;

        Collider[] hits = Physics.OverlapSphere(
            player.position,
            25f
        );

        foreach (var hit in hits)
        {
            ZombieFogInfluence z =
                hit.GetComponentInParent<ZombieFogInfluence>();

            if (!z) continue;

            float d = Vector3.Distance(
                player.position,
                hit.transform.position
            );

            float f = 1f - (d / z.influenceRadius);
            boost += z.densityBoost * Mathf.Clamp01(f);
        }

        return boost;
    }
}
