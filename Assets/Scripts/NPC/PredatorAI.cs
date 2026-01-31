using UnityEngine;
using System.Collections.Generic;

public class PredatorAI : MonoBehaviour
{
    public float detectionRadius = 20f;
    public float moveSpeed = 3f;
    public float attackDistance = 1.5f;
    public float attackCooldown = 1.2f;
    public int damage = 10;

    public float attackOffset = 1.2f; // distancia a la que se queda antes de atacar

    NPCMotor motor;

    public LayerMask preyLayer;

    NPCHealth currentTarget;
    float attackTimer;

    NPCGroupLeader currentLeaderTarget;

    private void Start()
    {
        motor = GetComponent<NPCMotor>();
    }

    void Update()
    {
        attackTimer -= Time.deltaTime;

        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            if (motor != null)
                motor.externallyControlled = false;

            FindNewTarget();
            return;
        }


        MoveToTarget();
        TryAttack();
    }

    void FindNewTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, preyLayer);

        Debug.Log("Detectados: " + hits.Length);

        if (hits.Length == 0)
        {
            currentTarget = null;
            return;
        }

        List<NPCHealth> possibleTargets = new List<NPCHealth>();

        foreach (var h in hits)
        {
            NPCHealth hp = h.GetComponent<NPCHealth>();
            if (hp == null) continue;

            var leader = hp.GetComponent<NPCGroupLeader>();

            // ❌ no atacar líder si aún tiene miembros
            if (leader != null && leader.members.Count > 0)
                continue;

            possibleTargets.Add(hp);
        }

        // si SOLO hay líder vivo → sí se puede atacar
        if (possibleTargets.Count == 0)
        {
            foreach (var h in hits)
            {
                NPCHealth hp = h.GetComponent<NPCHealth>();
                if (hp != null)
                    possibleTargets.Add(hp);
            }
        }

        if (possibleTargets.Count == 0) return;

        currentTarget = possibleTargets[Random.Range(0, possibleTargets.Count)];
    }

    void MoveToTarget()
    {
        if (motor == null || currentTarget == null) return;

        Vector3 dirToPrey = (currentTarget.transform.position - transform.position).normalized;

        // Punto al que se moverá (antes de chocar con la presa)
        Vector3 offsetTarget = currentTarget.transform.position - dirToPrey * attackOffset;

        motor.externallyControlled = true;
        motor.SetTargetPosition(offsetTarget);
    }



    void TryAttack()
    {
        float dist = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (dist > attackDistance) return;
        if (attackTimer > 0f) return;

        attackTimer = attackCooldown;
        currentTarget.TakeDamage(damage);
    }


#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
#endif
}
