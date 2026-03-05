// ============================================================
// WorkerSkinController.cs
// ============================================================
// Controla el modelo visual del trabajador.
// Cuando se asigna una profesión, DESTRUYE el modelo actual
// e INSTANCIA el skin del nuevo ProfessionSO.
//
// JERARQUÍA ESPERADA:
//   WorkerRoot              ← WorkerController, WorkerNeeds, WorkerSkinController
//   └── SkinRoot            ← _skinRoot (se crea aquí)
//       └── [ModelPrefab]   ← instanciado desde ProfessionSO.skinPrefab
//
// POOLING:
//   No destruye el GO raíz del trabajador — solo el modelo hijo.
//   Así el pool puede reutilizar el mismo WorkerRoot.
// ============================================================

using UnityEngine;

public class WorkerSkinController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Transform donde se crea el hijo con el modelo 3D. " +
             "Si es null, se usa este mismo transform.")]
    [SerializeField] Transform skinParent;

    // ─── Estado ───────────────────────────────────────────────
    GameObject    _currentSkinGO;
    Animator      _animator;
    ProfessionSO  _currentProfession;

    // ─── Propiedades públicas ─────────────────────────────────
    public Animator      Animator           => _animator;
    public ProfessionSO  CurrentProfession  => _currentProfession;

    void Awake()
    {
        if (skinParent == null) skinParent = transform;
    }

    // ─── API principal ────────────────────────────────────────

    /// <summary>
    /// Aplica la skin de una profesión.
    /// Destruye el modelo anterior e instancia el nuevo.
    /// Si profession es null o su skinPrefab es null,
    /// deja el trabajador sin modelo visible (rehabilitación en curso).
    /// </summary>
    public void ApplySkin(ProfessionSO profession)
    {
        _currentProfession = profession;

        // Destruir modelo anterior
        if (_currentSkinGO != null)
        {
            Destroy(_currentSkinGO);
            _currentSkinGO = null;
            _animator      = null;
        }

        if (profession == null || profession.skinPrefab == null) return;

        // Instanciar nuevo modelo
        _currentSkinGO = Instantiate(profession.skinPrefab, skinParent);
        _currentSkinGO.transform.localPosition = Vector3.zero;
        _currentSkinGO.transform.localRotation = Quaternion.identity;

        // Obtener Animator del modelo (puede estar en el raíz o en un hijo)
        _animator = _currentSkinGO.GetComponent<Animator>()
                 ?? _currentSkinGO.GetComponentInChildren<Animator>();

        // Asignar Animator Controller específico de la profesión
        if (_animator != null && profession.animatorController != null)
            _animator.runtimeAnimatorController = profession.animatorController;

        // Aplicar tinte de color al material
        ApplyTint(profession.skinTint);
    }

    // ─── Tinte ────────────────────────────────────────────────

    void ApplyTint(Color tint)
    {
        if (_currentSkinGO == null) return;

        // Buscar todos los renderers del modelo
        var renderers = _currentSkinGO.GetComponentsInChildren<Renderer>();

        foreach (var r in renderers)
        {
            // Intentamos con _BaseColor (URP/HDRP) y _Color (Standard)
            foreach (var mat in r.materials)
            {
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", tint);
                    break;
                }
                if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", tint);
                    break;
                }
            }
        }
    }

    // ─── Animaciones ──────────────────────────────────────────
    // Estos métodos son atajos seguros: si no hay Animator o la
    // profesión no tiene el parámetro, no falla.

    public void SetWorking(bool v)
    {
        if (_animator == null || _currentProfession == null) return;
        SafeSetBool(_currentProfession.workingParam, v);
    }

    public void SetWalking(bool v)
    {
        if (_animator == null || _currentProfession == null) return;
        SafeSetBool(_currentProfession.walkingParam, v);
    }

    public void TriggerAttack()  => SafeTrigger(_currentProfession?.attackTrigger);
    public void TriggerHurt()    => SafeTrigger(_currentProfession?.hurtTrigger);
    public void TriggerDie()     => SafeTrigger(_currentProfession?.dieTrigger);

    void SafeSetBool(string param, bool v)
    {
        if (_animator == null || string.IsNullOrEmpty(param)) return;
        _animator.SetBool(param, v);
    }

    void SafeTrigger(string trigger)
    {
        if (_animator == null || string.IsNullOrEmpty(trigger)) return;
        _animator.SetTrigger(trigger);
    }

    // ─── Reset para pooling ───────────────────────────────────

    /// <summary>Limpia el skin cuando el trabajador vuelve al pool.</summary>
    public void ResetForPool()
    {
        if (_currentSkinGO != null)
        {
            Destroy(_currentSkinGO);
            _currentSkinGO     = null;
            _animator          = null;
            _currentProfession = null;
        }
    }
}
