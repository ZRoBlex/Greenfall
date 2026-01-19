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

    private EnemyController detectedEnemy;

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

        NonLethalHealth nl = ec.GetComponent<NonLethalHealth>();
        if (nl == null) return;

        bool ko = nl.IsStunned();
        bool fullCapture = nl.currentCapture >= nl.maxCapture;

        if (ko || fullCapture)
        {
            interactionText.text = "CAPTURE";
            detectedEnemy = ec;
        }
    }

    void TryInteract()
    {
        if (detectedEnemy == null) return;

        NonLethalHealth nl = detectedEnemy.GetComponent<NonLethalHealth>();
        if (nl == null) return;

        bool fullCapture = nl.currentCapture >= nl.maxCapture;

        if (!fullCapture) return;

        detectedEnemy.SetType(CannibalType.Friendly);

        if (detectedEnemy.Motor != null)
            detectedEnemy.Motor.enabled = true;

        detectedEnemy.FSM.ChangeState(new FollowingState());

        interactionText.text = "";

        nl.currentCapture = 0f;
    }
}
