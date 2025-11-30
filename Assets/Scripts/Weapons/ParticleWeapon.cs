using UnityEngine;

public class ParticleWeapon : MonoBehaviour, IWeapon
{
    public ParticleSystem particlePrefab;
    public float duration = 1f;
    public float damage = 20f;
    public float range = 40f;
    public LayerMask hitMask;

    public void Fire(Transform firePoint, Transform aimTarget)
    {
        if (particlePrefab == null) return;

        // instanciar sistema de partículas en el firePoint y reproducir
        var ps = Instantiate(particlePrefab, firePoint.position, firePoint.rotation);
        ps.Play();

        // lógica simplificada: raycast hacia aimTarget o forward
        Vector3 dir = (aimTarget != null) ? (aimTarget.position - firePoint.position).normalized : firePoint.forward;

        // raycast para aplicar daño a quien toque (ej: jugador/enemigo)
        if (Physics.Raycast(firePoint.position, dir, out RaycastHit hit, range, hitMask))
        {
            var h = hit.collider.GetComponent<Health>();
            if (h != null)
            {
                h.ApplyDamage(damage);
            }
        }

        Destroy(ps.gameObject, duration + 0.5f);
    }
}