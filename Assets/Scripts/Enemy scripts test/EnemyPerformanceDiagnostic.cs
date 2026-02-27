using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Performance Diagnostic Tool
/// Shows what's consuming CPU and if optimizations are working
/// </summary>
public class EnemyPerformanceDiagnostic : MonoBehaviour
{
    [Header("Display Settings")]
    public bool showDiagnostics = true;
    public KeyCode toggleKey = KeyCode.F1;

    private OptimizedEnemyManager manager;
    private float updateInterval = 0.5f;
    private float updateTimer = 0f;

    // Cached stats
    private int totalEnemies = 0;
    private int activeEnemies = 0;
    private int semiActiveEnemies = 0;
    private int sleepEnemies = 0;

    private float avgFPS = 0f;
    private float frameTime = 0f;

    private int pathfindingQueue = 0;
    private int perceptionQueue = 0;

    void Start()
    {
        manager = OptimizedEnemyManager.Instance;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showDiagnostics = !showDiagnostics;
        }

        if (!showDiagnostics) return;

        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            UpdateStats();
            updateTimer = 0f;
        }
    }

    void UpdateStats()
    {
        // FPS
        avgFPS = 1f / Time.deltaTime;
        frameTime = Time.deltaTime * 1000f;

        if (manager == null) return;

        // Count enemies by LOD
        var allEnemies = FindObjectsOfType<OptimizedEnemyController>();
        totalEnemies = allEnemies.Length;
        activeEnemies = 0;
        semiActiveEnemies = 0;
        sleepEnemies = 0;

        foreach (var enemy in allEnemies)
        {
            switch (enemy.CurrentLOD)
            {
                case EnemyLOD.Active:
                    activeEnemies++;
                    break;
                case EnemyLOD.SemiActive:
                    semiActiveEnemies++;
                    break;
                case EnemyLOD.Sleep:
                    sleepEnemies++;
                    break;
            }
        }
    }

    void OnGUI()
    {
        if (!showDiagnostics) return;

        int width = 350;
        int height = 400;

        GUIStyle bgStyle = new GUIStyle();
        bgStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.8f));

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 16;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.yellow;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 12;
        labelStyle.normal.textColor = Color.white;

        GUIStyle warningStyle = new GUIStyle(GUI.skin.label);
        warningStyle.fontSize = 12;
        warningStyle.fontStyle = FontStyle.Bold;
        warningStyle.normal.textColor = Color.red;

        GUILayout.BeginArea(new Rect(10, 10, width, height), bgStyle);

        GUILayout.Label("🔍 ENEMY PERFORMANCE DIAGNOSTIC", titleStyle);
        GUILayout.Space(10);

        // FPS
        Color fpsColor = avgFPS >= 55 ? Color.green : (avgFPS >= 30 ? Color.yellow : Color.red);
        labelStyle.normal.textColor = fpsColor;
        GUILayout.Label($"FPS: {avgFPS:F1} ({frameTime:F1}ms)", labelStyle);
        labelStyle.normal.textColor = Color.white;

        GUILayout.Space(10);

        // Enemy counts
        GUILayout.Label($"<b>ENEMY COUNTS:</b>", labelStyle);
        GUILayout.Label($"  Total: {totalEnemies}", labelStyle);
        GUILayout.Label($"  Active (Full AI): {activeEnemies}", labelStyle);
        GUILayout.Label($"  SemiActive (Slow): {semiActiveEnemies}", labelStyle);
        GUILayout.Label($"  Sleep (Frozen): {sleepEnemies}", labelStyle);

        GUILayout.Space(10);

        // Warnings
        if (manager == null)
        {
            GUILayout.Label("⚠️ NO MANAGER FOUND!", warningStyle);
            GUILayout.Label("Create OptimizedEnemyManager in scene!", labelStyle);
        }
        else
        {
            GUILayout.Label($"<b>MANAGER STATUS:</b>", labelStyle);
            GUILayout.Label($"  Active Distance: {manager.lodActiveDistance}m", labelStyle);
            GUILayout.Label($"  Semi Distance: {manager.lodSemiActiveDistance}m", labelStyle);
            GUILayout.Label($"  Sleep Distance: {manager.lodSleepDistance}m", labelStyle);
            GUILayout.Space(5);
            GUILayout.Label($"  Max Path/Frame: {manager.maxPathfindingPerFrame}", labelStyle);
            GUILayout.Label($"  Max Perception/Frame: {manager.maxPerceptionPerFrame}", labelStyle);
        }

        GUILayout.Space(10);

        // Performance issues
        if (totalEnemies > 0)
        {
            float activePercent = (float)activeEnemies / totalEnemies * 100f;

            if (activePercent > 50f && totalEnemies > 50)
            {
                GUILayout.Label("⚠️ TOO MANY ACTIVE ENEMIES!", warningStyle);
                GUILayout.Label("Increase LOD distances!", labelStyle);
            }

            if (avgFPS < 30)
            {
                GUILayout.Label("⚠️ LOW FPS!", warningStyle);
                if (manager == null)
                {
                    GUILayout.Label("ADD MANAGER TO SCENE!", warningStyle);
                }
                else
                {
                    GUILayout.Label("Try:", labelStyle);
                    GUILayout.Label("  - Reduce Max Path/Frame to 3", labelStyle);
                    GUILayout.Label("  - Reduce Max Perception/Frame to 15", labelStyle);
                    GUILayout.Label("  - Reduce Active Distance to 20", labelStyle);
                }
            }
        }

        GUILayout.Space(10);
        GUILayout.Label($"Press {toggleKey} to toggle", labelStyle);

        GUILayout.EndArea();
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();

        return result;
    }
}