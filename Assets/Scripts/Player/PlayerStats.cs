using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Stats")]
    public float maxHunger = 100f;
    public float maxEnergy = 100f;

    public float hungerDecayPerSecond = 1f;
    public float energyDecayPerSecond = 8f;

    [Header("Recovery")]
    public float energyRecoverPerSecond = 10f;
    public float energyRecoverDelay = 2f; // ⏱️ tiempo antes de empezar a recuperar

    [Header("Multipliers")]
    public float walkHungerMultiplier = 1.25f;
    public float sprintHungerMultiplier = 2f;

    [Header("UI References")]
    public UIResource hungerUI;
    public UIResource energyUI;

    [Header("References")]
    public FirstPersonController playerController;

    private float currentHunger;
    private float currentEnergy;

    private float energyRecoverTimer = 0f; // ⏱️ contador interno


    [Header("Materials")]
    public float maxMaterials = 100f;

    private float currentMaterials;

    [Header("UI References")]
    public UIResource materialsUI;
    public UIMaterialsPanel materialsPanel;


    void Start()
    {
        currentHunger = maxHunger;
        currentEnergy = maxEnergy;

        currentMaterials = 0f; // normalmente empiezas sin materiales

        materialsPanel?.SetMaterialAmount("Generic", currentMaterials, maxMaterials);


        hungerUI?.SetAmount(currentHunger, maxHunger);
        energyUI?.SetAmount(currentEnergy, maxEnergy);
    }

    void Update()
    {
        bool isMoving = IsPlayerMoving();
        bool isSprinting = playerController != null && playerController.IsSprinting();

        // ======================
        // 🔻 HUNGER (igual que antes)
        // ======================
        float hungerMultiplier = 1f;

        if (isMoving)
            hungerMultiplier = walkHungerMultiplier;

        if (isSprinting)
            hungerMultiplier = sprintHungerMultiplier;

        currentHunger -= hungerDecayPerSecond * hungerMultiplier * Time.deltaTime;
        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);

        // ======================
        // 🔋 ENERGY
        // ======================

        if (isSprinting)
        {
            // 🔻 Consumir energía
            currentEnergy -= energyDecayPerSecond * Time.deltaTime;
            currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);

            // ⏸️ resetear timer cada frame que corre
            energyRecoverTimer = energyRecoverDelay;
        }
        else
        {
            // ⏱️ contar hacia atrás el delay
            if (energyRecoverTimer > 0f)
            {
                energyRecoverTimer -= Time.deltaTime;
            }
            else
            {
                // 🔼 recuperar energía
                if (currentEnergy < maxEnergy)
                {
                    currentEnergy += energyRecoverPerSecond * Time.deltaTime;
                    currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
                }
            }
        }

        // ======================
        // 🔹 UI
        // ======================
        hungerUI?.SetAmount(currentHunger, maxHunger);
        energyUI?.SetAmount(currentEnergy, maxEnergy);
    }

    private bool IsPlayerMoving()
    {
        if (playerController == null) return false;

        Vector2 moveInput = playerController.GetMovementInput();
        return moveInput.sqrMagnitude > 0.01f;
    }

    [Header("Thresholds")]
    [Range(0, 100)] public float hungerSprintThreshold = 20f;
    [Range(0, 100)] public float energySprintThreshold = 20f;

    public bool CanSprint()
    {
        float hungerPercent = (currentHunger / maxHunger) * 100f;
        float energyPercent = (currentEnergy / maxEnergy) * 100f;

        return hungerPercent > hungerSprintThreshold &&
               energyPercent > energySprintThreshold;
    }


    // 🍗 Recuperar hambre desde otros objetos (comida, pickups, etc.)
    public void AddHunger(float amount)
    {
        currentHunger += amount;
        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);

        // 🔹 Actualizar UI inmediatamente
        hungerUI?.SetAmount(currentHunger, maxHunger);
    }

    public void AddEnergy(float amount)
    {
        currentEnergy += amount;
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);

        energyUI?.SetAmount(currentEnergy, maxEnergy);
    }

    // 🧱 Agregar materiales desde pickups, crafting, recompensas, etc.
    public void AddMaterials(float amount)
    {
        currentMaterials += amount;
        currentMaterials = Mathf.Clamp(currentMaterials, 0, maxMaterials);

        materialsPanel?.SetMaterialAmount("Generic", currentMaterials, maxMaterials);
    }


    // 🔨 Consumir materiales (crafting, construir, etc.)
    public bool ConsumeMaterials(float amount)
    {
        if (currentMaterials < amount)
            return false;

        currentMaterials -= amount;
        currentMaterials = Mathf.Clamp(currentMaterials, 0, maxMaterials);

        materialsUI?.SetAmount(currentMaterials, maxMaterials);
        return true;
    }

    public bool HasResources(ResourceCost[] costs)
    {
        foreach (var cost in costs)
        {
            switch (cost.resourceType)
            {
                case ResourceType.Material:
                    if (currentMaterials < cost.amount) return false;
                    break;

                case ResourceType.Food:
                    if (currentHunger < cost.amount) return false;
                    break;

                case ResourceType.Energy:
                    if (currentEnergy < cost.amount) return false;
                    break;
            }
        }

        return true;
    }

    public bool ConsumeResources(ResourceCost[] costs)
    {
        if (!HasResources(costs))
            return false;

        foreach (var cost in costs)
        {
            switch (cost.resourceType)
            {
                case ResourceType.Material:
                    currentMaterials -= cost.amount;
                    materialsUI?.SetAmount(currentMaterials, maxMaterials);
                    break;

                case ResourceType.Food:
                    currentHunger -= cost.amount;
                    hungerUI?.SetAmount(currentHunger, maxHunger);
                    break;

                case ResourceType.Energy:
                    currentEnergy -= cost.amount;
                    energyUI?.SetAmount(currentEnergy, maxEnergy);
                    break;
            }
        }

        return true;
    }


}
