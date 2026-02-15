using UnityEngine;

/// <summary>
/// Sistema de captura ULTRA optimizado - ERROR ARREGLADO
/// </summary>
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerCapture : MonoBehaviour
{
    [Header("═══════ CAPTURA ═══════")]
    [Range(1f, 10f)]
    [SerializeField] float captureRange = 3f;

    [SerializeField] LayerMask enemyLayer;

    [Header("═══════ FEEDBACK ═══════")]
    [SerializeField] bool showDebugMessages = true;

    // ═══════ CACHE ═══════

    PlayerInputHandler input;
    Transform playerTransform;

    readonly Collider[] overlapResults = new Collider[10];
    int overlapCount;

    // ═══════ LIFECYCLE ═══════

    void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        playerTransform = transform;

        if (input == null)
        {
            Debug.LogError("❌ PlayerCapture: No PlayerInputHandler");
            enabled = false;
        }
    }

    void Update()
    {
        if (input.InteractTrigger)
        {
            TryCapture();
            input.ResetInteractTrigger();
        }
    }

    // ═══════ CAPTURA ═══════

    void TryCapture()
    {
        overlapCount = Physics.OverlapSphereNonAlloc(
            playerTransform.position,
            captureRange,
            overlapResults,
            enemyLayer,
            QueryTriggerInteraction.Ignore
        );

        if (overlapCount == 0)
            return;

        EnemyController closestEnemy = null;
        float closestDistSqr = float.MaxValue;

        for (int i = 0; i < overlapCount; i++)
        {
            EnemyController enemy = overlapResults[i].GetComponent<EnemyController>();

            if (enemy == null || !IsStunned(enemy))
                continue;

            float distSqr = (enemy.transform.position - playerTransform.position).sqrMagnitude;

            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closestEnemy = enemy;
            }
        }

        if (closestEnemy != null)
            CaptureEnemy(closestEnemy);
        else if (showDebugMessages)
            Debug.Log("⚠️ No hay enemigos stunned cerca");
    }

    bool IsStunned(EnemyController enemy)
    {
        if (enemy == null || enemy.FSM == null)
            return false;

        return enemy.FSM.CurrentState is StunnedState;
    }

    void CaptureEnemy(EnemyController enemy)
    {
        if (enemy == null)
            return;

        // Resetear non-lethal health
        NonLethalHealthAdapted nl = enemy.GetComponent<NonLethalHealthAdapted>();
        if (nl != null)
            nl.ResetHealth();

        // Cambiar a friendly
        enemy.SetTypeAndTeam(CannibalType.Friendly, "Player");

        // ✅ FIX: Usar Motor en vez de Movement
        if (enemy.Motor != null)
            enemy.Motor.enabled = true;

        // Target al jugador
        if (enemy.Perception != null)
            enemy.Perception.SetExternalTarget(playerTransform);

        // Cambiar a FollowingState
        if (enemy.FSM != null)
            enemy.FSM.ChangeState(new FollowingState());

        if (showDebugMessages)
            Debug.Log($"✅ {enemy.name} capturado → Friendly");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, captureRange);
    }
#endif
}