using UnityEngine;

public class EnemyTeamController : MonoBehaviour
{
    public FactionTeam currentTeam = FactionTeam.Neutral;
    public TeamZone currentZone;

    EnemyPathAgent agent;

    void Awake()
    {
        agent = GetComponent<EnemyPathAgent>();
    }

    public void AssignTeam(FactionTeam newTeam)
    {
        currentTeam = newTeam;
    }

    public void SetHomeZone(TeamZone zone)
    {
        currentZone = zone;
    }

    public bool HasHomeZone()
    {
        return currentZone != null && currentZone.zoneCenter != null;
    }

    public Vector3 GetHomePosition()
    {
        return HasHomeZone() ? currentZone.zoneCenter.position : transform.position;
    }
}
