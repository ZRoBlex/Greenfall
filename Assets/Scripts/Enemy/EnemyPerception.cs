using UnityEngine;

public class EnemyPerception : MonoBehaviour
{
    public EnemyStats stats;

    Transform player;

    public bool CanSeePlayer { get; private set; }
    public bool IsPlayerClose { get; private set; }
    public bool HeardPlayer { get; private set; }

    public Transform CurrentTarget { get; private set; }

    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        // 🔴 Si está desactivado por LOD, no procesar
        if (!enabled)
            return;

        CurrentTarget = null;

        if (player == null) return;

        CheckDistance();
        CheckVision();
        CheckHearing();

        if (CanSeePlayer || IsPlayerClose || HeardPlayer)
            CurrentTarget = player;
    }


    void CheckDistance()
    {
        float dist = Vector3.Distance(transform.position, player.position);
        IsPlayerClose = dist <= stats.closeDetectionRadius;
    }

    void CheckVision()
    {
        Vector3 toPlayer = player.position - transform.position;
        float dist = toPlayer.magnitude;

        if (dist > stats.perceptionRange)
        {
            CanSeePlayer = false;
            return;
        }

        float angle = Vector3.Angle(transform.forward, toPlayer);
        CanSeePlayer = angle <= stats.fieldOfView * 0.5f;
    }

    void CheckHearing()
    {
        float dist = Vector3.Distance(transform.position, player.position);
        HeardPlayer = dist <= stats.perceptionRange * 0.6f;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (stats == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stats.closeDetectionRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, stats.perceptionRange * 0.6f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stats.perceptionRange);

        Vector3 left = Quaternion.Euler(0, -stats.fieldOfView / 2f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, stats.fieldOfView / 2f, 0) * transform.forward;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + left * stats.perceptionRange);
        Gizmos.DrawLine(transform.position, transform.position + right * stats.perceptionRange);

        if (CurrentTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, CurrentTarget.position);
        }
    }
#endif

    public void SetExternalTarget(Transform target)
    {
        CurrentTarget = target;
    }
}
