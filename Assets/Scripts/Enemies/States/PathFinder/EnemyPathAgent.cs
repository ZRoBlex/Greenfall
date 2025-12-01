using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// EnemyPathAgent ligero y compatible:
/// - MoveTo(destination) -> calcula ruta con el LocalGridPathfinder
/// - GetNextNode(), AdvanceNode(distance) -> navegación por nodos
/// - UpdateDynamicPath() -> revalida y recalcula si es necesario
/// - Stop() -> cancela ruta actual (añadido para ChaseState)
/// - IsMoving / HasPath -> información pública del estado
/// </summary>
[DisallowMultipleComponent]
public class EnemyPathAgent : MonoBehaviour
{
    LocalGridPathfinder pf;
    public List<Vector3> path = new List<Vector3>();
    public int index = 0;
    public Vector3 currentTarget;

    public float repathInterval = 0.4f;
    float repathTimer = 0f;

    void Awake()
    {
        pf = GetComponent<LocalGridPathfinder>();
    }

    /// <summary>
    /// Pide una ruta desde la posición actual hasta destination.
    /// Reemplaza cualquier ruta previa.
    /// </summary>
    public void MoveTo(Vector3 destination)
    {
        currentTarget = destination;
        if (pf == null) pf = GetComponent<LocalGridPathfinder>();
        if (pf == null)
        {
            path = new List<Vector3>();
            index = 0;
            repathTimer = repathInterval;
            return;
        }

        path = pf.FindPath(transform.position, currentTarget) ?? new List<Vector3>();
        index = 0;
        repathTimer = repathInterval; // reset timer al pedir nueva ruta
    }

    /// <summary>
    /// Cancela la ruta actual y deja al agente en estado "quieto".
    /// </summary>
    public void Stop()
    {
        path?.Clear();
        index = 0;
        currentTarget = transform.position;
        repathTimer = 0f;
    }

    /// <summary>
    /// Devuelve el siguiente nodo objetivo (o la posición actual si no hay path).
    /// </summary>
    public Vector3 GetNextNode()
    {
        if (path == null || path.Count == 0) return transform.position;
        index = Mathf.Clamp(index, 0, path.Count - 1);
        return path[index];
    }

    /// <summary>
    /// Si la distancia al nodo actual es menor que distance, avanza el índice.
    /// Retorna true si llegaste al final del path (o si no había path).
    /// </summary>
    public bool AdvanceNode(float distance)
    {
        if (path == null || path.Count == 0) return true;

        if (Vector3.Distance(transform.position, path[index]) < distance)
        {
            index++;
            if (index >= path.Count)
            {
                // Llegó al final
                path.Clear();
                index = 0;
                return true;
            }

            return false;
        }
        return false;
    }

    /// <summary>
    /// Verifica periódicamente si la ruta sigue válida (usando pf.IsPathStillValid si existe)
    /// y recalcula si es necesario. Llamar cada frame desde el estado (o desde Update).
    /// </summary>
    public void UpdateDynamicPath()
    {
        repathTimer -= Time.deltaTime;

        // REPATHEAR SIEMPRE QUE EL TIMER LLEGUE A 0
        if (repathTimer <= 0)
        {
            repathTimer = repathInterval;

            if (currentTarget != null)
            {
                // recalcula la ruta completa al objetivo ACTUAL
                path = pf.FindPath(transform.position, currentTarget);
                index = 0;
            }
            return;
        }

        // --- VALIDACIÓN EXTRA: si la ruta actual deja de ser válida ---
        if (!pf.IsPathStillValid(path))
        {
            if (currentTarget != null)
            {
                path = pf.FindPath(transform.position, currentTarget);
                index = 0;
            }
        }
    }


    /// <summary>
    /// Indica si actualmente tiene una ruta y aún no llegó.
    /// </summary>
    public bool IsMoving => (path != null && path.Count > 0);

    /// <summary>
    /// Alias semántico: existe path pero no necesariamente se está moviendo (depende del estado).
    /// </summary>
    public bool HasPath => (path != null && path.Count > 0);
}
