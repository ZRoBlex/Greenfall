using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerCapture : MonoBehaviour
{
    [Header("Rango de Interacción")]
    public float interactRange = 3f;

    [Header("Layer de Enemigos")]
    public LayerMask enemyLayer;

    PlayerInputHandler input;
    Transform playerTransform;

    void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        playerTransform = transform;

        if (input == null)
            Debug.LogError("[PlayerCapture] No se encontró PlayerInputHandler en el jugador.");
    }

    void Update()
    {
        if (input == null)
            return;

        if (input.InteractTrigger)
        {
            TryCapture();
        }
    }

    void TryCapture()
    {
        Collider[] hits = Physics.OverlapSphere(
            playerTransform.position,
            interactRange,
            enemyLayer,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hits.Length; i++)
        {
            EnemyController enemy = hits[i].GetComponent<EnemyController>();
            if (enemy == null)
                continue;

            // 🔹 Debe estar en StunnedState REAL
            if (!(enemy.FSM.CurrentState is StunnedState))
                continue;

            CaptureEnemy(enemy);
            break; // solo uno por pulsación
        }
    }

    void CaptureEnemy(EnemyController enemy)
    {
        // 🔹 Resetear Non-Lethal Health
        NonLethalHealth nl = enemy.GetComponent<NonLethalHealth>();
        if (nl != null)
        {
            nl.ResetHealth();
        }

        // 🔹 Cambiar tipo y equipo
        enemy.SetTypeAndTeam(CannibalType.Friendly, "Player");

        // 🔹 Reactivar motor por seguridad
        if (enemy.Motor != null && !enemy.Motor.enabled)
            enemy.Motor.enabled = true;

        // 🔹 Forzar target al jugador
        if (enemy.Perception != null)
            enemy.Perception.SetExternalTarget(playerTransform);

        // 🔹 Cambiar inmediatamente al estado Following
        enemy.FSM.ChangeState(new FollowingState());

        Debug.Log($"[PlayerCapture] {enemy.name} capturado → ahora es Friendly");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
#endif
}
