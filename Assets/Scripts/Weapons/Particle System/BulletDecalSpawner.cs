using UnityEngine;

public class BulletDecalSpawner : MonoBehaviour
{
    public static BulletDecalSpawner Instance;

    [SerializeField] GameObject decalPrefab;
    [SerializeField] float decalLifeTime = 20f;

    void Awake()
    {
        Instance = this;
    }

    public void SpawnDecal(RaycastHit hit)
    {
        if (!decalPrefab) return;

        GameObject d = Instantiate(
            decalPrefab,
            hit.point + hit.normal * 0.001f,
            Quaternion.LookRotation(-hit.normal)
        );

        Destroy(d, decalLifeTime);
    }
}
