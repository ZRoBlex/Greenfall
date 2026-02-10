using UnityEngine;

/// <summary>
/// Niveles de detalle para optimización de enemigos
/// </summary>
public enum EnemyLOD
{
    Active,      // IA completa, animaciones, pathfinding
    SemiActive,  // IA simplificada, sin movimiento
    Sleep        // Completamente pausado
}