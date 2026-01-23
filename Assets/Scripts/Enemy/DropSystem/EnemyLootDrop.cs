using UnityEngine;
using System.Collections.Generic;

public class EnemyLootDrop : MonoBehaviour
{
    [Header("Weapon Loot (1 max)")]
    public List<LootItem> weaponLoot = new List<LootItem>();

    [Header("Ammo Loot (1 type max)")]
    public List<LootItem> ammoLoot = new List<LootItem>();

    [Header("Extra Loot (multiple allowed)")]
    public List<LootItem> extraLoot = new List<LootItem>();

    [Header("Drop Settings")]
    public Transform dropPoint;

    public Vector3 baseOffset = new Vector3(0, 0.5f, 0);
    public float randomRadius = 0.6f;
    public float upwardForce = 2f;
    public float scatterForce = 1.5f;
    public float torqueForce = 6f;

    bool hasDropped;

    void Awake()
    {
        if (dropPoint == null)
            dropPoint = transform;
    }

    void OnEnable()
    {
        hasDropped = false;
    }

    public void DropLoot()
    {
        if (hasDropped)
            return;

        hasDropped = true;

        // 🔫 UNA SOLA ARMA
        TryDropSingleFromList(weaponLoot);

        // 🔸 UN SOLO TIPO DE AMMO
        TryDropSingleFromList(ammoLoot);

        // 🎁 EXTRAS (pueden ser varios)
        TryDropMultipleFromList(extraLoot);
    }

    // -----------------------
    // DROP HELPERS
    // -----------------------

    void TryDropSingleFromList(List<LootItem> list)
    {
        foreach (var item in list)
        {
            if (item.prefab == null)
                continue;

            float roll = Random.Range(0f, 100f);
            if (roll > item.dropChance)
                continue;

            int amount = Mathf.Max(1, Random.Range(item.minAmount, item.maxAmount + 1));

            for (int i = 0; i < amount; i++)
                SpawnLoot(item.prefab);

            break; // 👈 CLAVE: solo uno
        }
    }

    void TryDropMultipleFromList(List<LootItem> list)
    {
        foreach (var item in list)
        {
            if (item.prefab == null)
                continue;

            float roll = Random.Range(0f, 100f);
            if (roll > item.dropChance)
                continue;

            int amount = Mathf.Max(1, Random.Range(item.minAmount, item.maxAmount + 1));

            for (int i = 0; i < amount; i++)
                SpawnLoot(item.prefab);
        }
    }

    // -----------------------
    // SPAWN
    // -----------------------

    void SpawnLoot(GameObject prefab)
    {
        Vector3 randomOffset = Random.insideUnitSphere * randomRadius;
        randomOffset.y = 0f;

        Vector3 spawnPos =
            dropPoint.position +
            baseOffset +
            randomOffset;

        GameObject loot = Instantiate(prefab, spawnPos, Quaternion.identity);

        Rigidbody rb = loot.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.rotation = Random.rotation;

            Vector3 forceDir =
                Vector3.up * upwardForce +
                Random.insideUnitSphere * scatterForce;

            rb.AddForce(forceDir, ForceMode.Impulse);

            Vector3 torque = Random.insideUnitSphere * torqueForce;
            rb.AddTorque(torque, ForceMode.Impulse);
        }
    }
}
