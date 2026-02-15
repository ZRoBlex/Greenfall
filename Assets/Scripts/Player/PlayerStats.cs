using System.Collections.Generic;
using UnityEngine;
using static StructureData;

/// <summary>
/// Stats del jugador ULTRA optimizados
/// - Cache de componentes
/// - Diccionarios pre-inicializados
/// - Sin buscar componentes en runtime
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [System.Serializable]
    public class MaterialDefinition
    {
        public string id;
        [Range(0f, 1000f)]
        public float maxAmount = 100f;
    }

    [Header("═══════ MATERIALES ═══════")]
    public List<MaterialDefinition> materialDefinitions = new List<MaterialDefinition>();

    [Header("═══════ STATS BÁSICOS ═══════")]
    [Range(0f, 200f)]
    public float maxHunger = 100f;

    [Range(0f, 200f)]
    public float maxEnergy = 100f;

    [Range(0f, 200f)]
    public float maxWater = 100f;

    [Header("═══════ DECAY ═══════")]
    [Range(0f, 10f)]
    public float hungerDecayPerSecond = 1f;

    [Range(0f, 20f)]
    public float energyDecayPerSecond = 8f;

    [Range(0f, 5f)]
    public float waterDrainPerSecond = 0.5f;

    [Header("═══════ RECOVERY ═══════")]
    [Range(0f, 30f)]
    public float energyRecoverPerSecond = 10f;

    [Range(0f, 5f)]
    public float energyRecoverDelay = 2f;

    [Header("═══════ MULTIPLIERS ═══════")]
    [Range(1f, 3f)]
    public float walkHungerMultiplier = 1.25f;

    [Range(1.5f, 5f)]
    public float sprintHungerMultiplier = 2f;

    [Header("═══════ THRESHOLDS ═══════")]
    [Range(0f, 100f)]
    public float hungerSprintThreshold = 20f;

    [Range(0f, 100f)]
    public float energySprintThreshold = 20f;

    [Header("═══════ DAMAGE ═══════")]
    [Range(0f, 20f)]
    [SerializeField] float hungerDamagePerSecond = 5f;

    [Range(0f, 20f)]
    [SerializeField] float waterDamagePerSecond = 8f;

    [Header("═══════ REFERENCIAS ═══════")]
    [SerializeField] FirstPersonController playerController;
    [SerializeField] PlayerHealth playerHealth;

    [Header("═══════ UI ═══════")]
    public UIResource hungerUI;
    public UIResource energyUI;
    public UIResource waterUI;
    public UIMaterialsPanel materialsPanel;

    // ═══════ ESTADO ═══════

    [HideInInspector] public float currentHunger;
    [HideInInspector] public float currentEnergy;
    [HideInInspector] public float currentWater;

    float energyRecoverTimer;

    // ═══════ MATERIALES ═══════

    readonly Dictionary<string, float> currentMaterialsById = new Dictionary<string, float>();
    readonly Dictionary<string, float> maxMaterialsById = new Dictionary<string, float>();

    // ═══════ LIFECYCLE ═══════

    void Start()
    {
        // Auto-find si falta
        if (playerController == null)
            playerController = GetComponent<FirstPersonController>();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        // Inicializar stats
        currentHunger = maxHunger;
        currentEnergy = maxEnergy;
        currentWater = maxWater;

        // Inicializar materiales
        InitializeMaterials();

        // Actualizar UI
        UpdateAllUI();
    }

    void InitializeMaterials()
    {
        currentMaterialsById.Clear();
        maxMaterialsById.Clear();

        foreach (var def in materialDefinitions)
        {
            if (string.IsNullOrEmpty(def.id))
                continue;

            currentMaterialsById[def.id] = 0f;
            maxMaterialsById[def.id] = def.maxAmount;

            if (materialsPanel != null)
                materialsPanel.SetMaterialAmount(def.id, 0f, def.maxAmount);
        }
    }

    void Update()
    {
        UpdateStats(Time.deltaTime);
        UpdateAllUI();
        HandleStarvationDamage(Time.deltaTime);

        // DEBUG: Añadir recursos
        if (Input.GetKeyDown(KeyCode.T))
        {
            AddHunger(10f);
            AddEnergy(10f);
            AddWater(10f);
            AddMaterials("Wood", 10f);
        }
    }

    // ═══════ UPDATE STATS ═══════

    void UpdateStats(float deltaTime)
    {
        bool isMoving = IsPlayerMoving();
        bool isSprinting = playerController != null && playerController.IsSprinting();

        // Hunger
        float hungerMult = isMoving ? walkHungerMultiplier : 1f;
        if (isSprinting)
            hungerMult = sprintHungerMultiplier;

        currentHunger -= hungerDecayPerSecond * hungerMult * deltaTime;
        currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);

        // Energy
        if (isSprinting)
        {
            currentEnergy -= energyDecayPerSecond * deltaTime;
            currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
            energyRecoverTimer = energyRecoverDelay;
        }
        else
        {
            energyRecoverTimer -= deltaTime;

            if (energyRecoverTimer <= 0f && currentEnergy < maxEnergy)
            {
                currentEnergy += energyRecoverPerSecond * deltaTime;
                currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
            }
        }

        // Water
        currentWater -= waterDrainPerSecond * deltaTime;
        currentWater = Mathf.Clamp(currentWater, 0f, maxWater);
    }

    void HandleStarvationDamage(float deltaTime)
    {
        if (playerHealth == null)
            return;

        if (currentHunger <= 0f)
            playerHealth.TakeDamage(hungerDamagePerSecond * deltaTime);

        if (currentWater <= 0f)
            playerHealth.TakeDamage(waterDamagePerSecond * deltaTime);
    }

    void UpdateAllUI()
    {
        if (hungerUI != null)
            hungerUI.SetAmount(currentHunger, maxHunger);

        if (energyUI != null)
            energyUI.SetAmount(currentEnergy, maxEnergy);

        if (waterUI != null)
            waterUI.SetAmount(currentWater, maxWater);
    }

    bool IsPlayerMoving()
    {
        if (playerController == null)
            return false;

        Vector2 input = playerController.GetMovementInput();
        return input.sqrMagnitude > 0.01f;
    }

    // ═══════ API PÚBLICA ═══════

    public bool CanSprint()
    {
        float hungerPercent = (currentHunger / maxHunger) * 100f;
        float energyPercent = (currentEnergy / maxEnergy) * 100f;

        return hungerPercent > hungerSprintThreshold &&
               energyPercent > energySprintThreshold;
    }

    public void AddHunger(float amount)
    {
        currentHunger = Mathf.Clamp(currentHunger + amount, 0f, maxHunger);
    }

    public void AddEnergy(float amount)
    {
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0f, maxEnergy);
    }

    public void AddWater(float amount)
    {
        currentWater = Mathf.Clamp(currentWater + amount, 0f, maxWater);
    }

    public void AddMaterials(string id, float amount)
    {
        if (!currentMaterialsById.ContainsKey(id))
        {
            Debug.LogWarning($"Material desconocido: {id}");
            return;
        }

        float current = currentMaterialsById[id];
        float max = maxMaterialsById[id];

        current = Mathf.Clamp(current + amount, 0f, max);
        currentMaterialsById[id] = current;

        if (materialsPanel != null)
            materialsPanel.SetMaterialAmount(id, current, max);
    }

    public bool ConsumeMaterials(string id, float amount)
    {
        if (!currentMaterialsById.ContainsKey(id))
            return false;

        float current = currentMaterialsById[id];

        if (current < amount)
            return false;

        current -= amount;
        currentMaterialsById[id] = current;

        if (materialsPanel != null)
        {
            float max = maxMaterialsById[id];
            materialsPanel.SetMaterialAmount(id, current, max);
        }

        return true;
    }

    public bool HasMaterials(MaterialCost[] costs)
    {
        foreach (var cost in costs)
        {
            if (!currentMaterialsById.TryGetValue(cost.materialId, out float current))
                return false;

            if (current < cost.amount)
                return false;
        }

        return true;
    }

    public bool ConsumeMaterials(MaterialCost[] costs)
    {
        if (!HasMaterials(costs))
            return false;

        foreach (var cost in costs)
        {
            float current = currentMaterialsById[cost.materialId];
            float max = maxMaterialsById[cost.materialId];

            current -= cost.amount;
            currentMaterialsById[cost.materialId] = current;

            if (materialsPanel != null)
                materialsPanel.SetMaterialAmount(cost.materialId, current, max);
        }

        return true;
    }
}