using UnityEngine;

[RequireComponent(typeof(NPCMotor), typeof(CharacterController))]
public class PreyAI : MonoBehaviour
{
    public float detectionRadius = 12f;
    public float fleeDistance = 10f;
    public LayerMask predatorLayer;
    public LayerMask groundLayer; // Floor

    NPCMotor motor;
    Transform currentPredator;
    float recheckTimer;

    CharacterController controller;

    void Awake()
    {
        motor = GetComponent<NPCMotor>();
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        recheckTimer -= Time.deltaTime;

        if (currentPredator == null)
            DetectPredator();
        else
            Flee();
    }

    void DetectPredator()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            detectionRadius,
            predatorLayer
        );

        if (hits.Length == 0)
            return;

        currentPredator = hits[0].transform;
        ChooseFleeDestination();
    }

    void Flee()
    {
        if (currentPredator == null)
            return;

        float dist = Vector3.Distance(transform.position, currentPredator.position);
        if (dist > detectionRadius * 1.5f)
        {
            currentPredator = null;
            motor.externallyControlled = false;
            return;
        }

        if (recheckTimer <= 0f)
        {
            ChooseFleeDestination();
            recheckTimer = 1.5f;
        }
    }

    void ChooseFleeDestination()
    {
        Vector3 dir = (transform.position - currentPredator.position).normalized;
        Vector3 rawTarget = transform.position + dir * fleeDistance;

        // Buscar suelo real
        Ray ray = new Ray(rawTarget + Vector3.up * 30f, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            motor.externallyControlled = true;

            // IMPORTANTE: mantener Y actual, no usar hit.point.y
            Vector3 target = hit.point;
            target.y = transform.position.y;

            motor.SetTargetPosition(target);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
#endif
}
