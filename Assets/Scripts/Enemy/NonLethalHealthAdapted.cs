using UnityEngine;
using System;

[RequireComponent(typeof(EnemyController))]
public class NonLethalHealthAdapted : MonoBehaviour
{
    [Header("Capture Settings")]
    public float maxCapture = 100f;
    public float currentCapture = 0f;
    public float decayPerSecond = 5f;
    public float stunDuration = 10f;

    [Header("Manual Control")]
    [Tooltip("Forzar al enemigo a estar stuned manualmente")]
    public bool forceStunned = false;

    // Eventos para animaciones o sistemas externos
    public event Action OnStunned;
    public event Action OnFullyStunned;
    public event Action OnRecovered;

    EnemyController ec;
    bool isStunned = false;
    float stunTimer = 0f;

    void Awake()
    {
        ec = GetComponent<EnemyController>();
    }

    void Start()
    {
        currentCapture = 0f;
    }

    void Update()
    {
        // Si el bool manual está activado, activar stun si no lo está
        if (forceStunned && !isStunned)
        {
            BecomeStunned();
        }

        // Si está stuned, contar tiempo
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
                Recover();

            return; // no decae ni actúa mientras está stuned
        }

        // Si no está al máximo, decae el capture
        if (!forceStunned && currentCapture > 0f)
        {
            currentCapture = Mathf.Max(0f, currentCapture - decayPerSecond * Time.deltaTime);
        }

        // Si llega al máximo → stun automático
        if (!forceStunned && currentCapture >= maxCapture)
        {
            BecomeStunned();
        }
        // Si llega a mitad → evento de stun leve
        else if (!forceStunned && currentCapture >= maxCapture * 0.5f)
        {
            OnStunned?.Invoke();
        }
    }

    /// <summary>
    /// Aplicar incremento de captura
    /// </summary>
    public void ApplyCapture(float amount)
    {
        if (isStunned) return;

        currentCapture += amount;
        currentCapture = Mathf.Clamp(currentCapture, 0f, maxCapture);
    }

    void BecomeStunned()
    {
        if (isStunned) return;

        isStunned = true;
        stunTimer = stunDuration;

        OnFullyStunned?.Invoke();

        // Cambiar estado
        ec?.FSM.ChangeState(new StunnedState());

        // Detener motor
        if (ec.Motor != null)
            ec.Motor.enabled = false;

        Debug.Log($"[{ec.stats.displayName}] Entró en StunnedState. (StunTimer={stunTimer}s)");
    }

    void Recover()
    {
        isStunned = false;
        currentCapture = 0f;
        forceStunned = false;

        OnRecovered?.Invoke();

        // Reactivar motor
        if (ec.Motor != null)
            ec.Motor.enabled = true;

        // Volver a Wander
        ec?.FSM.ChangeState(new WanderState());

        Debug.Log($"[{ec.stats.displayName}] Salió de StunnedState.");
    }

    public bool IsStunned() => isStunned;
}
