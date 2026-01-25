using UnityEngine;

public static class BuildSnapUtility
{
    public static Vector3 GetBottomOffset(
        GameObject prefab,
        Quaternion rot
    )
    {
        if (!prefab) return Vector3.zero;

        MeshRenderer[] renderers =
            prefab.GetComponentsInChildren<MeshRenderer>();

        if (renderers.Length == 0)
            return Vector3.zero;

        Bounds localBounds = renderers[0].localBounds;

        foreach (var r in renderers)
            localBounds.Encapsulate(r.localBounds);

        float yOffset = localBounds.extents.y;

        return rot * new Vector3(0, yOffset, 0);
    }
}
