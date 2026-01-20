using UnityEngine;

public interface IInteractable
{
    string GetInteractText();
    void Interact(GameObject interactor);
}
