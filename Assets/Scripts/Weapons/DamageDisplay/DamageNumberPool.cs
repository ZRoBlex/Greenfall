using UnityEngine;
using System.Collections.Generic;

public class DamageNumberPool : MonoBehaviour
{
    public static DamageNumberPool Instance;

    public DamageNumber prefab;
    public int preload = 20;

    Queue<DamageNumber> pool = new();

    void Awake()
    {
        Instance = this;

        for (int i = 0; i < preload; i++)
            Create();
    }

    void Create()
    {
        var n = Instantiate(prefab, transform);
        n.gameObject.SetActive(false);
        pool.Enqueue(n);
    }

    public DamageNumber Get()
    {
        if (pool.Count == 0)
            Create();

        return pool.Dequeue();
    }

    public void Release(DamageNumber n)
    {
        n.gameObject.SetActive(false);
        pool.Enqueue(n);
    }
}
