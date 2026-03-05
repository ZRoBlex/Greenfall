// ============================================================
// WorkerNeeds.cs
// ============================================================
// Simula las necesidades vitales del trabajador según el GDD:
//   Hunger    → baja con el tiempo; 0 = muerte
//   Rest      → baja al trabajar; sube al descansar
//   Obedience → qué tan bien sigue órdenes. Sube con buena
//               atención, baja con hambre / daño / trauma
//   Morale    → estado de ánimo grupal. Muy bajo = riesgo
//               de rebelión (preparado para sistema futuro)
//
// INTEGRACIÓN:
//   WorkerController llama Tick(dt) en su Update.
//   CommunityManager escucha eventos para UI y alertas.
// ============================================================

using System;
using UnityEngine;

public class WorkerNeeds : MonoBehaviour
{
    // ─── Valores actuales ─────────────────────────────────────
    [Header("Valores actuales")]
    [SerializeField, Range(0f, 100f)] float _hunger    = 80f;
    [SerializeField, Range(0f, 100f)] float _rest      = 100f;
    [SerializeField, Range(0f, 100f)] float _obedience = 50f;
    [SerializeField, Range(0f, 100f)] float _morale    = 60f;

    // ─── Tasas por segundo de juego ───────────────────────────
    [Header("Tasas (por segundo)")]
    [SerializeField] float hungerDecayRate    = 0.5f;  // hambre cae X/s
    [SerializeField] float restDecayAtWork    = 0.3f;  // descanso cae al trabajar
    [SerializeField] float restRecoveryRate   = 1.2f;  // descanso sube al descansar
    [SerializeField] float obedienceGainRate  = 0.08f; // sube si está bien
    [SerializeField] float obedienceDecayRate = 0.35f; // baja si tiene hambre/cansancio

    // ─── Umbrales ─────────────────────────────────────────────
    [Header("Umbrales de alerta")]
    [SerializeField] float hungerCritical    = 20f;
    [SerializeField] float restCritical      = 15f;
    [SerializeField] float rebellionThreshold = 10f;

    // ─── Eventos ──────────────────────────────────────────────
    public event Action OnHungerCritical;  // hambre < umbral
    public event Action OnRestCritical;    // descanso < umbral
    public event Action OnRebellionRisk;   // moral < umbral
    public event Action OnWorkerDied;      // hambre = 0
    public event Action OnNeedsChanged;    // cualquier cambio (para UI)

    // ─── Estado interno ───────────────────────────────────────
    bool  _isWorking;
    bool  _isDead;
    bool  _hungerAlertSent;
    bool  _restAlertSent;
    bool  _rebellionAlertSent;
    float _foodMult = 1f;  // viene de ProfessionSO.foodConsumptionRate

    // ─── Propiedades públicas ─────────────────────────────────
    public float Hunger    => _hunger;
    public float Rest      => _rest;
    public float Obedience => _obedience;
    public float Morale    => _morale;
    public bool  IsHungry  => _hunger    < hungerCritical;
    public bool  IsTired   => _rest      < restCritical;
    public bool  IsDead    => _isDead;

    // ─── API ──────────────────────────────────────────────────

    /// <summary>Llamar desde WorkerController cada frame.</summary>
    public void Tick(float dt)
    {
        if (_isDead) return;

        TickHunger(dt);
        TickRest(dt);
        TickObedience(dt);
        TickMorale(dt);
        CheckAlerts();

        OnNeedsChanged?.Invoke();
    }

    public void SetWorking(bool v)             => _isWorking = v;
    public void SetFoodMultiplier(float mult)  => _foodMult  = Mathf.Max(0.01f, mult);

    /// <summary>Alimentar al trabajador. foodAmount en unidades de ración (1 ración = +20 hambre).</summary>
    public void Feed(float foodAmount)
    {
        if (_isDead) return;
        _hunger = Mathf.Clamp(_hunger + foodAmount * 20f, 0f, 100f);
        _hungerAlertSent = false;  // puede volver a avisar si baja de nuevo
        OnNeedsChanged?.Invoke();
    }

    /// <summary>Aplicar cambio directo de obediencia (recompensa +, castigo -).</summary>
    public void ModifyObedience(float delta)
    {
        _obedience = Mathf.Clamp(_obedience + delta, 0f, 100f);
        OnNeedsChanged?.Invoke();
    }

    /// <summary>Aplicar cambio directo de moral (evento grupal, etc.).</summary>
    public void ModifyMorale(float delta)
    {
        _morale = Mathf.Clamp(_morale + delta, 0f, 100f);
        if (_morale >= rebellionThreshold + 10f)
            _rebellionAlertSent = false;
        OnNeedsChanged?.Invoke();
    }

    /// <summary>Reinicia las necesidades para un caníbal recién capturado.</summary>
    public void ResetForNewWorker()
    {
        _hunger              = 50f;
        _rest                = 100f;
        _obedience           = 25f;  // bajo: acaba de ser capturado
        _morale              = 40f;
        _isDead              = false;
        _hungerAlertSent     = false;
        _restAlertSent       = false;
        _rebellionAlertSent  = false;
        _isWorking           = false;
    }

    // ─── Ticks internos ───────────────────────────────────────

    void TickHunger(float dt)
    {
        _hunger = Mathf.Clamp(_hunger - hungerDecayRate * _foodMult * dt, 0f, 100f);

        if (_hunger <= 0f && !_isDead)
        {
            _isDead = true;
            OnWorkerDied?.Invoke();
        }
    }

    void TickRest(float dt)
    {
        if (_isWorking)
            _rest = Mathf.Clamp(_rest - restDecayAtWork * dt, 0f, 100f);
        else
            _rest = Mathf.Clamp(_rest + restRecoveryRate * dt, 0f, 100f);
    }

    void TickObedience(float dt)
    {
        bool comfortable = !IsHungry && !IsTired;
        float rate = comfortable ? obedienceGainRate : -obedienceDecayRate;
        _obedience = Mathf.Clamp(_obedience + rate * dt, 0f, 100f);
    }

    void TickMorale(float dt)
    {
        // La moral converge lentamente hacia obediencia*0.7 + hambre*0.3
        float target = _obedience * 0.7f + _hunger * 0.3f;
        _morale = Mathf.Lerp(_morale, target, dt * 0.04f);
    }

    void CheckAlerts()
    {
        if (_hunger < hungerCritical && !_hungerAlertSent)
        {
            _hungerAlertSent = true;
            OnHungerCritical?.Invoke();
        }

        if (_rest < restCritical && !_restAlertSent)
        {
            _restAlertSent = true;
            OnRestCritical?.Invoke();
        }

        if (_morale < rebellionThreshold && !_rebellionAlertSent)
        {
            _rebellionAlertSent = true;
            OnRebellionRisk?.Invoke();
        }
    }
}
