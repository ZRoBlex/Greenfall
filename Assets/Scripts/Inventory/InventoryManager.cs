using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("References")]
    public InventoryArea inventoryArea;
    public GameObject itemUIPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool AddItem(InventoryItemData data)
    {
        if (inventoryArea == null)
        {
            Debug.LogError("[InventoryManager] InventoryArea no asignado");
            return false;
        }

        GameObject go = Instantiate(itemUIPrefab, inventoryArea.transform);

        InventoryItemUI ui = go.GetComponent<InventoryItemUI>();
        ui.data = data;

        if (!inventoryArea.TryPlace(ui))
        {
            Destroy(go);
            Debug.Log("No hay espacio en el inventario");
            return false;
        }

        return true;
    }
}
