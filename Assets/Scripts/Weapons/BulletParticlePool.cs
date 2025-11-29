using System.Collections.Generic;
using UnityEngine;

public class BulletParticlePool : MonoBehaviour
{
    public static BulletParticlePool Instance;

    Dictionary<GameObject, Queue<ParticleSystem>> pools = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public ParticleSystem Get(GameObject prefab)
    {
        if (!pools.ContainsKey(prefab))
            pools[prefab] = new Queue<ParticleSystem>();

        if (pools[prefab].Count > 0)
        {
            var ps = pools[prefab].Dequeue();
            ps.gameObject.SetActive(true);
            return ps;
        }

        var go = Instantiate(prefab);
        return go.GetComponent<ParticleSystem>();
    }

    public void Return(GameObject prefab, ParticleSystem ps)
    {
        ps.gameObject.SetActive(false);
        pools[prefab].Enqueue(ps);
    }
}
