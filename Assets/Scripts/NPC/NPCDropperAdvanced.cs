using UnityEngine;
using System.Collections.Generic;

public class NPCDropperAdvanced : MonoBehaviour
{
    public enum Rarity { Common, Rare, Epic, Legendary }

    [System.Serializable]
    public class DropItem
    {
        public GameObject prefab;
        public Rarity rarity;
        [Range(0f, 1f)] public float chance = 0.5f;
        public int minAmount = 1;
        public int maxAmount = 1;
    }

    public DropItem[] drops;

    Dictionary<GameObject, Queue<GameObject>> pool = new Dictionary<GameObject, Queue<GameObject>>();

    void Awake()
    {
        foreach (var d in drops)
        {
            if (!pool.ContainsKey(d.prefab))
                pool[d.prefab] = new Queue<GameObject>();
        }
    }

    public void Drop()
    {
        foreach (var item in drops)
        {
            if (Random.value > item.chance)
                continue;

            int amount = Random.Range(item.minAmount, item.maxAmount + 1);

            for (int i = 0; i < amount; i++)
            {
                GameObject obj = GetFromPool(item.prefab);

                Vector3 offset = Random.insideUnitSphere * 0.7f;
                offset.y = 0;

                obj.transform.position = transform.position + offset;
                obj.SetActive(true);
            }
        }
    }

    GameObject GetFromPool(GameObject prefab)
    {
        if (pool[prefab].Count > 0)
            return pool[prefab].Dequeue();

        return Instantiate(prefab);
    }

    public void ReturnToPool(GameObject obj, GameObject prefab)
    {
        obj.SetActive(false);
        pool[prefab].Enqueue(obj);
    }
}
