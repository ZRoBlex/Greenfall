using UnityEngine;

public class BuildPreview : MonoBehaviour
{
    GameObject previewInstance;
    Renderer[] renderers;

    public Material validMat;
    public Material invalidMat;
    BuildPreviewCollision collision;

    void Awake()
    {
        collision = GetComponent<BuildPreviewCollision>();
    }

    public void Show(
        StructureConfig config,
        Vector3 worldPos,
        Quaternion rotation,
        bool valid)
    {
        if (previewInstance == null ||
            previewInstance.name != config.previewPrefab.name)
        {
            Clear();
            previewInstance = Instantiate(config.previewPrefab);
            renderers = previewInstance.GetComponentsInChildren<Renderer>();
        }

        previewInstance.transform.SetPositionAndRotation(worldPos, rotation);

        foreach (var r in renderers)
            r.material = valid ? validMat : invalidMat;

        previewInstance.SetActive(true);
    }

    public void Hide()
    {
        if (previewInstance)
            previewInstance.SetActive(false);
    }

    void Clear()
    {
        if (previewInstance)
            Destroy(previewInstance);
    }

    public bool IsBlockedByCollision()
    {
        return collision != null && collision.IsBlocked();
    }
}
