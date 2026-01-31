using UnityEngine;

public class NPCDropper : MonoBehaviour
{
    [System.Serializable]
    public class DropItem
    {
        public GameObject prefab;
        [Range(0f, 1f)] public float chance = 0.5f;
        public int minAmount = 1;
        public int maxAmount = 1;
    }

    public DropItem[] drops;

    public void Drop()
    {
        foreach (var item in drops)
        {
            if (Random.value <= item.chance)
            {
                int amount = Random.Range(item.minAmount, item.maxAmount + 1);

                for (int i = 0; i < amount; i++)
                {
                    Vector3 offset = Random.insideUnitSphere * 0.5f;
                    offset.y = 0f;

                    Instantiate(item.prefab, transform.position + offset, Quaternion.identity);
                }
            }
        }
    }
}
