using UnityEngine;

public class StructureDamageDealer : MonoBehaviour
{
    public int damage = 50;

    public void DealDamage(GameObject target)
    {
        var health = target.GetComponentInParent<StructureHealth>();
        if (health != null)
            health.ApplyDamage(damage);
    }
}
