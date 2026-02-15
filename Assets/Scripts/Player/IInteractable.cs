using UnityEngine;

/// <summary>
/// Interfaz universal para interacción
/// </summary>
public interface IInteractable
{
    string GetInteractText();
    bool CanInteract(GameObject interactor);
    void Interact(GameObject interactor);
    int GetPriority() => 0;
}