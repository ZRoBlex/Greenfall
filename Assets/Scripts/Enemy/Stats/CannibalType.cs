using UnityEngine;

/// <summary>
/// Tipos de comportamiento base para enemigos
/// </summary>
public enum CannibalType
{
    Aggressive,  // Ataca al jugador
    Passive,     // Huye del jugador
    Neutral,     // Ignora al jugador (patrulla)
    Friendly     // Sigue al jugador como aliado
}

/// <summary>
/// Probabilidad de aparición de cada tipo
/// </summary>
[System.Serializable]
public class CannibalTypeProbability
{
    public CannibalType type;

    [Range(0f, 100f)]
    [Tooltip("Peso probabilístico (no necesita sumar 100)")]
    public float weight = 25f;
}