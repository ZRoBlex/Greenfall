using UnityEngine;

[RequireComponent(typeof(EnemyController))]
public class Combat : MonoBehaviour
{
    public EnemyStats stats;
    public LayerMask damageMask;

    float lastAttackTime = -999f;

    public void TryMeleeAttack(Transform target)
    {
        if (Time.time - lastAttackTime < stats.attackCooldown) return;
        lastAttackTime = Time.time;

        Collider[] hits = Physics.OverlapSphere(transform.position, stats.attackRange, damageMask);
        foreach (var c in hits)
        {
            var h = c.GetComponent<PlayerHealth>();
            if (h != null) h.TakeDamage(stats.attackDamage);
        }
    }
}
