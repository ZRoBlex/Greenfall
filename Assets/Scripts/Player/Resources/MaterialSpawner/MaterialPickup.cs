using UnityEngine;

public class MaterialPickup : MonoBehaviour
{
    [Header("Pickup Data")]
    public string materialId;
    public int amount = 1;

    [HideInInspector]
    public GameObject prefabKey;   // 👈 quién me creó (para volver al pool correcto)

    [Header("Magnet Settings")]
    public float magnetSpeed = 12f;
    public float collectDistance = 0.6f;

    Transform target;
    bool magnetActive;
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        magnetActive = false;
        target = null;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
        }
    }

    void Update()
    {
        if (!magnetActive || target == null)
            return;

        Vector3 dir = (target.position - transform.position);
        float dist = dir.magnitude;

        if (dist <= collectDistance)
        {
            Collect();
            return;
        }

        dir.Normalize();
        transform.position += dir * magnetSpeed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        target = other.transform;
        magnetActive = true;

        if (rb != null)
            rb.isKinematic = true;
    }

    void Collect()
    {
        var stats = target.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.AddMaterials(materialId, amount);
        }

        PickupPool.Instance.Release(gameObject, prefabKey);
    }
}
