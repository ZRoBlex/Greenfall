using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ParticlePool : MonoBehaviour
{
    public static ParticlePool Instance;

    [System.Serializable]
    public class ParticleEntry
    {
        public ParticleSystem prefab;
        public int initialAmount = 10;
        public float lifetime = 2f;
    }

    [SerializeField] ParticleEntry[] particles;

    Dictionary<ParticleSystem, Queue<ParticleSystem>> pools =
        new Dictionary<ParticleSystem, Queue<ParticleSystem>>();

    Dictionary<ParticleSystem, float> lifetimes =
        new Dictionary<ParticleSystem, float>();

    void Awake()
    {
        Instance = this;

        foreach (var entry in particles)
        {
            Queue<ParticleSystem> q = new Queue<ParticleSystem>();

            for (int i = 0; i < entry.initialAmount; i++)
            {
                ParticleSystem ps = Instantiate(entry.prefab, transform);
                ps.gameObject.SetActive(false);
                q.Enqueue(ps);
            }

            pools.Add(entry.prefab, q);
            lifetimes.Add(entry.prefab, entry.lifetime);
        }
    }

    public void Spawn(ParticleSystem prefab, Vector3 pos, Quaternion rot)
    {
        if (!pools.ContainsKey(prefab))
        {
            pools[prefab] = new Queue<ParticleSystem>();
            lifetimes[prefab] = 2f;
        }

        ParticleSystem ps =
            pools[prefab].Count > 0
                ? pools[prefab].Dequeue()
                : Instantiate(prefab, transform);

        ps.transform.position = pos;
        ps.transform.rotation = rot;
        ps.gameObject.SetActive(true);
        ps.Play();

        StartCoroutine(ReturnAfter(prefab, ps, lifetimes[prefab]));
    }

    IEnumerator ReturnAfter(
        ParticleSystem prefab,
        ParticleSystem ps,
        float time
    )
    {
        yield return new WaitForSeconds(time);

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.gameObject.SetActive(false);
        pools[prefab].Enqueue(ps);
    }
}
