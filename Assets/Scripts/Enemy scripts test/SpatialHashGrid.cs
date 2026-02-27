using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spatial Hash Grid for O(1) spatial queries instead of O(n)
/// Replaces expensive Distance checks for all enemies
/// </summary>
public class SpatialHashGrid
{
    private Dictionary<Vector2Int, List<Transform>> grid;
    private float cellSize;
    private int capacity;

    // Pool for cell lists to avoid allocations
    private Stack<List<Transform>> listPool;

    public SpatialHashGrid(float cellSize, int estimatedCapacity = 100)
    {
        this.cellSize = cellSize;
        this.capacity = estimatedCapacity;

        grid = new Dictionary<Vector2Int, List<Transform>>(capacity);
        listPool = new Stack<List<Transform>>(32);

        // Pre-warm pool
        for (int i = 0; i < 32; i++)
        {
            listPool.Push(new List<Transform>(8));
        }
    }

    /// <summary>
    /// Clear all cells (call once per frame before re-adding entities)
    /// </summary>
    public void Clear()
    {
        foreach (var list in grid.Values)
        {
            list.Clear();
            if (listPool.Count < 64) // Limit pool size
                listPool.Push(list);
        }
        grid.Clear();
    }

    /// <summary>
    /// Add entity to grid
    /// </summary>
    public void Add(Transform entity)
    {
        Vector2Int cell = WorldToCell(entity.position);

        if (!grid.TryGetValue(cell, out List<Transform> list))
        {
            list = listPool.Count > 0 ? listPool.Pop() : new List<Transform>(8);
            grid[cell] = list;
        }

        list.Add(entity);
    }

    /// <summary>
    /// Get all entities within radius (checks only nearby cells)
    /// </summary>
    public void GetNearby(Vector3 position, float radius, List<Transform> results)
    {
        results.Clear();

        int cellRadius = Mathf.CeilToInt(radius / cellSize);
        Vector2Int centerCell = WorldToCell(position);
        float radiusSqr = radius * radius;

        // Check only cells within radius
        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int z = -cellRadius; z <= cellRadius; z++)
            {
                Vector2Int cell = centerCell + new Vector2Int(x, z);

                if (grid.TryGetValue(cell, out List<Transform> entities))
                {
                    foreach (var entity in entities)
                    {
                        if (entity == null) continue;

                        float distSqr = (entity.position - position).sqrMagnitude;
                        if (distSqr <= radiusSqr)
                        {
                            results.Add(entity);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Find closest entity within radius
    /// </summary>
    public Transform GetClosest(Vector3 position, float radius, System.Func<Transform, bool> filter = null)
    {
        Transform closest = null;
        float closestDistSqr = radius * radius;

        int cellRadius = Mathf.CeilToInt(radius / cellSize);
        Vector2Int centerCell = WorldToCell(position);

        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int z = -cellRadius; z <= cellRadius; z++)
            {
                Vector2Int cell = centerCell + new Vector2Int(x, z);

                if (grid.TryGetValue(cell, out List<Transform> entities))
                {
                    foreach (var entity in entities)
                    {
                        if (entity == null) continue;
                        if (filter != null && !filter(entity)) continue;

                        float distSqr = (entity.position - position).sqrMagnitude;
                        if (distSqr < closestDistSqr)
                        {
                            closestDistSqr = distSqr;
                            closest = entity;
                        }
                    }
                }
            }
        }

        return closest;
    }

    Vector2Int WorldToCell(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / cellSize),
            Mathf.FloorToInt(worldPos.z / cellSize)
        );
    }
}
