using UnityEngine;
using System.Collections.Generic;

public class BulletDecalPool : MonoBehaviour
{
    public static BulletDecalPool Instance;

    [Header("Decal")]
    [SerializeField] BulletDecal decalPrefab;
    [SerializeField] int initialSize = 30;

    [Header("Lifetime")]
    [SerializeField] float decalLifetime = 15f;
    [SerializeField] float fadeDuration = 2f;

    [Header("Forbidden Tags")]
    [SerializeField] string[] forbiddenTags;

    Queue<BulletDecal> pool = new Queue<BulletDecal>();

    void Awake()
    {
        Instance = this;

        for (int i = 0; i < initialSize; i++)
            CreateDecal();
    }

    void CreateDecal()
    {
        BulletDecal d = Instantiate(decalPrefab, transform);
        d.gameObject.SetActive(false);
        pool.Enqueue(d);
    }

    public void Spawn(RaycastHit hit)
    {
        if (!CanStick(hit.collider))
            return;

        if (pool.Count == 0)
            CreateDecal();

        BulletDecal decal = pool.Dequeue();
        decal.gameObject.SetActive(true);

        decal.transform.position = hit.point + hit.normal * 0.001f;
        decal.transform.rotation = Quaternion.LookRotation(-hit.normal);

        decal.Init(decalLifetime, fadeDuration);
    }

    public void Release(BulletDecal decal)
    {
        decal.ResetDecal();
        decal.gameObject.SetActive(false);
        pool.Enqueue(decal);
    }

    bool CanStick(Collider col)
    {
        if (forbiddenTags == null || forbiddenTags.Length == 0)
            return true;

        foreach (string tag in forbiddenTags)
        {
            if (!string.IsNullOrEmpty(tag) && col.CompareTag(tag))
                return false;
        }

        return true;
    }
}
