using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ProfessionController : MonoBehaviour
{
    [Header("Profesión Asignada")]
    public Profession profession;          // Profesión asignada
    public GameObject currentModel;        // Modelo instanciado
    public Animator anim;                  // Animator del modelo
    bool isActive = false;                 // Marca si la profesión está activa

    void Awake()
    {
        if (profession == null)
        {
            Debug.LogWarning($"[ProfessionController] No se asignó ninguna profesión en {name}");
            return;
        }

        SetupModel();
    }

    /// <summary>
    /// Instancia el modelo y asigna Animator
    /// </summary>
    public void SetupModel()
    {
        if (profession.modelPrefab != null)
        {
            currentModel = Instantiate(profession.modelPrefab, transform);
            anim = currentModel.GetComponent<Animator>();
            if (anim == null)
            {
                Debug.LogWarning($"[ProfessionController] No se encontró Animator en el modelo de {profession.displayName}");
            }
            else if (profession.animatorController != null)
            {
                anim.runtimeAnimatorController = profession.animatorController;
            }
        }
        else
        {
            Debug.LogWarning($"[ProfessionController] No hay prefab asignado para la profesión {profession.displayName}");
        }
    }

    /// <summary>
    /// Activa la profesión, sobrescribiendo valores si se desea.
    /// Esto se llama cuando el enemigo cambia al equipo del jugador.
    /// </summary>
    public void ActivateProfession(EnemyController enemy)
    {
        if (profession == null || isActive) return;

        isActive = true;

        // Los valores de movimiento, ataque y rango se siguen usando desde EnemyStats
        // Solo aquí podrías sobrescribirlos si quieres que la profesión tenga prioridad:
        // enemy.stats.moveSpeed = profession.baseMoveSpeed;
        // enemy.stats.attackDamage = profession.baseAttackDamage;

        Debug.Log($"[ProfessionController] Profesión {profession.displayName} activada en {enemy.name}");
    }

    // -----------------------------
    // MÉTODOS DE ANIMACIÓN
    // -----------------------------
    public void PlayAttack()
    {
        if (anim == null || string.IsNullOrEmpty(profession.attackAnim)) return;
        Debug.Log($"[ProfessionController] {profession.displayName} ataca con {profession.attackAnim}");
        // anim.Play(profession.attackAnim);
    }

    public void PlayWalk()
    {
        if (anim == null || string.IsNullOrEmpty(profession.walkAnim)) return;
        Debug.Log($"[ProfessionController] {profession.displayName} camina con {profession.walkAnim}");
        // anim.Play(profession.walkAnim);
    }

    public void PlayIdle()
    {
        if (anim == null || string.IsNullOrEmpty(profession.idleAnim)) return;
        Debug.Log($"[ProfessionController] {profession.displayName} está idle con {profession.idleAnim}");
        // anim.Play(profession.idleAnim);
    }

    public void PlaySpecial()
    {
        if (anim == null || string.IsNullOrEmpty(profession.specialAnim)) return;
        Debug.Log($"[ProfessionController] {profession.displayName} hace animación especial {profession.specialAnim}");
        // anim.Play(profession.specialAnim);
    }

    // -----------------------------
    // MÉTODOS DE ATRIBUTOS (opcional, leer valores sin tocar EnemyStats)
    // -----------------------------
    // Aquí puedes agregar métodos si quieres que la profesión tenga atributos propios
    // pero por ahora, EnemyStats sigue controlando movimiento, ataque, rango, etc.
}
