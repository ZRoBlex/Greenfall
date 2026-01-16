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

    EnemyController ec;

    public event Action<float> OnCaptureTaken;


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

        OnCaptureTaken?.Invoke(amount);

    }

    void BecomeUnconscious()
    {
        isUnconscious = true;
        unconsciousTimer = unconsciousDuration;
        OnBecameUnconscious?.Invoke();
        ec?.KnockOut();
    }

    void Recover()
    {
        isUnconscious = false;
        currentCapture = 0f;
        OnRecovered?.Invoke();

        if (ec == null) return;

        // Si está reclutado, no lo forzamos a patrullar
        if (ec.IsRecruited) return;

        // -> Llamar al método público Revive para restaurar estado y reactivar sistemas
        ec.Revive();
    }

    public bool IsUnconscious() => isUnconscious;
}
