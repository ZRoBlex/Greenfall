using UnityEngine;

/// <summary>
/// Adaptador para AmmoBox
/// Añadir este componente al prefab de AmmoBox
/// </summary>
[RequireComponent(typeof(AmmoBox))]
public class AmmoBoxAdapter : MonoBehaviour, IInteractable
{
    AmmoBox ammoBox;

    void Awake()
    {
        ammoBox = GetComponent<AmmoBox>();
    }

    public string GetInteractText()
    {
        return ammoBox != null ? ammoBox.GetInteractText() : "Grab Ammo";
    }

    public bool CanInteract(GameObject interactor)
    {
        return ammoBox != null;
    }

    public void Interact(GameObject interactor)
    {
        if (ammoBox != null)
            ammoBox.Interact(interactor);
    }

    public int GetPriority() => 5;
}

/// <summary>
/// Adaptador para AmmoPickup
/// </summary>
[RequireComponent(typeof(AmmoPickup))]
public class AmmoPickupAdapter : MonoBehaviour, IInteractable
{
    AmmoPickup pickup;

    void Awake()
    {
        pickup = GetComponent<AmmoPickup>();
    }

    public string GetInteractText()
    {
        return pickup != null ? pickup.GetInteractText() : "Pick Up Ammo";
    }

    public bool CanInteract(GameObject interactor)
    {
        return pickup != null;
    }

    public void Interact(GameObject interactor)
    {
        if (pickup != null)
            pickup.Interact(interactor);
    }

    public int GetPriority() => 5;
}

/// <summary>
/// Adaptador para SeedPickup
/// </summary>
[RequireComponent(typeof(SeedPickup))]
public class SeedPickupAdapter : MonoBehaviour, IInteractable
{
    SeedPickup seedPickup;

    void Awake()
    {
        seedPickup = GetComponent<SeedPickup>();
    }

    public string GetInteractText()
    {
        if (seedPickup != null && seedPickup.seedData != null)
            return $"Pick Up {seedPickup.amount}x {seedPickup.seedData.seedId}";

        return "Pick Up Seed";
    }

    public bool CanInteract(GameObject interactor)
    {
        return seedPickup != null && seedPickup.seedData != null;
    }

    public void Interact(GameObject interactor)
    {
        if (seedPickup == null)
            return;

        // Aquí conectas con tu inventario de semillas cuando lo hagas
        Debug.Log($"Picked up {seedPickup.amount}x {seedPickup.seedData.seedId}");

        // Desactivar pickup
        seedPickup.gameObject.SetActive(false);
    }

    public int GetPriority() => 3;
}

/// <summary>
/// Adaptador para Weapon
/// </summary>
public class WeaponAdapter : MonoBehaviour, IInteractable
{
    [SerializeField] Weapon weapon;
    WeaponInventory cachedInventory;

    void Awake()
    {
        if (weapon == null)
            weapon = GetComponent<Weapon>();
    }

    public string GetInteractText()
    {
        if (weapon == null)
            return "Grab Weapon";

        bool willSwap = cachedInventory != null && cachedInventory.IsFull;
        string action = willSwap ? "Swap" : "Grab";

        string weaponName = !string.IsNullOrEmpty(weapon.weaponName)
            ? weapon.weaponName
            : "Weapon";

        return $"{action} {weaponName}";
    }

    public bool CanInteract(GameObject interactor)
    {
        if (weapon == null)
            return false;

        if (cachedInventory == null)
            cachedInventory = interactor.GetComponentInChildren<WeaponInventory>();

        return true;
    }

    public void Interact(GameObject interactor)
    {
        if (cachedInventory == null)
            cachedInventory = interactor.GetComponentInChildren<WeaponInventory>();

        if (cachedInventory != null && weapon != null)
            cachedInventory.PickupWeapon(weapon);
    }

    public int GetPriority() => 10;
}

/// <summary>
/// Adaptador para ResourceNode
/// </summary>
public class ResourceNodeAdapter : MonoBehaviour, IInteractable
{
    [SerializeField] ResourceNode node;
    [SerializeField] float damage = 10f;

    void Awake()
    {
        if (node == null)
            node = GetComponent<ResourceNode>();
    }

    public string GetInteractText()
    {
        return node != null ? $"Harvest {node.name}" : "Harvest";
    }

    public bool CanInteract(GameObject interactor)
    {
        return node != null;
    }

    public void Interact(GameObject interactor)
    {
        if (node != null)
            node.Damage(damage);
    }

    public int GetPriority() => 3;
}

/// <summary>
/// Adaptador para PlantSpot (farming)
/// </summary>
public class PlantSpotAdapter : MonoBehaviour, IInteractable
{
    PlantSpot spot;
    PlayerFarmingTool farmingTool;

    void Awake()
    {
        spot = GetComponent<PlantSpot>();
    }

    public string GetInteractText()
    {
        if (spot == null)
            return "Plant";

        return spot.CanPlant() ? "Plant Seed" : "Already Planted";
    }

    public bool CanInteract(GameObject interactor)
    {
        if (spot == null || !spot.CanPlant())
            return false;

        if (farmingTool == null)
            farmingTool = interactor.GetComponentInChildren<PlayerFarmingTool>();

        return farmingTool != null && farmingTool.selectedSeed != null;
    }

    public void Interact(GameObject interactor)
    {
        if (farmingTool == null)
            farmingTool = interactor.GetComponentInChildren<PlayerFarmingTool>();

        if (farmingTool != null && farmingTool.selectedSeed != null && spot != null)
            spot.PlantSeed(farmingTool.selectedSeed);
    }

    public int GetPriority() => 4;
}

/// <summary>
/// Adaptador para PlantInstance (harvest)
/// </summary>
public class PlantInstanceAdapter : MonoBehaviour, IInteractable
{
    PlantInstance plant;

    void Awake()
    {
        plant = GetComponent<PlantInstance>();
    }

    public string GetInteractText()
    {
        if (plant == null)
            return "Harvest";

        return plant.CanHarvest() ? "Harvest Plant" : "Not Ready";
    }

    public bool CanInteract(GameObject interactor)
    {
        return plant != null && plant.CanHarvest();
    }

    public void Interact(GameObject interactor)
    {
        if (plant != null && plant.CanHarvest())
            plant.Harvest();
    }

    public int GetPriority() => 6;
}