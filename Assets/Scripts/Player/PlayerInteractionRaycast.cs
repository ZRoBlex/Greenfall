using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerInteractRaycast : MonoBehaviour
{
    [Header("Raycast")]
    public float interactDistance = 4f;
    public LayerMask enemyMask;

    [Header("UI")]
    public TextMeshProUGUI interactionText;

    [Header("References")]
    public Camera cam;
    public PlayerInput input;

    EnemyController detectedEnemy;

    void Awake()
    {
        if (cam == null) cam = Camera.main;

        input.actions["Interact"].performed += ctx => TryInteract();
        interactionText.text = "";
    }

    void Update()
    {
        DoRaycast();
    }

    void DoRaycast()
    {
        interactionText.text = "";
        detectedEnemy = null;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, enemyMask))
            return;

        EnemyController ec = hit.collider.GetComponentInParent<EnemyController>();
        if (ec == null) return;

        NonLethalHealth nl = ec.nonLethalHealth;
        if (nl == null) return;

        bool ko = nl.IsUnconscious();
        bool fullCapture = nl.currentCapture >= nl.maxCapture;

        // Mostrar "CAPTURE" si está KO o a 100%
        if (ko || fullCapture)
        {
            interactionText.text = "CAPTURE";
            detectedEnemy = ec;
        }
    }

    void TryInteract()
    {
        if (detectedEnemy == null) return;

        NonLethalHealth nl = detectedEnemy.nonLethalHealth;
        if (nl == null) return;

        bool fullCapture = nl.currentCapture >= nl.maxCapture;

        // SOLO interactuar si está al 100%
        if (!fullCapture) return;

        // Cambia estado a Friendly
        detectedEnemy.ChangeState(new FriendlyFollowState());

        interactionText.text = "";
    }
}
