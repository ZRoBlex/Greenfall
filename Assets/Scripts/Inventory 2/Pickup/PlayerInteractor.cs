// ============================================================
// PlayerInteractor.cs
// Carpeta: Scripts/Pickup/
// ------------------------------------------------------------
// Componente del JUGADOR que detecta objetos recogibles cercanos.
// Funciona con CUALQUIER IPickable sin importar el tipo.
//
// DOS MODOS (configurables en Inspector):
//
// MODO RAYCAST (recomendado para FPS):
//   Lanza un rayo desde el centro de la cámara hacia adelante.
//   Si golpea algo con IPickable → muestra el prompt y recoge con E.
//   Tiene rango configurable (ej: 2.5 metros).
//
// MODO SPHERE (alternativo):
//   Detecta todos los IPickable en un radio.
//   Recoge el más cercano al presionar E.
//   Útil si el juego no es FPS puro.
//
// PROMPT DE UI:
//   PickupPromptUI es un componente de UI separado.
//   PlayerInteractor solo llama Show(label) / Hide() en él.
//   Si no hay PickupPromptUI, funciona igual pero sin texto en pantalla.
//
// VA EN: el mismo GameObject que PlayerController
//
// EJEMPLO DE USO DESDE CÓDIGO:
//   No necesitas llamar nada manualmente.
//   Solo agrega este componente al Player.
// ============================================================

using UnityEngine;
using Greenfall.Inventory;

public class PlayerInteractor : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // CONFIGURACIÓN
    // ─────────────────────────────────────────────────────────

    public enum DetectionMode { Raycast, Sphere }

    [Header("Modo de Detección")]
    [SerializeField] private DetectionMode _mode = DetectionMode.Raycast;

    [Header("Raycast")]
    [Tooltip("Rango de detección en metros (modo Raycast).")]
    [SerializeField] private float _rayRange    = 2.5f;
    [Tooltip("Capas que puede golpear el raycast. Excluir Player.")]
    [SerializeField] private LayerMask _rayMask = ~0;

    [Header("Sphere")]
    [Tooltip("Radio de detección en metros (modo Sphere).")]
    [SerializeField] private float _sphereRadius = 2f;

    [Header("Input")]
    [Tooltip("Tecla para recoger.")]
    [SerializeField] private KeyCode _pickupKey = KeyCode.E;

    [Header("Referencias")]
    [Tooltip("La cámara principal. Se autodetecta si no se asigna.")]
    [SerializeField] private Camera _camera;

    [Tooltip("UI que muestra el texto de interacción. Puede ser null.")]
    [SerializeField] private PickupPromptUI _promptUI;

    // ─────────────────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────────────────

    // El IPickable que el jugador está mirando/tiene más cerca ahora
    private IPickable _currentTarget;

    // ─────────────────────────────────────────────────────────
    // INICIALIZACIÓN
    // ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (_camera == null)
            _camera = Camera.main;
    }

    // ─────────────────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────────────────

    private void Update()
    {
        // 1. Detectar qué IPickable hay disponible este frame
        IPickable detected = _mode == DetectionMode.Raycast
            ? DetectWithRaycast()
            : DetectWithSphere();

        // 2. Actualizar la UI si el target cambió
        if (detected != _currentTarget)
        {
            _currentTarget = detected;
            UpdatePrompt();
        }

        // 3. Recoger al presionar la tecla
        if (_currentTarget != null && Input.GetKeyDown(_pickupKey))
        {
            bool success = _currentTarget.TryPickup();

            if (success)
            {
                // El objeto fue recogido: limpiar target y UI
                _currentTarget = null;
                _promptUI?.Hide();
            }
            else
            {
                // Inventario lleno u otro error
                _promptUI?.ShowFullMessage();
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    // DETECCIÓN CON RAYCAST
    // ─────────────────────────────────────────────────────────

    private IPickable DetectWithRaycast()
    {
        if (_camera == null) return null;

        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, _rayRange, _rayMask,
                            QueryTriggerInteraction.Collide))
        {
            // Buscar IPickable en el objeto golpeado o en su padre
            return hit.collider.GetComponent<IPickable>()
                ?? hit.collider.GetComponentInParent<IPickable>();
        }

        return null;
    }

    // ─────────────────────────────────────────────────────────
    // DETECCIÓN CON ESFERA
    // ─────────────────────────────────────────────────────────

    private IPickable DetectWithSphere()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _sphereRadius, _rayMask,
                                               QueryTriggerInteraction.Collide);

        IPickable closest  = null;
        float     minDist  = float.MaxValue;

        foreach (var col in hits)
        {
            var pickable = col.GetComponent<IPickable>()
                        ?? col.GetComponentInParent<IPickable>();

            if (pickable == null) continue;

            float dist = Vector3.Distance(transform.position,
                                          pickable.WorldTransform.position);
            if (dist < minDist)
            {
                minDist  = dist;
                closest  = pickable;
            }
        }

        return closest;
    }

    // ─────────────────────────────────────────────────────────
    // UI PROMPT
    // ─────────────────────────────────────────────────────────

    private void UpdatePrompt()
    {
        if (_currentTarget == null)
        {
            _promptUI?.Hide();
            return;
        }

        // El label viene del propio objeto ("Recoger AK-47", "Recoger Semilla x3")
        string label = _currentTarget.GetPickupLabel();
        _promptUI?.Show(label, _pickupKey);
    }

    // ─────────────────────────────────────────────────────────
    // GIZMOS DE DEBUG
    // ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_mode == DetectionMode.Sphere)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _sphereRadius);
        }
        else if (_camera != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(_camera.transform.position,
                           _camera.transform.forward * _rayRange);
        }
    }
#endif
}
