using UnityEngine;
using UnityEngine.InputSystem;

public class ResourceHarvester : MonoBehaviour
{
    [Header("Harvest Settings")]
    public Camera cam;
    public float range = 4f;
    public float damage = 10f;

    public PlayerStats playerStats;
    public InputAction harvestAction;

    void Awake()
    {
        // 🔹 Tomar PlayerStats automáticamente
        playerStats = GetComponentInParent<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("❌ No se encontró PlayerStats en el jugador.");
        }

        // 🔹 Tomar PlayerInput automáticamente
        var playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput != null)
        {
            harvestAction = playerInput.actions["Harvest"];
        }
        else
        {
            Debug.LogError("❌ No se encontró PlayerInput en el jugador.");
        }
    }

    void OnEnable()
    {
        harvestAction?.Enable();
    }

    void OnDisable()
    {
        harvestAction?.Disable();
    }

    void Update()
    {
        if (harvestAction == null)
            return;

        if (harvestAction.WasPressedThisFrame())
        {
            TryHarvest();
        }
    }

    void TryHarvest()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            var node = hit.collider.GetComponentInParent<ResourceNode>();
            if (node != null)
            {
                node.Damage(damage);
            }
        }
    }
}
