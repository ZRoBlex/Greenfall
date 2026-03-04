// ============================================================
// WeaponSwitchAnimator.cs — ANIMACIÓN DE CAMBIO DE ARMA
// ============================================================
// PROPÓSITO:
//   Anima el weaponHolder hacia abajo (holster) cuando el jugador
//   cambia de arma, ejecuta el swap real, y luego sube de nuevo
//   (draw). También impone un cooldown para evitar spam de scroll.
//
// DÓNDE COLOCAR ESTE COMPONENTE:
//   En el mismo GameObject que WeaponInventory (el jugador).
//   Necesita una referencia al weaponHolder (el Transform que
//   contiene las armas).
//
// FLUJO:
//   1. WeaponInventory llama RequestSwitch(targetIndex).
//   2. Si hay animación en curso o en cooldown → ignorado.
//   3. Fase HOLSTER: el weaponHolder baja y se inclina.
//   4. Al llegar al punto más bajo → callback onSwitch() → arma real cambia.
//   5. Fase DRAW: el weaponHolder sube de vuelta.
//   6. Al terminar → cooldown timer arranca.
//
// INTEGRACIÓN CON WEAPONINVENTORY:
//   WeaponInventory llama:
//     switchAnimator.RequestSwitch(index, () => DoEquip(index));
//   En lugar de llamar Equip() directamente.
//
// INTEGRACIÓN CON SWAY:
//   El sway se suprime durante la animación para no interferir.
// ============================================================

using System;
using UnityEngine;

public class WeaponSwitchAnimator : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // CONFIGURACIÓN
    // ─────────────────────────────────────────────────────────

    [Header("WeaponHolder")]
    [Tooltip("El Transform que contiene las armas. Mismo que weaponHolder en WeaponInventory.")]
    [SerializeField] Transform weaponHolder;

    [Header("Holster — bajar arma")]
    [Tooltip("Cuánto baja el weaponHolder en Y al holstear.")]
    [SerializeField] float holsterDropY     = -0.35f;

    [Tooltip("Inclinación en X al bajar el arma (se inclina hacia adelante).")]
    [SerializeField] float holsterTiltX     = 15f;

    [Tooltip("Velocidad de la bajada. Más alto = más rápido.")]
    [SerializeField] float holsterSpeed     = 12f;

    [Header("Draw — subir arma")]
    [Tooltip("Velocidad de la subida al equipar el nuevo arma.")]
    [SerializeField] float drawSpeed        = 14f;

    [Header("Cooldown")]
    [Tooltip("Tiempo de espera después de terminar el draw antes de poder cambiar de nuevo.")]
    [SerializeField] float switchCooldown   = 0.25f;

    [Header("Referencias opcionales")]
    [Tooltip("Para suprimir el sway durante el cambio.")]
    [SerializeField] SwayController swayController;

    // ─────────────────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────────────────

    enum SwitchState { Idle, Holstering, Drawing }
    SwitchState _state = SwitchState.Idle;

    // Posición y rotación BASE del weaponHolder (sin animación de switch)
    Vector3    _basePos;
    Quaternion _baseRot;
    bool       _baseInitialized;

    // Posición/rotación target para cada fase
    Vector3    _holsterPos;
    Quaternion _holsterRot;

    // Callback para ejecutar el swap real de arma
    Action _onSwitchCallback;

    // Cooldown timer
    float _cooldownTimer;

    // ─────────────────────────────────────────────────────────
    // PROPIEDADES PÚBLICAS
    // ─────────────────────────────────────────────────────────

    /// <summary>True si hay una animación en curso o en cooldown.</summary>
    public bool IsBusy => _state != SwitchState.Idle || _cooldownTimer > 0f;

    // ─────────────────────────────────────────────────────────
    // INIT
    // ─────────────────────────────────────────────────────────

    void Start()
    {
        if (weaponHolder == null)
        {
            Debug.LogError("[WeaponSwitchAnimator] weaponHolder no asignado.");
            return;
        }

        CacheBase();
    }

    void CacheBase()
    {
        _basePos         = weaponHolder.localPosition;
        _baseRot         = weaponHolder.localRotation;
        _holsterPos      = _basePos + new Vector3(0f, holsterDropY, 0f);
        _holsterRot      = _baseRot * Quaternion.Euler(holsterTiltX, 0f, 0f);
        _baseInitialized = true;
    }

    // ─────────────────────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Solicitar un cambio de arma con animación.
    /// onSwitch: callback que se ejecuta al punto más bajo
    ///           (aquí WeaponInventory hace el swap real).
    /// Retorna false si está ocupado (en animación o cooldown).
    /// </summary>
    public bool RequestSwitch(Action onSwitch)
    {
        if (IsBusy) return false;
        if (!_baseInitialized) CacheBase();

        _onSwitchCallback = onSwitch;
        StartHolster();
        return true;
    }

    /// <summary>
    /// Cambio instantáneo sin animación (para cuando se dropea
    /// un arma y el juego necesita equipar la siguiente de golpe).
    /// </summary>
    public void ForceInstantSwitch(Action onSwitch)
    {
        _state        = SwitchState.Idle;
        _cooldownTimer = 0f;
        onSwitch?.Invoke();
        if (_baseInitialized)
        {
            weaponHolder.localPosition = _basePos;
            weaponHolder.localRotation = _baseRot;
        }
    }

    // ─────────────────────────────────────────────────────────
    // FASES DE ANIMACIÓN
    // ─────────────────────────────────────────────────────────

    void StartHolster()
    {
        _state = SwitchState.Holstering;
        swayController?.SetSwitching(true);
    }

    void StartDraw()
    {
        _state = SwitchState.Drawing;
        // Resetear posición al punto de holster para subir desde ahí
        weaponHolder.localPosition = _holsterPos;
        weaponHolder.localRotation = _holsterRot;
    }

    // ─────────────────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────────────────

    void Update()
    {
        if (!_baseInitialized) return;

        // Cooldown
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        switch (_state)
        {
            case SwitchState.Holstering:
                UpdateHolster();
                break;

            case SwitchState.Drawing:
                UpdateDraw();
                break;
        }
    }

    void UpdateHolster()
    {
        float dt = Time.deltaTime;

        // Mover hacia la posición de holster
        weaponHolder.localPosition = Vector3.Lerp(
            weaponHolder.localPosition, _holsterPos, holsterSpeed * dt);

        weaponHolder.localRotation = Quaternion.Slerp(
            weaponHolder.localRotation, _holsterRot, holsterSpeed * dt);

        // ¿Llegamos suficientemente abajo?
        float dist = Vector3.Distance(weaponHolder.localPosition, _holsterPos);
        if (dist < 0.015f)
        {
            // Ejecutar el swap real de arma
            _onSwitchCallback?.Invoke();
            _onSwitchCallback = null;

            // Ir a fase Draw
            StartDraw();
        }
    }

    void UpdateDraw()
    {
        float dt = Time.deltaTime;

        // Mover de vuelta a la posición base
        weaponHolder.localPosition = Vector3.Lerp(
            weaponHolder.localPosition, _basePos, drawSpeed * dt);

        weaponHolder.localRotation = Quaternion.Slerp(
            weaponHolder.localRotation, _baseRot, drawSpeed * dt);

        // ¿Llegamos suficientemente arriba?
        float dist = Vector3.Distance(weaponHolder.localPosition, _basePos);
        if (dist < 0.008f)
        {
            // Snap final a posición exacta
            weaponHolder.localPosition = _basePos;
            weaponHolder.localRotation = _baseRot;

            _state         = SwitchState.Idle;
            _cooldownTimer = switchCooldown;

            swayController?.SetSwitching(false);
        }
    }

    // ─────────────────────────────────────────────────────────
    // GIZMOS (visualizar la posición de holster en Editor)
    // ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (weaponHolder == null) return;

        Vector3 holsterWorldPos = weaponHolder.parent != null
            ? weaponHolder.parent.TransformPoint(weaponHolder.localPosition + new Vector3(0f, holsterDropY, 0f))
            : weaponHolder.position + new Vector3(0f, holsterDropY, 0f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(holsterWorldPos, 0.05f);
        Gizmos.DrawLine(weaponHolder.position, holsterWorldPos);
    }
#endif
}
