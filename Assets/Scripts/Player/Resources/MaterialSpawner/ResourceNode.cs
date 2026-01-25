using UnityEngine;
using System.Collections;

public class ResourceNode : MonoBehaviour
{
    public ResourceData data;

    public float currentHealth;
    bool depleted;
    public int minAmountMat;
    public int maxAmountMat;
    //public int ammount = Random.Range(3,7);
    public int ammount;
    void Awake()
    {
        ammount = Random.Range(minAmountMat,maxAmountMat);
        ResetNode();
    }

    public void ResetNode()
    {
        depleted = false;
        currentHealth = data.maxHealth;
        gameObject.SetActive(true);
    }

    public void Damage(float amount)
    {
        if (depleted)
            return;

        currentHealth -= amount;

        if (currentHealth <= 0f)
            Deplete();
    }

    void Deplete()
    {
        depleted = true;

        SpawnDrops();

        if (data.canRespawn)
            StartCoroutine(RespawnRoutine());
        else
            Destroy(gameObject);
    }

    void SpawnDrops()
    {
        foreach (var drop in data.drops)
        {
            // 🎲 Tirada de probabilidad
            if (Random.value > drop.dropChance)
                continue;

            int count = Random.Range(drop.minAmount, drop.maxAmount + 1);

            for (int i = 0; i < count; i++)
            {
                Vector3 offset = Random.insideUnitSphere * 0.6f;
                offset.y = Mathf.Abs(offset.y);

                var obj = PickupPool.Instance.Get(
                    drop.pickupPrefab,
                    transform.position + offset,
                    Quaternion.identity
                );

                var pickup = obj.GetComponent<MaterialPickup>();
                pickup.materialId = drop.materialId;

                // cantidad TOTAL que representa este pickup
                int amount = Random.Range(drop.minAmount, drop.maxAmount + 1);
                pickup.amount = ammount;

                pickup.prefabKey = drop.pickupPrefab;

                // 🔁 actualizar visual según amount
                pickup.UpdateVisual();


                var rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;

                    Vector3 force = new Vector3(
                        Random.Range(-1f, 1f),
                        Random.Range(2f, 4f),
                        Random.Range(-1f, 1f)
                    );

                    rb.AddForce(force, ForceMode.Impulse);
                }
            }
        }
    }




    IEnumerator RespawnRoutine()
    {
        gameObject.SetActive(false);
        yield return new WaitForSeconds(data.respawnTime);
        ResetNode();
    }
}
