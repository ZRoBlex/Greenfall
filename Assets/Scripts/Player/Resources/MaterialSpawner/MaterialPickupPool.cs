using System.Collections.Generic;
using UnityEngine;

public class PickupPool : MonoBehaviour
{
    public static PickupPool Instance;

    [System.Serializable]
    public class PoolEntry
    {
        public string id;                 // "Wood", "Stone", "Metal", "WaterBottle"
        public GameObject prefab;
        public int prewarmCount = 20;
    }

    [Header("Pools")]
    public List<PoolEntry> pools = new List<PoolEntry>();

    // prefab -> queue
    Dictionary<GameObject, Queue<GameObject>> poolByPrefab =
        new Dictionary<GameObject, Queue<GameObject>>();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        PrewarmAll();
    }

    void PrewarmAll()
    {
        foreach (var entry in pools)
        {
            if (entry.prefab == null)
                continue;

            var queue = new Queue<GameObject>();
            poolByPrefab[entry.prefab] = queue;

            for (int i = 0; i < entry.prewarmCount; i++)
            {
                var obj = Instantiate(entry.prefab, transform);
                obj.SetActive(false);
                queue.Enqueue(obj);
            }
        }
    }

    // 🔹 pedir pickup de un prefab específico
    public GameObject Get(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (!poolByPrefab.TryGetValue(prefab, out var queue))
        {
            // crear pool nuevo en runtime si no existe
            queue = new Queue<GameObject>();
            poolByPrefab[prefab] = queue;
        }

        GameObject obj;

        if (queue.Count > 0)
        {
            obj = queue.Dequeue();
        }
        else
        {
            obj = Instantiate(prefab, transform);
        }

        obj.transform.SetPositionAndRotation(pos, rot);
        obj.SetActive(true);

        return obj;
    }

    // 🔹 devolver pickup al pool correcto
    public void Release(GameObject obj, GameObject prefabKey)
    {
        obj.SetActive(false);

        if (!poolByPrefab.TryGetValue(prefabKey, out var queue))
        {
            queue = new Queue<GameObject>();
            poolByPrefab[prefabKey] = queue;
        }

        queue.Enqueue(obj);
    }
}
