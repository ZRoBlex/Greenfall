using UnityEngine;

public class BuildModeController : MonoBehaviour
{
    public bool IsBuildMode { get; private set; }

    public BuildController buildController;
    public GameObject weaponRoot;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
            ToggleBuildMode();
    }

    void ToggleBuildMode()
    {
        IsBuildMode = !IsBuildMode;

        buildController.enabled = IsBuildMode;

        if (weaponRoot != null)
            weaponRoot.SetActive(!IsBuildMode);

        if (!IsBuildMode)
            buildController.ForceHidePreview();
    }
}
