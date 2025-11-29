using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimatorBridge : MonoBehaviour
{
    Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    // ---------------------------
    // GENERALES
    // ---------------------------
    public void SetSpeed(float value)
    {
        if (anim == null) return;
        anim.SetFloat("Speed", value);
    }

    public void SetBool(string param, bool value)
    {
        if (anim == null) return;
        anim.SetBool(param, value);
    }

    public void SetTrigger(string param)
    {
        if (anim == null) return;
        anim.SetTrigger(param);
    }

    // ----------------------------------------
    //  FLOAT (mantengo por compatibilidad)
    // ----------------------------------------
    public void SetFloat(string param, float value)
    {
        if (anim == null) return;
        anim.SetFloat(param, value);
    }

    // ---------------------------
    // ATAQUE
    // ---------------------------
    public void TriggerAttack()
    {
        if (anim == null) return;
        anim.SetTrigger("Attack");
    }

    // ---------------------------
    // ATAJOS ÚTILES (bools dedicados)
    // ---------------------------

    // Activa la animación de caminar (IsWalking)
    public void SetWalking(bool value)
    {
        if (anim == null) return;
        anim.SetBool("IsWalking", value);

        // para compatibilidad con transiciones antiguas:
        // cuando está caminando no es idle
        if (value) anim.SetBool("IsIdle", false);
    }

    // Activa la animación Idle
    public void SetIdle(bool value)
    {
        if (anim == null) return;
        anim.SetBool("IsIdle", value);

        // cuando idle, no está caminando ni persiguiendo ni asustado
        if (value)
        {
            anim.SetBool("IsWalking", false);
            anim.SetBool("IsChasing", false);
            anim.SetBool("IsScared", false);
        }
    }

    // Animación de persecución
    public void SetChasing(bool value)
    {
        if (anim == null) return;
        anim.SetBool("IsChasing", value);

        if (value)
        {
            // priorizar chase
            anim.SetBool("IsWalking", false);
            anim.SetBool("IsIdle", false);
            anim.SetBool("IsScared", false);
        }
    }

    // Animación de asustado
    public void SetScared(bool value)
    {
        if (anim == null) return;
        anim.SetBool("IsScared", value);

        if (value)
        {
            // cuando asustado, dejar otras flags que puedan romper transiciones
            anim.SetBool("IsWalking", false);
            anim.SetBool("IsIdle", false);
            anim.SetBool("IsChasing", false);
        }
    }
}
