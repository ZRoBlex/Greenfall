using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ProfessionController1 : MonoBehaviour
{
    [Header("Profesi�n Asignada")]
    public Profession profession;          // Profesi�n asignada
    public GameObject currentModel;        // Modelo instanciado
    public Animator anim;                  // Animator del modelo
    bool isActive = false;                 // Marca si la profesi�n est� activa

    void Awake()
    {
        if (profession == null)
        {
            Debug.LogWarning($"[ProfessionController] No se asign� ninguna profesi�n en {name}");
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
                Debug.LogWarning($"[ProfessionController] No se encontr� Animator en el modelo de {profession.displayName}");
            }
            else if (profession.animatorController != null)
            {
                anim.runtimeAnimatorController = profession.animatorController;
            }
        }
        else
        {
            Debug.LogWarning($"[ProfessionController] No hay prefab asignado para la profesi�n {profession.displayName}");
        }
    }

    /// <summary>
    /// Activa la profesi�n, sobrescribiendo valores si se desea.
    /// Esto se llama cuando el enemigo cambia al equipo del jugador.
    /// </summary>
    public void ActivateProfession(EnemyController enemy)
    {
        if (profession == null || isActive) return;

        isActive = true;

        // Los valores de movimiento, ataque y rango se siguen usando desde EnemyStats
        // Solo aqu� podr�as sobrescribirlos si quieres que la profesi�n tenga prioridad:
        // enemy.stats.moveSpeed = profession.baseMoveSpeed;
        // enemy.stats.attackDamage = profession.baseAttackDamage;

        Debug.Log($"[ProfessionController] Profesi�n {profession.displayName} activada en {enemy.name}");
    }

    // -----------------------------
    // M�TODOS DE ANIMACI�N
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
        Debug.Log($"[ProfessionController] {profession.displayName} est� idle con {profession.idleAnim}");
        // anim.Play(profession.idleAnim);
    }

    public void PlaySpecial()
    {
        if (anim == null || string.IsNullOrEmpty(profession.specialAnim)) return;
        Debug.Log($"[ProfessionController] {profession.displayName} hace animaci�n especial {profession.specialAnim}");
        // anim.Play(profession.specialAnim);
    }

    // -----------------------------
    // M�TODOS DE ATRIBUTOS (opcional, leer valores sin tocar EnemyStats)
    // -----------------------------
    // Aqu� puedes agregar m�todos si quieres que la profesi�n tenga atributos propios
    // pero por ahora, EnemyStats sigue controlando movimiento, ataque, rango, etc.
}
