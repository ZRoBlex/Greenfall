using UnityEngine;

public class NPCGroupMember : MonoBehaviour
{
    public Transform leader;
    public float followDistance = 2.5f;
    public float moveSpeed = 2f;

    Vector3 localOffset;

    void Start()
    {
        // Offset fijo dentro del grupo
        Vector2 r = Random.insideUnitCircle * followDistance;
        localOffset = new Vector3(r.x, 0, r.y);
    }

    public void SetLeader(Transform newLeader)
    {
        leader = newLeader;
    }

    void LateUpdate()
    {
        if (leader == null) return;

        Vector3 targetPos = leader.position + localOffset;

        Vector3 dir = targetPos - transform.position;

        if (dir.magnitude < 0.1f)
            return;

        transform.position += dir.normalized * moveSpeed * Time.deltaTime;
    }
}
