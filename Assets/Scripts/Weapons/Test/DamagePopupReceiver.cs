using UnityEngine;

public class DamagePopupReceiver : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject healthDamagePrefab;
    public GameObject captureDamagePrefab;

    [Header("Popup Offset")]
    public Vector3 worldOffset = new Vector3(0, 1.5f, 0);

    Health health;
    NonLethalHealth nonLethal;
    DamageHitRelay relay;

    void Awake()
    {
        relay = GetComponent<DamageHitRelay>();

        health = GetComponent<Health>();
        if (health != null)
            health.OnDamageTaken += OnHealthDamage;

        nonLethal = GetComponent<NonLethalHealth>();
        if (nonLethal != null)
            nonLethal.OnCaptureTaken += OnCaptureDamage;
    }

    void OnDestroy()
    {
        if (health != null)
            health.OnDamageTaken -= OnHealthDamage;

        if (nonLethal != null)
            nonLethal.OnCaptureTaken -= OnCaptureDamage;
    }

    void OnHealthDamage(float amount)
    {
        SpawnPopup(amount, healthDamagePrefab);
    }

    void OnCaptureDamage(float amount)
    {
        SpawnPopup(amount, captureDamagePrefab);
    }

    void SpawnPopup(float value, GameObject prefab)
    {
        if (prefab == null) return;

        Vector3 basePos =
            relay != null && relay.LastHitPoint != Vector3.zero
            ? relay.LastHitPoint
            : transform.position;

        Vector3 finalPos = basePos + worldOffset;

        var go = Instantiate(prefab, finalPos, Quaternion.identity);
        go.GetComponent<DamageNumber>().SetValue(value);
    }
}
