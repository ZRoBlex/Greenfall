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

            if (unconsciousTimer <= 0f)
                Recover();

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

        // Pone al enemigo en animación de knockout / estado KO
        ec?.KnockOut();
    }

    void Recover()
    {
        isUnconscious = false;
        currentCapture = 0f;

        OnRecovered?.Invoke();

        if (ec == null)
            return;

        // Si es un aliado reclutado, no lo enviamos a patrullar
        if (ec.IsRecruited)
            return;

        // Volver AL ESTADO NORMAL
        // Igual que cuando deja de ver al jugador
        ec.ChangeState(new WanderState());
    }

    public bool IsUnconscious() => isUnconscious;
}
