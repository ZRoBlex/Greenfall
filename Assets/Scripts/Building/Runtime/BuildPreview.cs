using UnityEngine;

public class BuildPreview : MonoBehaviour
{
    Renderer[] rends;

    void Awake()
    {
        rends = GetComponentsInChildren<Renderer>();
    }

    public void SetValid(bool valid)
    {
        Color c = valid ? Color.cyan : Color.red;
        foreach (var r in rends)
            r.material.color = c;
    }
}
