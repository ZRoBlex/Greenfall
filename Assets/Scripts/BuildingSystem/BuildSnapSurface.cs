using UnityEngine;

public static class BuildSnapSurface
{
    public static Vector3 SnapToSurface(
        GameObject prefab,
        Vector3 hitPoint,
        Vector3 hitNormal,
        Quaternion rotation
    )
    {
        GameObject ghost = Object.Instantiate(prefab);
        ghost.transform.rotation = rotation;

        Bounds bounds = CalculateBounds(ghost);
        Object.Destroy(ghost);

        float bottomOffset = bounds.extents.y;
        return hitPoint + hitNormal.normalized * bottomOffset;
    }

    static Bounds CalculateBounds(GameObject go)
    {
        Bounds b = new Bounds(go.transform.position, Vector3.zero);

        foreach (var r in go.GetComponentsInChildren<Renderer>())
            b.Encapsulate(r.bounds);

        return b;
    }
}
