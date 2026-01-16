using UnityEngine;

public class BuildPreview : MonoBehaviour
{
    GameObject instance;
    Renderer[] renderers;
    BuildPreviewCollision collision;

    public Material validMat;
    public Material invalidMat;

    void Awake()
    {
        collision = GetComponent<BuildPreviewCollision>();
    }

    public void Show(StructureData data, Vector3 pos, Quaternion rot, bool valid)
    {
        if (instance == null || instance.name != data.previewPrefab.name)
        {
            if (instance) Destroy(instance);
            instance = Instantiate(data.previewPrefab);
            renderers = instance.GetComponentsInChildren<Renderer>();
        }

        instance.transform.SetPositionAndRotation(pos, rot);

        foreach (var r in renderers)
            r.material = valid ? validMat : invalidMat;

        instance.SetActive(true);
    }

    public void Hide()
    {
        if (instance) instance.SetActive(false);
    }

    public bool Blocked() => collision != null && collision.IsBlocked();
}
