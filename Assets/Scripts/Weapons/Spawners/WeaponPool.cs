using UnityEngine;
using System.Collections.Generic;

public class WeaponPool : MonoBehaviour
{
    public static WeaponPool Instance { get; private set; }

    [Header("Initial Pool Size")]
    [SerializeField] int initialPerWeapon = 3;

    Dictionary<Weapon, Queue<Weapon>> pool = new();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // =========================
    public Weapon GetWeapon(Weapon prefab, Transform parent)
    {
        if (!pool.ContainsKey(prefab))
            CreatePool(prefab);

        Weapon weapon;

        if (pool[prefab].Count > 0)
        {
            weapon = pool[prefab].Dequeue();
        }
        else
        {
            weapon = Instantiate(prefab);
        }

        PrepareWeaponForSpawn(weapon, parent);
        return weapon;
    }

    // =========================
    public void ReturnWeapon(Weapon weapon, Weapon prefab)
    {
        weapon.gameObject.SetActive(false);
        weapon.transform.SetParent(transform);

        if (!pool.ContainsKey(prefab))
            pool[prefab] = new Queue<Weapon>();

        pool[prefab].Enqueue(weapon);
    }

    // =========================
    void CreatePool(Weapon prefab)
    {
        pool[prefab] = new Queue<Weapon>();

        for (int i = 0; i < initialPerWeapon; i++)
        {
            Weapon w = Instantiate(prefab, transform);
            w.gameObject.SetActive(false);
            pool[prefab].Enqueue(w);
        }
    }

    // =========================
    void PrepareWeaponForSpawn(Weapon weapon, Transform parent)
    {
        weapon.transform.SetParent(parent);
        weapon.gameObject.SetActive(true);

        if (weapon.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
