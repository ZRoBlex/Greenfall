using UnityEngine;

public class DamagePopupReceiver : MonoBehaviour
{
    [Header("Settings por tipo de daño")]
    public DamagePopupSettings[] healthSettings;
    public DamagePopupSettings[] captureSettings;
    // public DamagePopupSettings[] stunSettings; // futuro: añadir otros tipos

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
            health.OnDamageTaken += OnHealthDamage;

        capture = GetComponent<NonLethalHealth>();
        if (capture)
            capture.OnCaptureTaken += OnCaptureDamage;
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
    void OnHealthDamage(float damage)
    {
        if (healthSettings == null || healthSettings.Length == 0)
            return;

        foreach (var s in healthSettings)
        {
            if (s != null && s.enabled)
                Spawn(damage, lastHitWasCritical, s, DamageDisplayType.Health);
        }

        lastHitWasCritical = false;
    }

    // ------------------------------------------------
    // EVENTO DE CAPTURE / NONLETHAL
    // ------------------------------------------------
    void OnCaptureDamage(float damage)
    {
        if (captureSettings == null || captureSettings.Length == 0)
            return;

        foreach (var s in captureSettings)
        {
            if (s != null && s.enabled)
                Spawn(damage, lastHitWasCritical, s, DamageDisplayType.Capture);
        }

        lastHitWasCritical = false;
    }

    // ------------------------------------------------
    // GENERA EL POPUP
    // ------------------------------------------------
    void Spawn(float value, bool crit, DamagePopupSettings s, DamageDisplayType type)
    {
        Vector3 pos = relay != null && relay.LastHitPoint != Vector3.zero
            ? relay.LastHitPoint
            : transform.position + s.worldOffset;

        DamageNumber num = DamageNumberPool.Instance.Get();
        num.Show(value, type, crit, s, pos);
    }
}
