using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimatorBridge : MonoBehaviour
{
    Animator anim;
    EnemyStats stats; // Tomamos las animaciones desde EnemyStats

    void Awake()
    {
        // 1. Primero intenta en este mismo objeto
        anim = GetComponent<Animator>();

        // 2. Si no existe, búscalo en hijos
        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        if (anim == null)
            Debug.LogError("[AnimatorBridge] No se encontró Animator en " + name + " ni en sus hijos");

        // Tomar EnemyStats desde EnemyController del mismo objeto
        EnemyController ec = GetComponent<EnemyController>();
        if (ec != null)
            stats = ec.stats;

        if (stats == null)
            Debug.LogWarning("[AnimatorBridge] No se encontró EnemyStats en " + name);
    }


    // ---------------------------
    // MÉTODOS GENERALES
    // ---------------------------

    public void Play(string animName)
    {
        if (anim == null || string.IsNullOrEmpty(animName)) return;

        // Para pruebas sin animaciones, mostramos debug
        Debug.Log($"[AnimatorBridge] Reproduciendo animación: {animName}");

        // Cuando tengas animaciones reales, descomenta:
         anim.Play(animName);
    }

    public void SetBool(string param, bool value)
    {
        if (anim == null) return;
        anim.SetBool(param, value);
        Debug.Log($"[AnimatorBridge] SetBool({param}, {value})");
    }

    public void SetTrigger(string param)
    {
        if (anim == null) return;
        anim.SetTrigger(param);
        Debug.Log($"[AnimatorBridge] SetTrigger({param})");
    }

    public void SetFloat(string param, float value)
    {
        if (anim == null) return;
        anim.SetFloat(param, value);
        Debug.Log($"[AnimatorBridge] SetFloat({param}, {value})");
    }

    // ---------------------------
    // MÉTODOS POR ESTADO
    // ---------------------------

    public void PlayIdle() { if (stats != null) Play(stats.idleAnim); }
    public void PlayWalk() { if (stats != null) Play(stats.walkAnim); }
    public void PlayChase() { if (stats != null) Play(stats.chaseAnim); }
    public void PlayScared() { if (stats != null) Play(stats.scaredAnim); }
    public void PlayLook() { if (stats != null) Play(stats.lookAnim); }
    public void PlayAttack() { if (stats != null) Play(stats.attackAnim); }
}
