using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [System.Serializable]
    public class MaterialDefinition
    {
        public string id;            // "Wood", "Metal", "Scrap", "Brick"
        public float maxAmount = 100f;
    }

    [Header("Material Definitions (IDs visibles en Inspector)")]
    public List<MaterialDefinition> materialDefinitions = new List<MaterialDefinition>();

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

    [Header("Health Damage When Depleted")]
    [SerializeField] private PlayerHealth playerHealth;

    [SerializeField] private float hungerDamagePerSecond = 5f;
    [SerializeField] private float waterDamagePerSecond = 8f;


    public float currentHunger;
    public float currentEnergy;

    private float energyRecoverTimer = 0f; // ⏱️ contador interno


    [Header("Materials")]
    public float maxMaterials = 100f;

    private float currentMaterials;
    private Dictionary<string, float> currentMaterialsById =
    new Dictionary<string, float>();

    private Dictionary<string, float> maxMaterialsById =
        new Dictionary<string, float>();

    [Header("UI References")]
    public UIResource materialsUI;
    public UIMaterialsPanel materialsPanel;

    [Header("Water")]
    public float maxWater = 100f;
    public float currentWater = 100f;

    [SerializeField] private UIResource waterUI;
    [SerializeField] private UIResourceSO waterResourceSO;


    void Start()
    {
        currentHunger = maxHunger;
        currentEnergy = maxEnergy;

        waterUI?.SetAmount(currentWater, maxWater);
        maxWater = waterResourceSO.maxAmount;
        currentWater = maxWater;


        // 🔹 Inicializar materiales desde las definitions del inspector
        foreach (var def in materialDefinitions)
        {
            if (string.IsNullOrEmpty(def.id))
                continue;

            currentMaterialsById[def.id] = 0f;
            maxMaterialsById[def.id] = def.maxAmount;

            // 🔄 Refrescar UI (esto es lo que pone el texto en 0)
            materialsPanel?.SetMaterialAmount(def.id, 0f, def.maxAmount);
        }

        // 🔎 Validar que cada material tenga slot en el panel UI
        if (materialsPanel != null)
        {
            foreach (var def in materialDefinitions)
            {
                bool found = false;

                foreach (var entry in materialsPanel.materials)
                {
                    if (entry.materialId == def.id)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Debug.LogWarning($"⚠️ No hay slot UI para el material ID: {def.id}");
                }
            }
        }



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
        ConsumeWaterOverTime(Time.deltaTime);

        // ======================
        // ❤️ DAÑO POR HAMBRE / AGUA
        // ======================
        HandleStarvationAndDehydrationDamage(Time.deltaTime);

        //if(Input.GetKeyDown(KeyCode.T))
        //{
        //    AddHunger(10f);
        //    AddEnergy(10f);
        //    AddWater(10f);
        //    AddMaterials("Wood", 10f);
        //}

        //if (Input.GetKeyDown(KeyCode.C))
        //{
        //    //AddHunger(10f);
        //    //AddEnergy(10f);
        //    //AddWater(10f);
        //    ConsumeMaterials("Wood", 10f);
        //}

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
    public void AddMaterials(string id, float amount)
    {
        if (!currentMaterialsById.ContainsKey(id))
        {
            Debug.LogWarning($"Material desconocido: {id}");
            return;
        }

        float current = currentMaterialsById[id];
        float max = maxMaterialsById[id];

        current += amount;
        current = Mathf.Clamp(current, 0, max);

        currentMaterialsById[id] = current;

        materialsPanel?.SetMaterialAmount(id, current, max);
    }



    // 🔨 Consumir materiales (crafting, construir, etc.)
    public bool ConsumeMaterials(string id, float amount)
    {
        if (!currentMaterialsById.ContainsKey(id))
            return false;

        float current = currentMaterialsById[id];

        if (current < amount)
            return false;

        current -= amount;
        currentMaterialsById[id] = current;

        float max = maxMaterialsById[id];
        materialsPanel?.SetMaterialAmount(id, current, max);

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

    [SerializeField] private float waterDrainPerSecond = 0.5f;

    // 💧 Consumir agua con el tiempo
    public void ConsumeWaterOverTime(float deltaTime)
    {
        currentWater -= waterDrainPerSecond * deltaTime;
        currentWater = Mathf.Clamp(currentWater, 0f, maxWater);

        waterUI?.SetAmount(currentWater, maxWater);
    }

    // 💧 Recuperar agua (beber, fuentes, botellas)
    public void AddWater(float amount)
    {
        currentWater += amount;
        currentWater = Mathf.Clamp(currentWater, 0f, maxWater);

        waterUI?.SetAmount(currentWater, maxWater);
    }
    void HandleStarvationAndDehydrationDamage(float deltaTime)
    {
        if (playerHealth == null) return;

        // 🍗 Daño por hambre
        if (currentHunger <= 0f)
        {
            playerHealth.TakeDamage(hungerDamagePerSecond * deltaTime);
        }

        // 💧 Daño por sed
        if (currentWater <= 0f)
        {
            playerHealth.TakeDamage(waterDamagePerSecond * deltaTime);
        }
    }

}
