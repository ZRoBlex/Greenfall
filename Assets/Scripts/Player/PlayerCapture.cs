using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerCapture : MonoBehaviour
{
    [Header("Rango de Interacción")]
    public float interactRange = 3f;

    [Header("Layer de Enemigos")]
    public LayerMask enemyLayer;

    private PlayerInputHandler inputHandler;
    private Transform playerTransform;

    void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        playerTransform = transform;
        inputHandler.enabled = true;
    }

    void Update()
    {
        if (inputHandler.InteractTrigger)
        {
            TryCapture();
            inputHandler.ResetInteractTrigger(); // reset para evitar múltiples capturas en un solo frame
        }
    }

    void TryCapture()
    {
        // Detectamos enemigos dentro del rango
        Collider[] hits = Physics.OverlapSphere(playerTransform.position, interactRange, enemyLayer);

        foreach (Collider hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();
            StunnedState stunnedState = enemy?.FSM.CurrentState as StunnedState;

            if (enemy != null && stunnedState != null)
            {
                // Cambiamos el tipo a Friendly
                enemy.SetTypeAndTeam(CannibalType.Friendly, "Player");

                // Reactivamos el motor en caso de que esté desactivado por stun
                if (enemy.Motor != null)
                    enemy.Motor.enabled = true;

                // Opcional: asignar al jugador como target
                if (enemy.Perception != null)
                    enemy.Perception.SetExternalTarget(playerTransform);

                // Cambiar inmediatamente al estado Following
                enemy.FSM.ChangeState(new FollowingState());

                Debug.Log($"[PlayerCapture] Enemigo {enemy.name} capturado y ahora es Friendly!");
            }
        }
    }

    // Debug visual en editor
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(playerTransform.position, interactRange);
    }
#endif
}
