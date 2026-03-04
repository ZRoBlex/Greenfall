// ============================================================
// PickupPromptUI.cs
// Carpeta: Scripts/Pickup/
// ------------------------------------------------------------
// UI simple que muestra "Recoger AK-47 [E]" cuando el jugador
// mira un objeto recogible.
//
// ESTRUCTURA EN CANVAS:
// PromptPanel (PickupPromptUI)
// ├── LabelText      (TextMeshProUGUI) → "Recoger AK-47 [E]"
// └── FullText       (TextMeshProUGUI) → "Inventario lleno"
//
// CONFIGURACIÓN:
// 1. Crea un Panel en el Canvas centrado en pantalla (o abajo al centro)
// 2. Ponle este componente
// 3. Asigna los dos TMP texts
// 4. Arrastra este componente al PlayerInteractor → _promptUI
// ============================================================

using System.Collections;
using UnityEngine;
using TMPro;

public class PickupPromptUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // REFERENCIAS
    // ─────────────────────────────────────────────────────────

    [Header("Textos")]
    [Tooltip("Texto principal: 'Recoger [nombre] [tecla]'")]
    [SerializeField] private TextMeshProUGUI _labelText;

    [Tooltip("Texto de inventario lleno. Se muestra brevemente y desaparece.")]
    [SerializeField] private TextMeshProUGUI _fullText;

    [Header("Duración")]
    [Tooltip("Segundos que el mensaje de 'Inventario lleno' permanece visible.")]
    [SerializeField] private float _fullMessageDuration = 1.5f;

    [Header("Texto de Inventario Lleno")]
    [SerializeField] private string _fullMessageText = "Inventario lleno";

    // ─────────────────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────────────────

    private Coroutine _fullTextRoutine;

    // ─────────────────────────────────────────────────────────
    // INICIALIZACIÓN
    // ─────────────────────────────────────────────────────────

    private void Awake()
    {
        // Empezar oculto
        if (_labelText != null) _labelText.gameObject.SetActive(false);
        if (_fullText  != null) _fullText.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Muestra el label de interacción.
    /// Ej: "Recoger AK-47" → se formatea como "Recoger AK-47  [E]"
    /// </summary>
    public void Show(string label, KeyCode key)
    {
        if (_labelText == null) return;

        // Formato: "Recoger AK-47  [E]"
        _labelText.text = $"{label}  [{key}]";
        _labelText.gameObject.SetActive(true);
    }

    /// <summary>Oculta el prompt de interacción.</summary>
    public void Hide()
    {
        if (_labelText != null) _labelText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Muestra el mensaje de "Inventario lleno" durante unos segundos.
    /// </summary>
    public void ShowFullMessage()
    {
        if (_fullText == null) return;

        if (_fullTextRoutine != null) StopCoroutine(_fullTextRoutine);
        _fullTextRoutine = StartCoroutine(ShowFullRoutine());
    }

    // ─────────────────────────────────────────────────────────
    // COROUTINE
    // ─────────────────────────────────────────────────────────

    private IEnumerator ShowFullRoutine()
    {
        _fullText.text = _fullMessageText;
        _fullText.gameObject.SetActive(true);

        yield return new WaitForSeconds(_fullMessageDuration);

        _fullText.gameObject.SetActive(false);
    }
}
