using UnityEngine;
using System.Collections.Generic;

public enum EnemyLOD
{
    Active,
    SemiActive,
    Sleep
}

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    public Transform player;

    [Header("Limits")]
    public int maxActiveEnemies = 50;
    public float activeDistance = 30f;
    public float semiActiveDistance = 60f;

    [Header("Repath Control")]
    public int maxRepathsPerFrame = 5;

    List<EnemyController> enemies = new List<EnemyController>();

    int repathsThisFrame;

    void Awake()
    {
        Instance = this;
    }

    void LateUpdate()
    {
        repathsThisFrame = 0;
    }

    public void Register(EnemyController e)
    {
        if (!enemies.Contains(e))
            enemies.Add(e);
    }

    public bool CanRepath()
    {
        if (repathsThisFrame >= maxRepathsPerFrame)
            return false;

        repathsThisFrame++;
        return true;
    }

    void Update()
    {
        UpdateLOD();
    }

    void UpdateLOD()
    {
        if (player == null) return;

        enemies.Sort((a, b) =>
        {
            float da = (a.transform.position - player.position).sqrMagnitude;
            float db = (b.transform.position - player.position).sqrMagnitude;
            return da.CompareTo(db);
        });

        int activeCount = 0;

        foreach (var e in enemies)
        {
            float dist = Vector3.Distance(e.transform.position, player.position);

            if (dist <= activeDistance && activeCount < maxActiveEnemies)
            {
                e.SetLOD(EnemyLOD.Active);
                activeCount++;
            }
            else if (dist <= semiActiveDistance)
            {
                e.SetLOD(EnemyLOD.SemiActive);
            }
            else
            {
                e.SetLOD(EnemyLOD.Sleep);
            }
        }
    }


#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
            return;

        Vector3 center = player.position;

        // ===== ACTIVE =====
        Gizmos.color = new Color(0f, 1f, 0f, 0.15f); // Verde
        Gizmos.DrawSphere(center, activeDistance);

        // ===== SEMI ACTIVE =====
        Gizmos.color = new Color(1f, 1f, 0f, 0.12f); // Amarillo
        Gizmos.DrawSphere(center, semiActiveDistance);

        // ===== SLEEP =====
        Gizmos.color = new Color(1f, 0f, 0f, 0.08f); // Rojo
        //Gizmos.DrawSphere(center, sleepDistance);
    }
#endif

}
