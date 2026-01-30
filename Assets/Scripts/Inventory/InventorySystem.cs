using NUnit.Framework;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem instance;
    public int maxSlots = 20;

    public List<SoObjects> items = new List<SoObjects>();

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool AddItem(SoObjects NewItem)
    {
        if(items.Count <= maxSlots)
        {
            items.Add(NewItem);
            Debug.Log($"Item agregado al inventario : {NewItem.Nombre}");
            return true;
        }

        Debug.Log("Inventario lleno");
        return false;
    }

    public void RemoveItem(SoObject ItemToRemove)
    {
        if(items.Contains(ItemToRemove))
        {
            items.Remove(ItemToRemove);
            Debug.Log($"Item removido del inventario : {ItemToRemove.Nombre}");
        }
        else
        {
            Debug.Log("El item no se encuentra en el inventario");
        }
    }

    public void UseItem()
    {

    }






    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
