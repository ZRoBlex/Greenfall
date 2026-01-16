using UnityEngine;

public class DamagePopupReceiver : MonoBehaviour
{
    public GameObject damageNumberPrefab;

    Health health;
    DamageHitRelay relay;

    void Awake()
    {
        health = GetComponent<Health>();
        relay = GetComponent<DamageHitRelay>();

        health.OnDamageTaken += OnDamage;
    }

    void OnDamage(float amount)
    {
        Vector3 pos = relay != null
            ? relay.LastHitPoint
            : transform.position + Vector3.up * 1.2f;

        var go = Instantiate(damageNumberPrefab, pos, Quaternion.identity);
        go.GetComponent<DamageNumber>().SetValue(amount);
    }
}
