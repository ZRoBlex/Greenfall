using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.I;

    [Header("References")]
    public GameObject rootPanel; // panel principal
    public InventorySlotUI slotPrefab;
    public Transform gridParent;

    [Header("Runtime")]
    List<InventorySlotUI> slotUIs = new();

    [Header("Scripts to disable when inventory is open")]
    public List<MonoBehaviour> scriptsToDisable = new();

    bool isOpen;
    void Start()
    {
        BuildGrid();
        rootPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }
    }

    void Toggle()
    {
        isOpen = !isOpen;
        rootPanel.SetActive(isOpen);

        Cursor.visible = isOpen;
        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;

        foreach (var script in scriptsToDisable)
        {
            if (script != null)
                script.enabled = !isOpen;
        }
    }


    void BuildGrid()
    {
        slotUIs.Clear();

        foreach (Transform c in gridParent)
            Destroy(c.gameObject);

        var inv = GeneralInventory.Instance;

        for (int i = 0; i < inv.slots.Count; i++)
        {
            var ui = Instantiate(slotPrefab, gridParent);
            ui.Init(this, i);
            slotUIs.Add(ui);
        }
    }

    public void Refresh()
    {
        var inv = GeneralInventory.Instance;

        for (int i = 0; i < slotUIs.Count; i++)
        {
            slotUIs[i].Set(inv.slots[i]);
        }
    }

    public void OnSlotClicked(int index)
    {
        var inv = GeneralInventory.Instance;
        var slot = inv.slots[index];

        // manejar drag/drop desde aquí
        DragItemUI.Instance.OnSlotClicked(index, slot);
        Refresh();
    }
}
