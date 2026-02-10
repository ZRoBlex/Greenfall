using UnityEngine;

/// <summary>
/// Interfaz para sistemas de movimiento
/// Permite cambiar entre Grid y NavMesh sin romper código
/// </summary>
public interface IEnemyMovement
{
    //void Initialize(EnemyStats stats);
    //void SetTarget(Transform target);
    //void SetDestination(Vector3 position);
    //bool HasReachedDestination();
    //void SetEnabled(bool enabled);
    //void ResetMovement();

    // Propiedades
    bool RotateTowardsMovement { get; set; }
}