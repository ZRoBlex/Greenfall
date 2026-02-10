using UnityEngine;

/// <summary>
/// Puente con Animator ULTRA optimizado
/// - Cache de hashes
/// - Sin Debug.Log en runtime
/// </summary>
[RequireComponent(typeof(Animator))]
public class AnimatorBridge : MonoBehaviour
{
    Animator anim;
    EnemyStats stats;

    // ═══════ CACHE DE HASHES (mucho más rápido) ═══════

    int hashIsScared;
    int hashIsChasing;
    int hashIsIdle;
    int hashIsWalking;

    // ═══════ LIFECYCLE ═══════

    void Awake()
    {
        anim = GetComponent<Animator>();

        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        if (anim == null)
        {
            Debug.LogError($"[AnimatorBridge] No Animator en {name}");
            return;
        }

        EnemyController ec = GetComponent<EnemyController>();
        if (ec != null)
            stats = ec.stats;

        // Pre-calcular hashes (MUCHO más rápido que strings)
        hashIsScared = Animator.StringToHash("IsScared");
        hashIsChasing = Animator.StringToHash("IsChasing");
        hashIsIdle = Animator.StringToHash("IsIdle");
        hashIsWalking = Animator.StringToHash("IsWalking");
    }

    // ═══════ API PÚBLICA ═══════

    public void Play(string animName)
    {
        if (anim == null || string.IsNullOrEmpty(animName)) return;
        anim.Play(animName);
    }

    public void SetBool(string param, bool value)
    {
        if (anim == null) return;

        // Usar hash si es un parámetro común
        int hash = GetHashForParam(param);
        if (hash != 0)
            anim.SetBool(hash, value);
        else
            anim.SetBool(param, value);
    }

    public void SetTrigger(string param)
    {
        if (anim == null) return;
        anim.SetTrigger(param);
    }

    public void SetFloat(string param, float value)
    {
        if (anim == null) return;
        anim.SetFloat(param, value);
    }

    public void ResetSpecialBools()
    {
        if (anim == null) return;

        anim.SetBool(hashIsScared, false);
        anim.SetBool(hashIsChasing, false);
        anim.SetBool(hashIsIdle, false);
    }

    // ═══════ MÉTODOS POR ESTADO ═══════

    public void PlayIdle() { if (stats != null) Play(stats.idleAnim); }
    public void PlayWalk() { if (stats != null) Play(stats.walkAnim); }
    public void PlayChase() { if (stats != null) Play(stats.chaseAnim); }
    public void PlayScared() { if (stats != null) Play(stats.scaredAnim); }
    public void PlayLook() { if (stats != null) Play(stats.lookAnim); }
    public void PlayAttack() { if (stats != null) Play(stats.attackAnim); }

    // ═══════ HELPERS ═══════

    int GetHashForParam(string param)
    {
        switch (param)
        {
            case "IsScared": return hashIsScared;
            case "IsChasing": return hashIsChasing;
            case "IsIdle": return hashIsIdle;
            case "IsWalking": return hashIsWalking;
            default: return 0;
        }
    }
}