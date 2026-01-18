using UnityEngine;
using System.Collections.Generic;

public class WeaponPoolManager : MonoBehaviour
{
    public static WeaponPoolManager Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] int initialPoolSize = 3;
    [SerializeField] List<WeaponSpawnEntry> weapons;


    Dictionary<Weapon, Queue<Weapon>> pool = new();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // =========================
    public Weapon GetWeapon(Weapon prefab)
    {
        if (!pool.ContainsKey(prefab))
            pool[prefab] = new Queue<Weapon>();

        if (pool[prefab].Count == 0)
            CreateWeapon(prefab);

        Weapon weapon = pool[prefab].Dequeue();

        weapon.gameObject.SetActive(true);
        weapon.transform.SetParent(null);

        ResetWeaponState(weapon);

        return weapon;
    }

    // =========================
    public void ReturnWeapon(Weapon weapon, Weapon prefab)
    {
        weapon.gameObject.SetActive(false);
        weapon.transform.SetParent(transform);

        ResetWeaponState(weapon);

        pool[prefab].Enqueue(weapon);
    }

    // =========================
    void CreateWeapon(Weapon prefab)
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            Weapon w = Instantiate(prefab, transform);
            w.gameObject.SetActive(false);

            pool[prefab].Enqueue(w);
        }
    }

    // =========================
    void ResetWeaponState(Weapon weapon)
    {
        if (weapon.TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
        }

        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
    }
}
