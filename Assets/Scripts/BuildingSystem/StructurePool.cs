using UnityEngine;
using System.Collections.Generic;

public class StructurePool : MonoBehaviour
{
    public static StructurePool Instance;

    Dictionary<GameObject, Queue<GameObject>> pool = new();

    void Awake()
    {
        Instance = this;
    }

    public GameObject Get(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (!pool.ContainsKey(prefab))
            pool[prefab] = new Queue<GameObject>();

        if (pool[prefab].Count > 0)
        {
            GameObject obj = pool[prefab].Dequeue();
            obj.transform.SetPositionAndRotation(pos, rot);
            obj.SetActive(true);
            return obj;
        }

        return Instantiate(prefab, pos, rot);
    }

    public void Release(GameObject obj)
    {
        obj.SetActive(false);
        GameObject prefab = obj.GetComponent<StructureInstance>().prefab;
        pool[prefab].Enqueue(obj);
    }
}
