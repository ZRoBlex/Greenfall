using UnityEngine;

public class TeamZone : MonoBehaviour
{
    public FactionTeam team;

    [Header("Punto central que los enemigos intentan ocupar")]
    public Transform zoneCenter;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        if (zoneCenter != null)
            Gizmos.DrawSphere(zoneCenter.position, 0.5f);
    }
}
