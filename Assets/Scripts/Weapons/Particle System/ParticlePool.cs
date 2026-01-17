using UnityEngine;
using System.Collections.Generic;

public class ParticlePool : MonoBehaviour
{
    public static ParticlePool Instance;

    [SerializeField] int initialSize = 20;

    Dictionary<ParticleSystem, Queue<ParticleSystem>> pools =
        new Dictionary<ParticleSystem, Queue<ParticleSystem>>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public ParticleSystem Get(ParticleSystem prefab)
    {
        if (!prefab) return null;

        if (!pools.ContainsKey(prefab))
            pools[prefab] = new Queue<ParticleSystem>();

        if (pools[prefab].Count > 0)
        {
            ParticleSystem ps = pools[prefab].Dequeue();
            ps.gameObject.SetActive(true);
            return ps;
        }

        ParticleSystem newPs = Instantiate(prefab);
        return newPs;
    }

    public void Release(ParticleSystem prefab, ParticleSystem instance)
    {
        instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        instance.gameObject.SetActive(false);

        if (!pools.ContainsKey(prefab))
            pools[prefab] = new Queue<ParticleSystem>();

        pools[prefab].Enqueue(instance);
    }


}
