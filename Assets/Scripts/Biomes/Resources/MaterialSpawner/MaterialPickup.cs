using UnityEngine;

public class MaterialPickup : MonoBehaviour
{
    [Header("Pickup Data")]
    public string materialId;
    public int amount = 1;

    [HideInInspector]
    public GameObject prefabKey; // para volver al pool correcto

    [Header("Visual Stack Prefabs")]
    public GameObject smallStackPrefab;
    public GameObject mediumStackPrefab;
    public GameObject largeStackPrefab;

    [Header("Stack Thresholds")]
    public int mediumThreshold = 5;   // >=5 → medium
    public int largeThreshold = 15;   // >=15 → large

    [Header("Magnet Settings")]
    public float magnetSpeed = 12f;
    public float collectDistance = 0.6f;

    [Header("Stack Merge Settings")]
    public float mergeRadius = 1.2f;
    public int maxStackSize = 50;
    public float mergeCheckInterval = 0.25f;


    Transform target;
    bool magnetActive;
    Rigidbody rb;

    Transform visualRoot;
    GameObject currentVisual;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        visualRoot = new GameObject("VisualRoot").transform;
        visualRoot.SetParent(transform);
        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
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

        UpdateVisual();
    }

    // 🔁 Decide qué prefab visual usar según amount
    public void UpdateVisual()
    {
        if (currentVisual != null)
            Destroy(currentVisual);

        GameObject prefabToUse = smallStackPrefab;

        if (amount >= largeThreshold && largeStackPrefab != null)
            prefabToUse = largeStackPrefab;
        else if (amount >= mediumThreshold && mediumStackPrefab != null)
            prefabToUse = mediumStackPrefab;

        if (prefabToUse != null)
        {
            currentVisual = Instantiate(prefabToUse, visualRoot);
            currentVisual.transform.localPosition = Vector3.zero;
            currentVisual.transform.localRotation = Quaternion.identity;
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

    float mergeTimer;
    bool mergingLocked; // evita loops raros

    void LateUpdate()
    {
        mergeTimer -= Time.deltaTime;
        if (mergeTimer <= 0f)
        {
            mergeTimer = mergeCheckInterval;
            TryMergeNearby();
        }
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

    void TryMergeNearby()
    {
        if (mergingLocked)
            return;

        // no fusionar mientras está siendo atraído por el jugador
        if (magnetActive)
            return;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            mergeRadius
        );

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject)
                continue;

            var other = hit.GetComponentInParent<MaterialPickup>();
            if (other == null)
                continue;

            if (other.materialId != materialId)
                continue;

            if (other.mergingLocked)
                continue;

            // 🔢 cuánto espacio queda en ESTE stack
            int spaceLeft = maxStackSize - amount;
            if (spaceLeft <= 0)
                return;

            // 🔢 cuánto podemos tomar del otro
            int takeAmount = Mathf.Min(spaceLeft, other.amount);

            if (takeAmount <= 0)
                continue;

            // 🔒 bloquear ambos para evitar doble merge
            mergingLocked = true;
            other.mergingLocked = true;

            // ➕ transferir
            amount += takeAmount;
            other.amount -= takeAmount;

            UpdateVisual();

            // 🧹 si el otro quedó vacío → pool
            if (other.amount <= 0)
            {
                PickupPool.Instance.Release(
                    other.gameObject,
                    other.prefabKey
                );
            }
            else
            {
                other.UpdateVisual();
            }

            // 🔓 desbloquear
            mergingLocked = false;
            other.mergingLocked = false;

            // solo fusiona con UNO por tick (más estable)
            return;
        }
    }

}
