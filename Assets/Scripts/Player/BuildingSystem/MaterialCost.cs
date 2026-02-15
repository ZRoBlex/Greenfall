using UnityEngine;

/// <summary>
/// Costo de materiales ÚNICO
/// Usado en construcción, crafting, upgrades, etc.
/// </summary>
[System.Serializable]
public class MaterialCost
{
    [Tooltip("ID del material (debe coincidir con PlayerStats.materialDefinitions)")]
    public string materialId; // "Wood", "Stone", "Metal", "Brick", etc.

    [Range(0f, 1000f)]
    public float amount;

    // Constructores
    public MaterialCost() { }

    public MaterialCost(string id, float amt)
    {
        materialId = id;
        amount = amt;
    }

    // Helper para validación
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(materialId) && amount > 0f;
    }
}