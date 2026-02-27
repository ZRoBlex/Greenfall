using UnityEngine;

/// <summary>
/// NonLethalHealth Adapter for Optimized System
/// Drop-in replacement that works with OptimizedEnemyController
/// </summary>
[RequireComponent(typeof(OptimizedEnemyController))]
public class NonLethalHealthAdapted : MonoBehaviour
{
    [Header("Capture Settings")]
    public float maxCapture = 100f;
    public float currentCapture = 0f;
    public float decayPerSecond = 5f;
    public float unconsciousDuration = 30f;

    [Header("Manual Control")]
    public bool forceStunned = false;

    // Events
    public event System.Action OnStunned;
    public event System.Action OnFullyStunned;
    public event System.Action OnRecovered;
    public event System.Action<float> OnCaptureTaken;

    public bool CanBeCaptured => ec == null || ec.CurrentType != CannibalType.Friendly;

    // Internos
    OptimizedEnemyController ec;
    bool isStunned = false;
    float stunTimer = 0f;

    void Awake()
    {
        ec = GetComponent<OptimizedEnemyController>();
    }

    void Start()
    {
        currentCapture = 0f;
    }

    void Update()
    {
        // Sleep LOD check
        if (ec != null && ec.CurrentLOD == EnemyLOD.Sleep)
            return;

        if (forceStunned && !isStunned)
            BecomeStunned();

        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
                Recover();
            return;
        }

        if (!forceStunned && currentCapture > 0f)
        {
            currentCapture = Mathf.Max(0f, currentCapture - decayPerSecond * Time.deltaTime);
        }
    }

    public void ApplyCaptureTick(float amount)
    {
        if (ec != null && ec.CurrentType == CannibalType.Friendly)
            return;

        if (ec != null && ec.CurrentLOD == EnemyLOD.Sleep)
            return;

        if (isStunned) return;

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
        if (ec != null && ec.CurrentLOD == EnemyLOD.Sleep)
            return;

        if (isStunned) return;

        isStunned = true;
        stunTimer = unconsciousDuration;

        OnFullyStunned?.Invoke();

        // Change to StunnedState
        ec?.FSM.ChangeState(new StunnedState());

        if (ec.Motor != null)
            ec.Motor.enabled = false;

        Debug.Log($"[{ec.stats.displayName}] Stunned (Timer={stunTimer}s)");
    }

    void Recover()
    {
        if (ec != null && ec.CurrentLOD == EnemyLOD.Sleep)
            return;

        isStunned = false;
        currentCapture = 0f;
        forceStunned = false;

        OnRecovered?.Invoke();

        if (ec.Motor != null)
            ec.Motor.enabled = true;

        ec?.FSM.ChangeState(new OptimizedWanderState());

        Debug.Log($"[{ec.stats.displayName}] Recovered from stun");
    }

    public bool IsStunned() => isStunned;

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

            if (ec != null && ec.Motor != null)
                ec.Motor.enabled = true;
        }
    }
}
