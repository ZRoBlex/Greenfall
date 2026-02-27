using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized Enemy Manager:
/// - Distributes pathfinding requests across frames
/// - Manages LOD based on distance to player
/// - Spatial hashing for efficient queries
/// - Update batching
/// </summary>
public class OptimizedEnemyManager : MonoBehaviour
{
    public static OptimizedEnemyManager Instance { get; private set; }

    [Header("Performance Settings")]
    [Tooltip("Max pathfinding requests per frame")]
    public int maxPathfindingPerFrame = 5;

    [Tooltip("Max perception updates per frame")]
    public int maxPerceptionPerFrame = 20;

    [Header("LOD Distances")]
    public float lodActiveDistance = 25f;
    public float lodSemiActiveDistance = 50f;
    public float lodSleepDistance = 100f;

    [Header("Spatial Hashing")]
    public float spatialCellSize = 10f;

    // Registered enemies
    private List<OptimizedEnemyController> allEnemies = new List<OptimizedEnemyController>();
    private HashSet<OptimizedEnemyController> activeEnemies = new HashSet<OptimizedEnemyController>();

    // Pathfinding queue
    private Queue<System.Action> pathfindingQueue = new Queue<System.Action>();
    private int pathfindingThisFrame = 0;

    // Perception queue
    private Queue<OptimizedEnemyPerception> perceptionQueue = new Queue<OptimizedEnemyPerception>();
    private int perceptionThisFrame = 0;

    // Spatial hash
    private SpatialHashGrid spatialHash;

    // Player reference (cached)
    private Transform player;

    // Frame timing
    private float updateTimer = 0f;
    private const float UPDATE_INTERVAL = 0.1f; // Update LOD every 0.1s

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Initialize spatial hash
        spatialHash = new SpatialHashGrid(spatialCellSize);

        // Find player once
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            player = playerGO.transform;
    }

    void Update()
    {
        // Reset per-frame counters
        pathfindingThisFrame = 0;
        perceptionThisFrame = 0;

        // Process pathfinding queue
        ProcessPathfindingQueue();

        // Process perception queue
        ProcessPerceptionQueue();

        // Update LOD periodically
        updateTimer += Time.deltaTime;
        if (updateTimer >= UPDATE_INTERVAL)
        {
            UpdateLOD();
            UpdateSpatialHash();
            updateTimer = 0f;
        }
    }

    /// <summary>
    /// Register enemy with manager
    /// </summary>
    public void Register(OptimizedEnemyController enemy)
    {
        if (!allEnemies.Contains(enemy))
        {
            allEnemies.Add(enemy);
            activeEnemies.Add(enemy);
        }
    }

    /// <summary>
    /// Unregister enemy
    /// </summary>
    public void Unregister(OptimizedEnemyController enemy)
    {
        allEnemies.Remove(enemy);
        activeEnemies.Remove(enemy);
    }

    /// <summary>
    /// Request pathfinding (queued to avoid frame spikes)
    /// </summary>
    public void RequestPathfinding(System.Action pathfindCallback)
    {
        pathfindingQueue.Enqueue(pathfindCallback);
    }

    /// <summary>
    /// Process pathfinding queue with frame budget
    /// </summary>
    private void ProcessPathfindingQueue()
    {
        while (pathfindingQueue.Count > 0 && pathfindingThisFrame < maxPathfindingPerFrame)
        {
            var callback = pathfindingQueue.Dequeue();
            callback?.Invoke();
            pathfindingThisFrame++;
        }
    }

    /// <summary>
    /// Request perception update (queued)
    /// </summary>
    public void RequestPerceptionUpdate(OptimizedEnemyPerception perception)
    {
        if (!perceptionQueue.Contains(perception))
            perceptionQueue.Enqueue(perception);
    }

    /// <summary>
    /// Process perception queue
    /// </summary>
    private void ProcessPerceptionQueue()
    {
        while (perceptionQueue.Count > 0 && perceptionThisFrame < maxPerceptionPerFrame)
        {
            var perception = perceptionQueue.Dequeue();
            if (perception != null && perception.enabled)
            {
                perception.UpdatePerception();
                perceptionThisFrame++;
            }
        }
    }

    /// <summary>
    /// Update LOD based on distance to player
    /// </summary>
    private void UpdateLOD()
    {
        if (player == null) return;

        Vector3 playerPos = player.position;

        foreach (var enemy in allEnemies)
        {
            if (enemy == null) continue;

            float distSqr = (enemy.transform.position - playerPos).sqrMagnitude;

            if (distSqr <= lodActiveDistance * lodActiveDistance)
            {
                enemy.SetLOD(EnemyLOD.Active);
            }
            else if (distSqr <= lodSemiActiveDistance * lodSemiActiveDistance)
            {
                enemy.SetLOD(EnemyLOD.SemiActive);
            }
            else if (distSqr <= lodSleepDistance * lodSleepDistance)
            {
                enemy.SetLOD(EnemyLOD.Sleep);
            }
            else
            {
                // Too far - could despawn
                enemy.SetLOD(EnemyLOD.Sleep);
            }
        }
    }

    /// <summary>
    /// Update spatial hash for efficient queries
    /// </summary>
    private void UpdateSpatialHash()
    {
        spatialHash.Clear();

        foreach (var enemy in activeEnemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                spatialHash.Add(enemy.transform);
            }
        }
    }

    /// <summary>
    /// Get enemies near position (uses spatial hash)
    /// </summary>
    public void GetEnemiesNear(Vector3 position, float radius, List<Transform> results)
    {
        spatialHash.GetNearby(position, radius, results);
    }

    /// <summary>
    /// Get closest enemy to position
    /// </summary>
    public Transform GetClosestEnemy(Vector3 position, float maxRadius)
    {
        return spatialHash.GetClosest(position, maxRadius);
    }

    /// <summary>
    /// Get player transform (cached)
    /// </summary>
    public Transform GetPlayer()
    {
        return player;
    }

    /// <summary>
    /// Debug info
    /// </summary>
    void OnGUI()
    {
        if (!Application.isPlaying) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"<b>Enemy Manager Stats</b>");
        GUILayout.Label($"Total Enemies: {allEnemies.Count}");
        GUILayout.Label($"Active Enemies: {activeEnemies.Count}");
        GUILayout.Label($"Pathfinding Queue: {pathfindingQueue.Count}");
        GUILayout.Label($"Perception Queue: {perceptionQueue.Count}");
        GUILayout.Label($"Pathfinding/Frame: {pathfindingThisFrame}/{maxPathfindingPerFrame}");
        GUILayout.Label($"Perception/Frame: {perceptionThisFrame}/{maxPerceptionPerFrame}");
        GUILayout.EndArea();
    }
}

/// <summary>
/// LOD levels for enemies
/// </summary>
public enum EnemyLOD
{
    Active,      // Full AI, perception, pathfinding
    SemiActive,  // Reduced update rate
    Sleep        // Frozen
}
