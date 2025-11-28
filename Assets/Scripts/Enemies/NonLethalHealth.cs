using UnityEngine;
using System;

[RequireComponent(typeof(EnemyController))]
public class NonLethalHealth : MonoBehaviour
{
    public float maxCapture = 100f;
    public float currentCapture = 0f;
    public float decayPerSecond = 5f;
    public float unconsciousDuration = 30f;

    public event Action OnStunned;
    public event Action OnBecameUnconscious;
    public event Action OnRecovered;

    bool isUnconscious = false;
    float unconsciousTimer = 0f;

    void Start()
    {
        currentCapture = 0f;
    }

    void Update()
    {
        if (isUnconscious)
        {
            unconsciousTimer -= Time.deltaTime;
            if (unconsciousTimer <= 0f) Recover();
            return;
        }

        if (currentCapture > 0f)
        {
            currentCapture = Mathf.Max(0f, currentCapture - decayPerSecond * Time.deltaTime);
        }
    }

    public void ApplyCaptureTick(float amount)
    {
        if (isUnconscious) return;

        currentCapture += amount;
        currentCapture = Mathf.Clamp(currentCapture, 0f, maxCapture);

        if (currentCapture >= maxCapture)
        {
            BecomeUnconscious();
        }
        else if (currentCapture >= maxCapture * 0.5f)
        {
            OnStunned?.Invoke();
        }
    }

    void BecomeUnconscious()
    {
        isUnconscious = true;
        unconsciousTimer = unconsciousDuration;
        OnBecameUnconscious?.Invoke();
        var ec = GetComponent<EnemyController>();
        ec?.KnockOut();
    }

    void Recover()
    {
        isUnconscious = false;
        currentCapture = 0f;
        OnRecovered?.Invoke();
        // maybe restore AI
        var ec = GetComponent<EnemyController>();
        if (ec != null && !ec.IsRecruited)
            ec.ChangeState(new WanderState());
    }

    public bool IsUnconscious() => isUnconscious;
}
