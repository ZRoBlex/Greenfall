using UnityEngine;

public class ParticleDamageDealer : MonoBehaviour
{
    public float captureTick = 10f;     // lo que suma al medidor no letal
    public float extraStunTick = 0f;    // si quieres stun, suma más tick
    public bool isLethal = false;
    public float lethalDamage = 0f;

    bool alreadyHit = false;
    ParticleShrinkOnHit shrinker;

    void Awake()
    {
        shrinker = GetComponent<ParticleShrinkOnHit>();
    }

    void OnParticleCollision(GameObject other)
    {
        if (alreadyHit) return;
        alreadyHit = true;

        // -----------------------------
        // LETAL DAMAGE
        // -----------------------------
        if (isLethal)
        {
            var hp = other.GetComponentInParent<Health>();
            if (hp != null)
            {
                hp.ApplyDamage(lethalDamage);
            }
        }
        else
        {
            // -----------------------------
            // NON-LETHAL DAMAGE
            // -----------------------------
            var nl = other.GetComponentInParent<NonLethalHealth>();
            if (nl != null)
            {
                // el ÚNICO método válido en tu sistema es éste
                nl.ApplyCaptureTick(captureTick);

                // si quieres stun, lo haces aumentando el tick
                if (extraStunTick > 0f)
                {
                    nl.ApplyCaptureTick(extraStunTick);
                }
            }
        }

        // -----------------------------
        // ENCOGER LA BALA AL IMPACTAR
        // -----------------------------
        if (shrinker != null)
            shrinker.StartShrink();
        else
            Destroy(gameObject);
    }
}
