using UnityEngine;
using System;

/// <summary>
/// Sistema de captura NO LETAL - Ultra optimizado
/// Compatible con LOD y pooling
/// </summary>
public class NonLethalHealthAdapted : MonoBehaviour
{
    [Header("═══════ CONFIGURACIÓN DE CAPTURA ═══════")]
    public float maxCapture = 100f;
    public float captureDecayPerSecond = 5f;
    public float stunDuration = 30f;

    [Header("═══════ ESTADO ACTUAL ═══════")]
    public float currentCapture;

    [Header("═══════ CONTROL MANUAL ═══════")]
    [Tooltip("Forzar stun manualmente (debug)")]
    public bool forceStunned;

    // ═══════ EVENTOS ═══════
    public event Action OnStunned;           // 50% captura
    public event Action OnFullyStunned;      // 100% captura
    public event Action OnRecovered;         // Recuperado
    public event Action<float> OnCaptureTaken;

    // ═══════ PROPIEDADES ═══════
    public bool CanBeCaptured => enemyController == null || enemyController.CurrentType != CannibalType.Friendly;

    // ═══════ FLAGS ═══════
    bool isStunned;
    float stunTimer;

    // ═══════ CACHE ═══════
    EnemyController enemyController;
    Transform cachedTransform;

    // ═══════ LIFECYCLE ═══════

    void Awake()
    {
        enemyController = GetComponent<EnemyController>();
        cachedTransform = transform;
        currentCapture = 0f;
        isStunned = false;
    }

    void OnEnable()
    {
        currentCapture = 0f;
        isStunned = false;
        forceStunned = false;
        stunTimer = 0f;
    }

    void Update()
    {
        // 🔴 Sleep LOD → No procesar NADA
        if (enemyController != null && enemyController.CurrentLOD == EnemyLOD.Sleep)
            return;

        // Forzar stun manual (debug)
        if (forceStunned && !isStunned)
            BecomeStunned();

        // Si está stunned, contar tiempo
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
                Recover();
            return;
        }

        // Decaimiento de captura
        if (!forceStunned && currentCapture > 0f)
        {
            currentCapture = Mathf.Max(0f, currentCapture - captureDecayPerSecond * Time.deltaTime);
        }
    }

    // ═══════ API PÚBLICA ═══════

    /// <summary>
    /// Aplicar daño de captura
    /// </summary>
    public void ApplyCaptureTick(float amount)
    {
        // No capturar amigos
        if (enemyController != null && enemyController.CurrentType == CannibalType.Friendly)
            return;

        // No capturar en Sleep
        if (enemyController != null && enemyController.CurrentLOD == EnemyLOD.Sleep)
            return;

        if (isStunned || amount <= 0f) return;

        currentCapture += amount;
        currentCapture = Mathf.Clamp(currentCapture, 0f, maxCapture);

        // 100% → Stun completo
        if (currentCapture >= maxCapture)
        {
            BecomeStunned();
        }
        // 50% → Evento de medio stun
        else if (currentCapture >= maxCapture * 0.5f)
        {
            OnStunned?.Invoke();
        }

        OnCaptureTaken?.Invoke(amount);
    }

    public bool IsStunned() => isStunned;

    public float GetCapturePercent() => maxCapture > 0 ? currentCapture / maxCapture : 0f;

    // Compatibilidad con código antiguo
    public void KnockOut() => BecomeStunned();
    public void Revive() => Recover();

    public void ResetHealth()
    {
        currentCapture = 0f;
        forceStunned = false;

        if (isStunned)
        {
            isStunned = false;
            stunTimer = 0f;

            // Reactivar motor
            if (enemyController != null && enemyController.Motor != null)
                enemyController.Motor.enabled = true;
        }
    }

    // ═══════ STUN SYSTEM ═══════

    void BecomeStunned()
    {
        // No permitir stun en Sleep
        if (enemyController != null && enemyController.CurrentLOD == EnemyLOD.Sleep)
            return;

        if (isStunned) return;

        isStunned = true;
        stunTimer = stunDuration;

        OnFullyStunned?.Invoke();

        // Cambiar a StunnedState
        if (enemyController != null && enemyController.FSM != null)
            enemyController.FSM.ChangeState(new StunnedState());

        // Detener motor
        if (enemyController != null && enemyController.Motor != null)
            enemyController.Motor.enabled = false;
    }

    void Recover()
    {
        // No recuperar en Sleep
        if (enemyController != null && enemyController.CurrentLOD == EnemyLOD.Sleep)
            return;

        isStunned = false;
        currentCapture = 0f;
        forceStunned = false;

        OnRecovered?.Invoke();

        // Reactivar motor
        if (enemyController != null && enemyController.Motor != null)
            enemyController.Motor.enabled = true;

        // Volver a Wander
        if (enemyController != null && enemyController.FSM != null)
            enemyController.FSM.ChangeState(new WanderState());
    }
}