using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WeaponSpawner : MonoBehaviour
{
    [Header("Spawn Mode")]
    [SerializeField] bool spawnOnStart = true;
    [SerializeField] bool useAreaSpawn = false;

    [Header("Spawn Area")]
    [SerializeField] Vector3 areaSize = new Vector3(1.5f, 0f, 1.5f);

    [Header("Weapon Pool")]
    [SerializeField] List<WeaponSpawnEntry> weapons = new();

    [Header("Respawn Settings")]
    [SerializeField] float respawnDelay = 3f;
    [SerializeField] float despawnTimeIfNotPicked = 60f;

    [Header("Ground Placement")]
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float groundCheckDistance = 3f;
    [SerializeField] Vector3 spawnOffset = Vector3.up * 0.05f;

    [Header("Rotation")]
    [SerializeField] bool randomRotation = true;
    [SerializeField] Vector3 minRotation = new Vector3(0, 0, 0);
    [SerializeField] Vector3 maxRotation = new Vector3(0, 360, 0);

    [Header("Physics Rotation")]
    [SerializeField] float randomAngularForce = 5f;

    [Header("Continuous Spawn")]
    [SerializeField] bool forceRespawnEvenIfPicked = false;

    // =========================
    Weapon spawnedWeapon;
    Weapon spawnedPrefab;

    Coroutine respawnRoutine;
    Coroutine despawnRoutine;

    bool isRespawning;

    // =========================
    void Start()
    {
        if (spawnOnStart)
            SpawnWeapon();
    }

    // =========================
    void Update()
    {
        // 🧠 Si el arma fue tomada por el jugador
        if (spawnedWeapon != null && spawnedWeapon.transform.parent != transform)
        {
            spawnedWeapon = null;
            spawnedPrefab = null;
        }

        // 🔁 MODO NORMAL
        if (!forceRespawnEvenIfPicked)
        {
            if (spawnedWeapon == null && !isRespawning)
                respawnRoutine = StartCoroutine(RespawnRoutine());

            return;
        }

        // 🔥 MODO FORZADO
        if (!isRespawning)
            respawnRoutine = StartCoroutine(RespawnRoutine());
    }

    // =========================
    void SpawnWeapon()
    {
        if (weapons.Count == 0) return;
        if (!forceRespawnEvenIfPicked && spawnedWeapon != null) return;

        spawnedPrefab = GetWeaponByProbability();
        if (!spawnedPrefab) return;

        Vector3 pos = GetSpawnPosition();

        spawnedWeapon = WeaponPoolManager.Instance.GetWeapon(spawnedPrefab);

        spawnedWeapon.transform.SetParent(transform);
        spawnedWeapon.transform.position = pos;
        spawnedWeapon.transform.rotation = Quaternion.identity;

        ApplySpawnPhysics(spawnedWeapon);

        if (despawnRoutine != null)
            StopCoroutine(despawnRoutine);

        despawnRoutine = StartCoroutine(DespawnIfNotPicked());
    }

    // =========================
    IEnumerator RespawnRoutine()
    {
        isRespawning = true;
        yield return new WaitForSeconds(respawnDelay);

        // ❌ Eliminar solo si sigue siendo hija del spawner
        if (
            spawnedWeapon != null &&
            spawnedWeapon.transform.parent == transform
        )
        {
            WeaponPoolManager.Instance.ReturnWeapon(
                spawnedWeapon,
                spawnedPrefab
            );

            spawnedWeapon = null;
            spawnedPrefab = null;
        }

        SpawnWeapon();
        isRespawning = false;
    }

    // =========================
    IEnumerator DespawnIfNotPicked()
    {
        yield return new WaitForSeconds(despawnTimeIfNotPicked);

        if (
            spawnedWeapon != null &&
            spawnedWeapon.transform.parent == transform
        )
        {
            WeaponPoolManager.Instance.ReturnWeapon(
                spawnedWeapon,
                spawnedPrefab
            );

            spawnedWeapon = null;
            spawnedPrefab = null;
        }
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
        Vector3 pos = transform.position;

        if (useAreaSpawn)
        {
            Vector3 half = areaSize * 0.5f;
            pos += new Vector3(
                Random.Range(-half.x, half.x),
                0f,
                Random.Range(-half.z, half.z)
            );
        }

        if (Physics.Raycast(
            pos + Vector3.up,
            Vector3.down,
            out RaycastHit hit,
            groundCheckDistance,
            groundLayer
        ))
        {
            return hit.point + spawnOffset;
        }

        return pos;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        if (useAreaSpawn)
            Gizmos.DrawWireCube(
                transform.position,
                new Vector3(areaSize.x, 0.05f, areaSize.z)
            );
        else
            Gizmos.DrawSphere(transform.position, 0.2f);
    }
#endif

    // =========================
    void ApplySpawnPhysics(Weapon weapon)
    {
        if (!weapon.TryGetComponent(out Rigidbody rb))
            return;

        rb.isKinematic = false;

        Vector3 randomTorque = new Vector3(
            Random.Range(-randomAngularForce, randomAngularForce),
            Random.Range(-randomAngularForce, randomAngularForce),
            Random.Range(-randomAngularForce, randomAngularForce)
        );

        rb.AddTorque(randomTorque, ForceMode.Impulse);
    }
}
