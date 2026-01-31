using UnityEngine;
using System.Collections.Generic;

public class NPCGroupLeader : MonoBehaviour
{
    public List<NPCGroupMember> members = new List<NPCGroupMember>();

    public float wanderRadius = 10f;
    public float moveSpeed = 2f;
    public float waitTime = 3f;

    Vector3 target;
    float timer;

    void Start()
    {
        PickNewTarget();
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
            PickNewTarget();

        MoveLeader();
    }

    void MoveLeader()
    {
        Vector3 dir = target - transform.position;

        if (dir.magnitude < 0.3f)
            return;

        Vector3 move = dir.normalized * moveSpeed * Time.deltaTime;
        transform.position += move;
    }

    void PickNewTarget()
    {
        Vector2 r = Random.insideUnitCircle * wanderRadius;
        target = transform.position + new Vector3(r.x, 0, r.y);
        timer = waitTime;
    }

    public void RemoveMember(NPCGroupMember m)
    {
        members.Remove(m);

        if (members.Count > 0)
            PromoteNewLeader();
    }

    void PromoteNewLeader()
    {
        NPCGroupMember newLeader = members[0];
        members.RemoveAt(0);

        var leader = newLeader.gameObject.AddComponent<NPCGroupLeader>();
        leader.members = members;

        foreach (var m in members)
            m.SetLeader(leader.transform);

        Destroy(this);
    }
}
