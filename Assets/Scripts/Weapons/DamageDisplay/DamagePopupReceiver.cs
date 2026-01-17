using UnityEngine;

public class DamagePopupReceiver : MonoBehaviour
{
    public DamagePopupSettings settings;
    public DamageDisplayType displayType;

    Health health;
    NonLethalHealth capture;
    DamageHitRelay relay;

    // 🔹 Estado del último golpe
    bool lastHitWasCritical;

    void Awake()
    {
        relay = GetComponent<DamageHitRelay>();

        health = GetComponent<Health>();
        if (health)
            health.OnDamageTaken += OnDamage;

        capture = GetComponent<NonLethalHealth>();
        if (capture)
            capture.OnCaptureTaken += OnDamage;
    }

    // ------------------------------------------------
    // ESTE MÉTODO LO LLAMA EL ARMA
    // ------------------------------------------------
    public void SetLastHitCritical(bool critical)
    {
        lastHitWasCritical = critical;
    }

    // ------------------------------------------------
    // EVENTO DE VIDA
    // ------------------------------------------------
    void OnDamage(float damage)
    {
        if (!settings || !settings.enabled)
            return;

        Spawn(damage, lastHitWasCritical);

        // 🔁 reset para el siguiente golpe
        lastHitWasCritical = false;
    }

    void Spawn(float value, bool crit)
    {
        Vector3 pos = relay != null && relay.LastHitPoint != Vector3.zero
            ? relay.LastHitPoint
            : transform.position + settings.worldOffset;

        DamageNumber num = DamageNumberPool.Instance.Get();
        num.Show(value, displayType, crit, settings, pos);
    }
}
