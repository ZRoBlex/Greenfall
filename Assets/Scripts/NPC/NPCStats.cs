using UnityEngine;

[CreateAssetMenu(menuName = "NPC/NPCStats")]
public class NPCStats : ScriptableObject
{
    public float moveSpeed = 2f;
    public float turnSpeed = 5f;

    public int wanderRadius = 10;
    public float cellHeight = 1.8f;

    public float minWaitTime = 1f;
    public float maxWaitTime = 3f;

    public LayerMask obstacleLayers;
    public string[] obstacleTags;
}
