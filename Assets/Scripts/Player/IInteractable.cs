using UnityEngine;

/// <summary>
/// Interfaz para CUALQUIER objeto interactuable
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Texto que se muestra en UI
    /// </summary>
    string GetInteractText();

    /// <summary>
    /// Se puede interactuar ahora?
    /// </summary>
    bool CanInteract(GameObject interactor);

    /// <summary>
    /// Ejecutar interacción
    /// </summary>
    void Interact(GameObject interactor);

    /// <summary>
    /// Prioridad (mayor = más importante si hay varios overlaps)
    /// </summary>
    int GetPriority() => 0;
}