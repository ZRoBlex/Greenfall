using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] GameObject inventoryUI;
    bool isOpen;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }
    }

    void ToggleInventory()
    {
        isOpen = !isOpen;
        inventoryUI.SetActive(isOpen);

        if (isOpen)
            GamePauseManager.Pause();
        else
            GamePauseManager.Resume();
    }
}
