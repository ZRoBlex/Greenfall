using UnityEngine;

public class EnemySpawnerArea : MonoBehaviour
{
    public Transform player;
    public Vector2 areaSize = new Vector2(80, 80); // ancho x largo
    public float heightRaycast = 200f;

    void LateUpdate()
    {
        if (player != null)
            transform.position = player.position;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);

        Vector3 center = transform.position;
        Vector3 size = new Vector3(areaSize.x, 1f, areaSize.y);

        Gizmos.DrawCube(center, size);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);
    }
#endif

    public bool IsInsideArea(Vector3 pos)
    {
        Vector3 local = pos - transform.position;

        return Mathf.Abs(local.x) <= areaSize.x * 0.5f &&
               Mathf.Abs(local.z) <= areaSize.y * 0.5f;
    }
}
