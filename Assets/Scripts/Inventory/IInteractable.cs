using UnityEngine;
public interface IInteractable
{
    /// <summary>Texto que aparece en la UI. Ej: "Abrir Puerta", "Hablar con Elias".</summary>
    string GetInteractionText();

    /// <summary>Ejecuta la acción de interacción. Llamado al presionar E.</summary>
    bool TryInteract(GameObject interactor);

    /// <summary>Transform del objeto para calcular distancia.</summary>
    Transform WorldTransform { get; }
}
