// ============================================================
// WorkerAssignmentUI.cs
// ============================================================
// UI de gestión de comunidad.
// Muestra la lista de trabajadores, su estado actual,
// y permite al jugador:
//   - Asignar / cambiar profesión
//   - Asignar / cambiar área de trabajo
//   - Ver necesidades (hambre, descanso, obediencia)
//
// ARQUITECTURA:
//   Este script es solo el CONTROLADOR de la UI.
//   No gestiona lógica de trabajadores, solo llama
//   métodos de CommunityManager.
//
// CÓMO CONECTARLO:
//   1. Crea un Canvas con un panel de "Gestión de Comunidad"
//   2. Agrega este script a cualquier GO de la escena
//   3. Asigna las listas de profesiones disponibles y áreas
//   4. Abre la UI con la tecla configurada (default: Tab)
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorkerAssignmentUI : MonoBehaviour
{
    [Header("Tecla para abrir/cerrar")]
    [SerializeField] KeyCode toggleKey = KeyCode.Tab;

    [Header("Panel raíz")]
    [SerializeField] GameObject panelRoot;

    [Header("Lista de trabajadores")]
    [Tooltip("Transform padre donde se crean las filas de trabajadores.")]
    [SerializeField] Transform workerListParent;
    [SerializeField] GameObject workerRowPrefab; // prefab de una fila

    [Header("Panel de asignación (se activa al seleccionar un trabajador)")]
    [SerializeField] GameObject assignPanel;
    [SerializeField] TextMeshProUGUI selectedWorkerName;
    [SerializeField] Transform professionButtonsParent;
    [SerializeField] Transform workAreaButtonsParent;
    [SerializeField] GameObject professionButtonPrefab;
    [SerializeField] GameObject workAreaButtonPrefab;

    [Header("Datos disponibles")]
    [Tooltip("Todas las profesiones que el jugador puede asignar.")]
    [SerializeField] List<ProfessionSO> availableProfessions = new List<ProfessionSO>();

    [Header("Stats globales")]
    [SerializeField] TextMeshProUGUI statsText;

    // ─── Estado ───────────────────────────────────────────────
    bool             _isOpen;
    WorkerController _selectedWorker;
    readonly List<GameObject> _rowInstances   = new List<GameObject>();
    readonly List<GameObject> _profBtnInstances = new List<GameObject>();
    readonly List<GameObject> _areaBtnInstances = new List<GameObject>();

    // ─── Init / Update ────────────────────────────────────────

    void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (assignPanel != null) assignPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            ToggleUI();

        if (_isOpen)
            RefreshStats();
    }

    // ─── Toggle ───────────────────────────────────────────────

    public void ToggleUI()
    {
        _isOpen = !_isOpen;
        panelRoot?.SetActive(_isOpen);

        if (_isOpen)
        {
            BuildWorkerList();
            BuildProfessionButtons();
            BuildWorkAreaButtons();
            Time.timeScale = 0f;  // pausar el juego mientras gestionas
        }
        else
        {
            Time.timeScale = 1f;
            ClearSelection();
        }
    }

    // ─── Lista de trabajadores ────────────────────────────────

    void BuildWorkerList()
    {
        // Limpiar filas anteriores
        foreach (var r in _rowInstances)
            if (r != null) Destroy(r);
        _rowInstances.Clear();

        if (workerListParent == null || workerRowPrefab == null) return;

        var cm = CommunityManager.Instance;
        if (cm == null) return;

        foreach (var worker in cm.AllWorkers)
        {
            var row = Instantiate(workerRowPrefab, workerListParent);
            _rowInstances.Add(row);

            // Rellenar texto de la fila
            // Se asume que el prefab tiene TextMeshProUGUI hijos en este orden:
            // [0] nombre, [1] profesión, [2] estado
            var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0) texts[0].text = worker.WorkerName;
            if (texts.Length > 1) texts[1].text = worker.ProfessionType.ToString();
            if (texts.Length > 2) texts[2].text = worker.State.ToString();

            // Icono de hambre
            var img = row.GetComponentInChildren<Image>();
            if (img != null)
            {
                img.color = worker.Needs.IsHungry ? Color.red
                          : worker.Needs.IsTired  ? Color.yellow
                          : Color.green;
            }

            // Click en la fila → seleccionar trabajador
            var capturedWorker = worker;
            var btn = row.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => SelectWorker(capturedWorker));
        }
    }

    // ─── Botones de profesión ─────────────────────────────────

    void BuildProfessionButtons()
    {
        foreach (var b in _profBtnInstances) if (b != null) Destroy(b);
        _profBtnInstances.Clear();

        if (professionButtonsParent == null || professionButtonPrefab == null) return;

        foreach (var prof in availableProfessions)
        {
            var btn = Instantiate(professionButtonPrefab, professionButtonsParent);
            _profBtnInstances.Add(btn);

            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = prof.displayName;

            var icon = btn.GetComponentInChildren<Image>();
            if (icon != null && prof.icon != null) icon.sprite = prof.icon;

            var capturedProf = prof;
            var b = btn.GetComponent<Button>();
            if (b != null)
                b.onClick.AddListener(() => OnProfessionButtonClicked(capturedProf));
        }
    }

    // ─── Botones de áreas de trabajo ──────────────────────────

    void BuildWorkAreaButtons()
    {
        foreach (var b in _areaBtnInstances) if (b != null) Destroy(b);
        _areaBtnInstances.Clear();

        if (workAreaButtonsParent == null || workAreaButtonPrefab == null) return;

        // Buscar todas las WorkArea en escena
        var areas = FindObjectsOfType<WorkArea>();

        foreach (var area in areas)
        {
            var btn = Instantiate(workAreaButtonPrefab, workAreaButtonsParent);
            _areaBtnInstances.Add(btn);

            var texts = btn.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0)
                texts[0].text = area.data != null ? area.data.workAreaName : area.name;
            if (texts.Length > 1)
                texts[1].text = $"{area.WorkerCount}/{area.MaxWorkers}";

            // Grisear si está llena o el trabajador seleccionado no es compatible
            var b = btn.GetComponent<Button>();

            var capturedArea = area;
            if (b != null)
            {
                b.onClick.AddListener(() => OnWorkAreaButtonClicked(capturedArea));
                // Actualizar interactuabilidad al seleccionar trabajador
            }
        }
    }

    // ─── Selección ────────────────────────────────────────────

    void SelectWorker(WorkerController worker)
    {
        _selectedWorker = worker;
        if (assignPanel != null) assignPanel.SetActive(true);
        if (selectedWorkerName != null)
            selectedWorkerName.text = $"{worker.WorkerName} — {worker.ProfessionType}";

        // Resaltar qué áreas son compatibles con la profesión del trabajador
        UpdateWorkAreaButtonStates();
    }

    void ClearSelection()
    {
        _selectedWorker = null;
        if (assignPanel != null) assignPanel.SetActive(false);
    }

    void UpdateWorkAreaButtonStates()
    {
        // Desactiva botones de áreas incompatibles con la profesión del trabajador seleccionado
        for (int i = 0; i < _areaBtnInstances.Count; i++)
        {
            var areas = FindObjectsOfType<WorkArea>();
            if (i >= areas.Length) break;
            var area = areas[i];
            var btn = _areaBtnInstances[i]?.GetComponent<Button>();
            if (btn == null) continue;

            bool compatible = _selectedWorker?.Profession != null &&
                              _selectedWorker.Profession.CanWorkAt(area.WorkTag);
            bool notFull    = !area.IsFull;

            btn.interactable = compatible && notFull;
        }
    }

    // ─── Handlers de clicks ───────────────────────────────────

    void OnProfessionButtonClicked(ProfessionSO profession)
    {
        if (_selectedWorker == null) return;
        CommunityManager.Instance?.AssignProfession(_selectedWorker, profession);

        // Actualizar la UI
        BuildWorkerList();
        if (selectedWorkerName != null)
            selectedWorkerName.text = $"{_selectedWorker.WorkerName} — {_selectedWorker.ProfessionType}";
        UpdateWorkAreaButtonStates();
    }

    void OnWorkAreaButtonClicked(WorkArea area)
    {
        if (_selectedWorker == null) return;
        bool ok = CommunityManager.Instance?.AssignToWorkArea(_selectedWorker, area) ?? false;

        if (!ok)
            Debug.LogWarning($"[WorkerUI] No se pudo asignar {_selectedWorker.WorkerName} a {area.data?.workAreaName}");

        BuildWorkerList();
    }

    // ─── Stats globales ───────────────────────────────────────

    void RefreshStats()
    {
        if (statsText == null) return;
        var cm = CommunityManager.Instance;
        if (cm == null) return;

        statsText.text =
            $"Comunidad: {cm.TotalWorkers} trabajadores\n" +
            $"Comida: {cm.GetResourceAmount(ResourceTypeProfesion.Food):F0}\n" +
            $"Madera: {cm.GetResourceAmount(ResourceTypeProfesion.Wood):F0}\n" +
            $"Metal: {cm.GetResourceAmount(ResourceTypeProfesion.Metal):F0}\n" +
            $"Componentes: {cm.GetResourceAmount(ResourceTypeProfesion.Components):F0}";
    }
}
