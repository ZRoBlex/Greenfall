using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class PlayerAmmoInteractor : MonoBehaviour
{
    [Header("Raycast")]
    public float interactDistance = 4f;

    [Header("Allowed Tags")]
    public List<string> interactableTags = new List<string>()
    {
        "AmmoBox",
        "Weapon",
        "Enemy"
    };

    [Header("UI")]
    public TextMeshProUGUI interactText;

    [Header("References")]
    public Camera playerCamera;
    public PlayerInput playerInput;                 // 👈 PlayerInput directo
    public string actionMapName = "Player";         // 👈 Action Map
    public string interactActionName = "Interact"; // 👈 Action

    private InputAction interactAction;
    private AmmoBox hoveredBox;

    void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        // 👇 Buscar PlayerInput automáticamente
        if (playerInput == null)
        {
            playerInput = GetComponentInParent<PlayerInput>();

            if (playerInput == null)
            {
                Debug.LogError("❌ PlayerAmmoInteractor: No se encontró PlayerInput en el padre");
                return;
            }
            else
            {
                Debug.Log("✅ PlayerAmmoInteractor: PlayerInput encontrado automáticamente");
            }
        }

        // 👇 Obtener Action Map y Action
        var map = playerInput.actions.FindActionMap(actionMapName, true);
        interactAction = map.FindAction(interactActionName, true);

        // 👇 Suscribirse al evento (pulso real tipo GetKeyDown)
        interactAction.performed += OnInteractPerformed;
    }

    void OnDestroy()
    {
        if (interactAction != null)
            interactAction.performed -= OnInteractPerformed;
    }

    void Update()
    {
        hoveredBox = null;
        interactText.text = "";

        Vector3 origin = playerCamera.transform.position + playerCamera.transform.forward * 0.1f;
        Ray ray = new Ray(origin, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            string hitTag = hit.collider.tag;

            if (IsTagAllowed(hitTag) || IsTagAllowed(hit.collider.transform.root.tag))
            {
                AmmoBox box = hit.collider.GetComponentInParent<AmmoBox>();
                if (box != null)
                {
                    hoveredBox = box;
                    interactText.text = "INTERACT";
                }
            }
        }

        if (hoveredBox != null)
        {
            Debug.Log("👀 Hovering AmmoBox: " + hoveredBox.name);
        }
    }

    // 👇 Se llama EXACTAMENTE cuando presionas Interact
    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (hoveredBox == null)
            return;

        Debug.Log("💥 Interact ejecutado sobre: " + hoveredBox.name);

        hoveredBox.Interact(this.gameObject);
        interactText.text = "";
        hoveredBox = null;
    }

    bool IsTagAllowed(string tag)
    {
        for (int i = 0; i < interactableTags.Count; i++)
        {
            if (interactableTags[i] == tag)
                return true;
        }
        return false;
    }
}
