using UnityEngine;

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
        anim.SetFloat("Speed", value);
    }

    public void SetBool(string param, bool value)
    {
        anim.SetBool(param, value);
    }

    public void SetTrigger(string param)
    {
        anim.SetTrigger(param);
    }

    // ----------------------------------------
    //  FLOAT
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
        anim.SetTrigger("Attack");
    }

    // ---------------------------
    // ATAJOS ÚTILES (opcional)
    // ---------------------------
    public void SetChasing(bool value)
    {
        anim.SetBool("IsChasing", value);
    }

    public void SetScared(bool value)
    {
        anim.SetBool("IsScared", value);
    }
}
