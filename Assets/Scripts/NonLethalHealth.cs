using UnityEngine;
using System;

[RequireComponent(typeof(EnemyController))]
public class NonLethalHealth : MonoBehaviour
{
    [Header("Capture Settings")]
    public float maxCapture = 100f;
    public float currentCapture = 0f;
    public float decayPerSecond = 5f;
    public float unconsciousDuration = 30f;

    [Header("Manual Control")]
    [Tooltip("Forzar al enemigo a estar stuned manualmente")]
    public bool forceStunned = false;

    // Eventos
    public event Action OnStunned;
    public event Action OnFullyStunned;
    public event Action OnRecovered;
    public event Action<float> OnCaptureTaken;

    // Internos
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
        // Forzar stun manual
        if (forceStunned && !isStunned)
            BecomeStunned();

        // Si está stun, contar tiempo
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
                Recover();
            return; // no decae ni actúa mientras está stun
        }

        // Decaimiento de captura
        if (!forceStunned && currentCapture > 0f)
        {
            currentCapture = Mathf.Max(0f, currentCapture - decayPerSecond * Time.deltaTime);
        }

        // Captura máxima → stun automático
        if (!forceStunned && currentCapture >= maxCapture)
        {
            BecomeStunned();
        }
        // Captura a mitad → evento leve
        else if (!forceStunned && currentCapture >= maxCapture * 0.5f)
        {
            OnStunned?.Invoke();
        }
    }

    /// <summary>
    /// Aplicar incremento de captura, solo si es captureDamage
    /// </summary>
    public void ApplyCaptureTick(float amount, bool isCaptureDamage = true)
    {
        if (!isCaptureDamage || isStunned) return; // Ignorar si no es captura o está stun

        currentCapture += amount;
        currentCapture = Mathf.Clamp(currentCapture, 0f, maxCapture);

        if (currentCapture >= maxCapture)
        {
            BecomeStunned();
        }
        else if (currentCapture >= maxCapture * 0.5f)
        {
            OnStunned?.Invoke();
        }

        OnCaptureTaken?.Invoke(amount);
    }

    void BecomeStunned()
    {
        if (isStunned) return;

        isStunned = true;
        stunTimer = unconsciousDuration;

        OnFullyStunned?.Invoke();

        // Cambiar estado a StunnedState
        ec?.FSM.ChangeState(new StunnedState());

        // Detener motor mientras está stun
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

        // Volver a WanderState
        ec?.FSM.ChangeState(new WanderState());

        Debug.Log($"[{ec.stats.displayName}] Salió de StunnedState.");
    }

    public bool IsStunned() => isStunned;

    // Compatibilidad scripts antiguos
    public void KnockOut() => BecomeStunned();
    public void Revive() => Recover();
}
