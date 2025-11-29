using UnityEngine;

public class BulletParticleDamage : MonoBehaviour
{
    bool lethal;
    float lethalDamage;
    float nonLethalTick;
    float stunDuration;

    public void Configure(bool lethal, float ldmg, float ndmg, float stun)
    {
        this.lethal = lethal;
        lethalDamage = ldmg;
        nonLethalTick = ndmg;
        stunDuration = stun;
    }

    void OnParticleCollision(GameObject other)
    {
        var health = other.GetComponent<Health>();
        var nlh = other.GetComponent<NonLethalHealth>();

        if (lethal && health != null)
            health.ApplyDamage(lethalDamage);

        if (!lethal && nlh != null)
        {
            nlh.ApplyCaptureTick(nonLethalTick);
            // si quieres agregar stun directo:
            // nlh.Stun(stunDuration);
        }
    }
}
