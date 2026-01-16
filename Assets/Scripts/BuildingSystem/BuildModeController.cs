using UnityEngine;

public class BuildModeController : MonoBehaviour
{
    public bool IsBuildMode { get; private set; }

    [Header("References")]
    public BuildController buildController;
    public GameObject weaponRoot;

    void Awake()
    {
        // 🔒 FORZAR estado inicial
        IsBuildMode = false;

        if (buildController != null)
            buildController.enabled = false;

        if (weaponRoot != null)
            weaponRoot.SetActive(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
            ToggleBuildMode();
    }

    void ToggleBuildMode()
    {
        IsBuildMode = !IsBuildMode;

        if (buildController != null)
            buildController.enabled = IsBuildMode;

        if (weaponRoot != null)
            weaponRoot.SetActive(!IsBuildMode);

        if (!IsBuildMode && buildController != null)
            buildController.ForceHidePreview();
    }
}
