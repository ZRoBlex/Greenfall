using UnityEngine;

public class PlayerInteractController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] PlayerInteractRaycast interactRaycast;
    [SerializeField] PlayerInputHandler inputHandler;

    void Awake()
    {
        if (inputHandler == null)
            inputHandler = GetComponent<PlayerInputHandler>();
    }

    void Update()
    {
        if (!inputHandler.InteractTrigger)
            return;

        TryInteract();
        inputHandler.ResetInteractTrigger(); // MUY IMPORTANTE
    }

    void TryInteract()
    {
        if (interactRaycast == null)
            return;

        var interactable = interactRaycast.CurrentInteractable;
        if (interactable == null)
            return;

        interactable.Interact(gameObject);
    }
}
