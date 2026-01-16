using UnityEngine;

public class GridSystem : MonoBehaviour
{
    public float cellSize = 1f;
    public bool enabledGrid = true;

    /// <summary>
    /// Snap determinista basado en dirección del rayo
    /// </summary>
    public Vector3 Snap(Vector3 world, Vector3 rayDir)
    {
        if (!enabledGrid)
            return world;

        // pequeño bias en dirección del rayo (CLAVE)
        Vector3 biased = world + rayDir.normalized * 0.001f;

        float x = Mathf.Floor(biased.x / cellSize) * cellSize + cellSize * 0.5f;
        float z = Mathf.Floor(biased.z / cellSize) * cellSize + cellSize * 0.5f;

        return new Vector3(x, world.y, z);
    }
}
