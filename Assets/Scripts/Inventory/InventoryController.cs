using UnityEngine;

public class InventoryController : MonoBehaviour
{
    public GameObject inventoryUI;
    bool open;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            open = !open;
            inventoryUI.SetActive(open);

            Cursor.visible = open;
            Cursor.lockState = open
                ? CursorLockMode.None
                : CursorLockMode.Locked;
        }
    }
}
