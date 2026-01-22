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
    public float sleepDistance = 90f; // 🔥 NUEVO

    [Header("Repath Control")]
    public int maxRepathsPerFrame = 5;

    [Header("Debug Overlay")]
    public bool showDebugOverlay = true;
    public Vector2 debugPosition = new Vector2(10, 10);

    List<EnemyController> enemies = new List<EnemyController>();

    int repathsThisFrame;

    // Conteos LOD
    int activeCount;
    int semiActiveCount;
    int sleepCount;

    // Conteo real en escena (solo activos en jerarquía)
    int activeInSceneCount;

    // FPS
    float fps;
    float fpsTimer;
    int frameCount;

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
        if (e == null)
            return;

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
        UpdateFPS();

        // Limpieza periódica de referencias muertas
        if (Time.frameCount % 60 == 0) // cada ~1 segundo
            CleanupList();
    }

    /* ===================== LOD ===================== */

    void UpdateLOD()
    {
        if (player == null)
            return;

        // Reset conteos
        activeCount = 0;
        semiActiveCount = 0;
        sleepCount = 0;
        activeInSceneCount = 0;

        // Ordenar por distancia (para priorizar Active)
        enemies.Sort((a, b) =>
        {
            if (a == null || b == null) return 0;

            float da = (a.transform.position - player.position).sqrMagnitude;
            float db = (b.transform.position - player.position).sqrMagnitude;
            return da.CompareTo(db);
        });

        int activeAssigned = 0;

        foreach (var e in enemies)
        {
            // 🔒 Ignorar enemigos desactivados o destruidos
            if (e == null || !e.gameObject.activeInHierarchy)
                continue;

            activeInSceneCount++;

            float dist = Vector3.Distance(e.transform.position, player.position);

            if (dist <= activeDistance && activeAssigned < maxActiveEnemies)
            {
                e.SetLOD(EnemyLOD.Active);
                activeAssigned++;
                activeCount++;
            }
            else if (dist <= semiActiveDistance)
            {
                e.SetLOD(EnemyLOD.SemiActive);
                semiActiveCount++;
            }
            else if (dist <= sleepDistance)
            {
                e.SetLOD(EnemyLOD.Sleep);
                sleepCount++;
            }
            else
            {
                // Fuera de todo rango → forzar Sleep
                e.SetLOD(EnemyLOD.Sleep);
                sleepCount++;
            }
        }
    }

    /* ===================== FPS ===================== */

    void UpdateFPS()
    {
        frameCount++;
        fpsTimer += Time.unscaledDeltaTime;

        if (fpsTimer >= 0.5f)
        {
            fps = frameCount / fpsTimer;
            frameCount = 0;
            fpsTimer = 0f;
        }
    }

    /* ===================== CLEANUP ===================== */

    void CleanupList()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (enemies[i] == null)
                enemies.RemoveAt(i);
        }
    }

    /* ===================== DEBUG GUI ===================== */

    void OnGUI()
    {
        if (!showDebugOverlay)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.normal.textColor = Color.white;

        Rect area = new Rect(debugPosition.x, debugPosition.y, 300, 210);
        GUI.Box(area, "");

        float y = debugPosition.y + 10;

        GUI.Label(new Rect(debugPosition.x + 10, y, 280, 25), "=== ENEMY MANAGER ===", style);
        y += 25;

        GUI.Label(new Rect(debugPosition.x + 10, y, 280, 25), "FPS: " + fps.ToString("F1"), style);
        y += 25;

        GUI.Label(new Rect(debugPosition.x + 10, y, 280, 25), "Active:        " + activeCount, style);
        y += 22;

        GUI.Label(new Rect(debugPosition.x + 10, y, 280, 25), "SemiActive:    " + semiActiveCount, style);
        y += 22;

        GUI.Label(new Rect(debugPosition.x + 10, y, 280, 25), "Sleep:         " + sleepCount, style);
        y += 22;

        GUI.Label(new Rect(debugPosition.x + 10, y, 280, 25), "Active In Scene: " + activeInSceneCount, style);
        y += 25;

        GUI.Label(new Rect(debugPosition.x + 10, y, 280, 25), "Registered Total: " + enemies.Count, style);
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
        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Gizmos.DrawSphere(center, activeDistance);

        // ===== SEMI ACTIVE =====
        Gizmos.color = new Color(1f, 1f, 0f, 0.12f);
        Gizmos.DrawSphere(center, semiActiveDistance);

        // ===== SLEEP =====
        Gizmos.color = new Color(1f, 0f, 0f, 0.08f);
        Gizmos.DrawSphere(center, sleepDistance);
    }
#endif
}
