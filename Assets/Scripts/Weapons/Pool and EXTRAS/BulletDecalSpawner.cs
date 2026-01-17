using UnityEngine;

public class BulletDecalSpawner : MonoBehaviour
{
    public static BulletDecalSpawner Instance;

    [Header("Decal")]
    [SerializeField] GameObject decalPrefab;
    [SerializeField] float decalLifeTime = 20f;

    [Header("Do NOT stick to")]
    [SerializeField] string[] ignoreTags;
    [SerializeField] LayerMask ignoreLayers;

    void Awake()
    {
        Instance = this;
    }

    public void Spawn(RaycastHit hit)
    {
        if (!decalPrefab)
            return;

        // ❌ TAGS BLOQUEADOS
        foreach (string tag in ignoreTags)
        {
            if (!string.IsNullOrEmpty(tag) && hit.collider.CompareTag(tag))
                return;
        }

        // ❌ LAYERS BLOQUEADOS
        if ((ignoreLayers.value & (1 << hit.collider.gameObject.layer)) != 0)
            return;

        // ✅ SPAWN DECAL
        GameObject d = Instantiate(
            decalPrefab,
            hit.point + hit.normal * 0.001f,
            Quaternion.LookRotation(-hit.normal)
        );

        Destroy(d, decalLifeTime);
    }
}
