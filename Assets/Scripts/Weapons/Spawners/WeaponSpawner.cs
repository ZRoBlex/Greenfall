using UnityEngine;
using System.Collections.Generic;

public class WeaponSpawner : MonoBehaviour
{
    [Header("Spawn Mode")]
    [SerializeField] bool spawnOnStart = true;
    [SerializeField] bool useAreaSpawn = false;

    [Header("Spawn Area")]
    [SerializeField] Vector3 areaSize = new Vector3(1f, 0f, 1f);

    [Header("Weapon Pool")]
    [SerializeField] List<WeaponSpawnEntry> weapons = new();

    [Header("Spawn Settings")]
    [SerializeField] float groundCheckDistance = 2f;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] Vector3 spawnOffset = Vector3.up * 0.05f;

    Weapon spawnedWeapon;

    // =========================
    void Start()
    {
        if (spawnOnStart)
            SpawnWeapon();
    }

    // =========================
    public void SpawnWeapon()
    {
        if (spawnedWeapon != null) return;
        if (weapons.Count == 0) return;

        Weapon prefab = GetWeaponByProbability();
        if (!prefab) return;

        Vector3 spawnPos = GetSpawnPosition();

        spawnedWeapon = Instantiate(
            prefab,
            spawnPos,
            Quaternion.identity
        );
    }

    // =========================
    Weapon GetWeaponByProbability()
    {
        float total = 0f;

        foreach (var w in weapons)
            total += Mathf.Max(0f, w.probability);

        float rand = Random.Range(0f, total);
        float current = 0f;

        foreach (var w in weapons)
        {
            current += Mathf.Max(0f, w.probability);
            if (rand <= current)
                return w.weaponPrefab;
        }

        return weapons[0].weaponPrefab;
    }

    // =========================
    Vector3 GetSpawnPosition()
    {
        Vector3 basePos = transform.position;

        if (useAreaSpawn)
        {
            Vector3 half = areaSize * 0.5f;
            basePos += new Vector3(
                Random.Range(-half.x, half.x),
                0f,
                Random.Range(-half.z, half.z)
            );
        }

        // 🧠 Ground check
        if (Physics.Raycast(
            basePos + Vector3.up,
            Vector3.down,
            out RaycastHit hit,
            groundCheckDistance,
            groundLayer
        ))
        {
            return hit.point + spawnOffset;
        }

        return basePos;
    }

    // =========================
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        if (useAreaSpawn)
            Gizmos.DrawWireCube(
                transform.position,
                new Vector3(areaSize.x, 0.01f, areaSize.z)
            );
        else
            Gizmos.DrawSphere(transform.position, 0.15f);
    }
#endif
}
