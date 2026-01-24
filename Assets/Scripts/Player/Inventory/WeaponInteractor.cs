using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponInteractor : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] float pickupDistance = 3f;
    [SerializeField] LayerMask weaponLayer;

    [Header("References")]
    [SerializeField] WeaponInventory inventory;
    [SerializeField] TextMeshProUGUI interactText;

    [SerializeField] PlayerInput playerInput;
    [SerializeField] string interactActionName = "Interact";
    InputAction interactAction;

    [System.Serializable]
    public class InteractableTagMessage
    {
        public string tag;
        public string message;
    }

    [Header("Extra Interactable Messages")]
    [SerializeField] InteractableTagMessage[] interactableMessages;


    Weapon hoveredWeapon;

    void Awake()
    {
        if (playerInput != null)
        {
            interactAction = playerInput.actions[interactActionName];
        }
    }


    void Update()
    {
        hoveredWeapon = null;

        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance, weaponLayer))
        {
            // 🔹 1) Primero: mensajes por TAG (tienen prioridad absoluta)
            if (TryGetCustomMessage(hit, out string customMessage))
            {
                string key = GetInteractKey();
                interactText.alignment = TextAlignmentOptions.Center;
                interactText.text = $"{customMessage}\n<size=70%>({key})</size>";
                interactText.gameObject.SetActive(true);
                return; // ⛔ No evaluar armas ni nada más
            }

            // 🔹 2) Luego: lógica normal de armas
            Weapon w = hit.collider.GetComponentInParent<Weapon>();
            if (w != null)
            {
                hoveredWeapon = w;
            }
        }

        if (hoveredWeapon != null)
        {
            UpdateInteractText();
            interactText.gameObject.SetActive(true);
        }
        else
        {
            interactText.gameObject.SetActive(false);
        }

        // 🔹 1.5) AmmoBox con nombre personalizado
        AmmoBox ammoBox = hit.collider.GetComponentInParent<AmmoBox>();
        if (ammoBox != null)
        {
            string key = GetInteractKey();
            interactText.alignment = TextAlignmentOptions.Center;
            interactText.text = $"{ammoBox.GetInteractText()}\n<size=70%>({key})</size>";
            interactText.gameObject.SetActive(true);
            return;
        }

    }




    public bool CanPickup => hoveredWeapon != null;

    public void Pickup()
    {
        if (!CanPickup) return;

        Weapon w = hoveredWeapon;
        hoveredWeapon = null;
        interactText.gameObject.SetActive(false);

        //inventory.AddWeapon(w);
        inventory.PickupWeapon(w);

    }

    string GetInteractKey()
    {
        if (interactAction == null)
            return "?";

        // Obtener el binding efectivo (soporta rebinds)
        var binding = interactAction.bindings[0];
        return InputControlPath.ToHumanReadableString(
            binding.effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice
        );
    }

    void UpdateInteractText()
    {
        string key = GetInteractKey();

        bool willSwap = inventory.IsFull;

        string actionText = willSwap ? "Swap Weapon" : "Grab";

        string weaponLabel = hoveredWeapon != null &&
                              !string.IsNullOrEmpty(hoveredWeapon.weaponName)
            ? $" ({hoveredWeapon.weaponName})"
            : "";

        interactText.alignment = TextAlignmentOptions.Center;
        interactText.text = $"{actionText}{weaponLabel}\n<size=70%>({key})</size>";
    }


    bool TryGetCustomMessage(RaycastHit hit, out string message)
    {
        foreach (var entry in interactableMessages)
        {
            if (hit.collider.CompareTag(entry.tag))
            {
                message = entry.message;
                return true;
            }
        }

        message = null;
        return false;
    }

}
