using System.Collections.Generic;
using UnityEngine;

public class WeaponGroundPool : MonoBehaviour
{
    public static WeaponGroundPool Instance;

    [SerializeField] Weapon weaponPrefab;
    [SerializeField] int initialAmount = 20;

    Queue<Weapon> pool = new Queue<Weapon>();

    void Awake()
    {
        Instance = this;

        for (int i = 0; i < initialAmount; i++)
            CreateWeapon();
    }

    void CreateWeapon()
    {
        Weapon w = Instantiate(weaponPrefab, transform);
        PrepareAsGround(w);
        w.gameObject.SetActive(false);
        pool.Enqueue(w);
    }

    public Weapon Spawn(WeaponStats stats, Vector3 position, Vector3 force)
    {
        Weapon w = pool.Count > 0 ? pool.Dequeue() : Instantiate(weaponPrefab);

        w.stats = stats;
        PrepareAsGround(w);

        w.transform.position = position;
        w.gameObject.SetActive(true);

        Rigidbody rb = w.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(force, ForceMode.Impulse);

        return w;
    }

    public void Release(Weapon w)
    {
        PrepareAsGround(w);
        w.gameObject.SetActive(false);
        pool.Enqueue(w);
    }

    void PrepareAsGround(Weapon w)
    {
        w.enabled = false;

        Rigidbody rb = w.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = false;

        Collider col = w.GetComponent<Collider>();
        if (col) col.enabled = true;
    }
}
