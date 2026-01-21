using UnityEngine;

public class AmmoPickupUIManager : MonoBehaviour
{
    public static AmmoPickupUIManager Instance;

    [SerializeField] GameObject ammoPickupUIPrefab;
    [SerializeField] Transform uiParent; // normalmente tu Canvas

    [Header("Spawn Offset")]
    [SerializeField] Vector3 screenOffset = new Vector3(0, -150, 0);

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ShowAmmoPickup(
        int amount,
        string ammoName,
        Sprite icon = null
    )
    {
        if (!ammoPickupUIPrefab || !uiParent) return;
        if (amount <= 0) return;

        GameObject go = Instantiate(ammoPickupUIPrefab, uiParent);

        // Posición centro pantalla + offset
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt)
        {
            rt.anchoredPosition = screenOffset;
        }

        AmmoPickupUI ui = go.GetComponent<AmmoPickupUI>();
        if (ui)
        {
            ui.Setup(amount, ammoName, icon);
        }
    }
}
