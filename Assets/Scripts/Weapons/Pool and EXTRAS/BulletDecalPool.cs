using UnityEngine;
using System.Collections.Generic;

public class BulletDecalPool : MonoBehaviour
{
    public static BulletDecalPool Instance;

    [System.Serializable]
    public class DecalEntry
    {
        public string name;
        public BulletDecal prefab;

        [Tooltip("Si está vacío, se pega a todo")]
        public string[] allowedTags;

        [Tooltip("Nunca se pega a estos tags")]
        public string[] ignoreTags;

        public int initialAmount = 20;
        public float lifeTime = 15f;
        public float fadeTime = 3f;

        [HideInInspector] public Queue<BulletDecal> pool;
    }


    [Header("Decal Types")]
    [SerializeField] DecalEntry[] decals;

    [Header("Fallback")]
    [SerializeField] bool useDefaultIfNoTagMatch = true;
    [SerializeField] int defaultIndex = 0;

    [SerializeField] string[] globalIgnoreTags;


    void Awake()
    {
        Instance = this;

        foreach (var entry in decals)
        {
            entry.pool = new Queue<BulletDecal>();

            for (int i = 0; i < entry.initialAmount; i++)
            {
                BulletDecal d = Instantiate(entry.prefab, transform);
                d.gameObject.SetActive(false);
                entry.pool.Enqueue(d);
            }
        }
    }

    // ----------------------------------------------------
    // SPAWN
    // ----------------------------------------------------
    public void Spawn(RaycastHit hit)
    {
        // ❌ IGNORE GLOBAL
        foreach (string tag in globalIgnoreTags)
        {
            if (!string.IsNullOrEmpty(tag) && hit.collider.CompareTag(tag))
                return;
        }

        DecalEntry entry = GetDecalForHit(hit);
        if (entry == null)
            return;

        BulletDecal decal =
            entry.pool.Count > 0
                ? entry.pool.Dequeue()
                : Instantiate(entry.prefab, transform);

        decal.Activate(hit, entry.lifeTime, entry.fadeTime);
    }


    // ----------------------------------------------------
    // RELEASE
    // ----------------------------------------------------
    public void Release(BulletDecal decal)
    {
        decal.transform.SetParent(transform);
        decal.gameObject.SetActive(false);

        foreach (var entry in decals)
        {
            if (entry.prefab == decal.PrefabReference)
            {
                entry.pool.Enqueue(decal);
                return;
            }
        }

        Destroy(decal.gameObject); // seguridad
    }

    // ----------------------------------------------------
    // TAG MATCHING
    // ----------------------------------------------------
    DecalEntry GetDecalForHit(RaycastHit hit)
    {
        string hitTag = hit.collider.tag;

        foreach (var entry in decals)
        {
            // ✅ SIN allowed tags = default
            if (entry.allowedTags == null || entry.allowedTags.Length == 0)
                return entry;

            foreach (string tag in entry.allowedTags)
            {
                if (!string.IsNullOrEmpty(tag) && hitTag == tag)
                    return entry;
            }
        }

        return null;
    }


}
