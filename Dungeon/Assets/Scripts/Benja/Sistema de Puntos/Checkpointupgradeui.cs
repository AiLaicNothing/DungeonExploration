using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Panel de mejora de stats por tradeoff.
/// Permite seleccionar múltiples stats a subir y múltiples a bajar.
/// El botón Confirmar se habilita cuando el balance cuadra con los puntos disponibles.
/// </summary>
public class Checkpointupgradeui : MonoBehaviour
{
    [Header("Referencias UI")]
    public TextMeshProUGUI pointsText;
    public TextMeshProUGUI balanceText;       // muestra "Costo: +3 / Balance: -2 → gastas 1 punto"
    public Transform statListContainer;       // Content del ScrollView
    public GameObject statRowPrefab;          // Prefab StatRow
    public Button confirmButton;

    private List<StatRowUI> _rows = new();

    // Selecciones actuales (listas porque permiten múltiples de la misma stat si se quisiera,
    // pero StatRowUI es mutuamente excluyente por fila, así que cada ID aparece máx 1 vez)
    private List<string> _selectedIncreases = new();
    private List<string> _selectedDecreases = new();

    void OnEnable()
    {
        BuildRows();

        confirmButton.onClick.AddListener(OnConfirm);
        PlayerStats.Instance.OnStatChanged += HandleStatChanged;
        PlayerStats.Instance.OnPointsChanged += HandlePointsChanged;

        UpdatePointsText();
        ValidateConfirmButton();
    }

    void OnDisable()
    {
        confirmButton.onClick.RemoveListener(OnConfirm);
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatChanged -= HandleStatChanged;
            PlayerStats.Instance.OnPointsChanged -= HandlePointsChanged;
        }

        _selectedIncreases.Clear();
        _selectedDecreases.Clear();
    }

    // ── Construcción ──────────────────────────────────────────────────
    void BuildRows()
    {
        foreach (Transform child in statListContainer)
            Destroy(child.gameObject);
        _rows.Clear();

        foreach (var stat in PlayerStats.Instance.AllStats)
        {
            var go = Instantiate(statRowPrefab, statListContainer);
            var row = go.GetComponent<StatRowUI>();
            row.Setup(stat, OnSelectIncrease, OnSelectDecrease);
            _rows.Add(row);
        }
    }

    // ── Callbacks de selección ────────────────────────────────────────
    void OnSelectIncrease(string statId)
    {
        // Al marcar +, remueve de cualquier lista previa
        if (statId != null)
        {
            _selectedDecreases.Remove(statId);
            if (!_selectedIncreases.Contains(statId))
                _selectedIncreases.Add(statId);
        }
        else
        {
            // Cuando el toggle se deselecciona, removemos todo lo que esté solo en increase
            // (no podemos saber cuál fila llamó con null sin más contexto; la fila ya se desmarcó)
            // Reconstruimos desde el estado de las filas:
            RebuildSelectionsFromRows();
        }
        ValidateConfirmButton();
    }

    void OnSelectDecrease(string statId)
    {
        if (statId != null)
        {
            _selectedIncreases.Remove(statId);
            if (!_selectedDecreases.Contains(statId))
                _selectedDecreases.Add(statId);
        }
        else
        {
            RebuildSelectionsFromRows();
        }
        ValidateConfirmButton();
    }

    /// <summary>Reconstruye las listas de selección leyendo el estado real de cada fila.</summary>
    void RebuildSelectionsFromRows()
    {
        _selectedIncreases.Clear();
        _selectedDecreases.Clear();
        foreach (var row in _rows)
        {
            if (row.IsIncreaseSelected) _selectedIncreases.Add(row.StatId);
            else if (row.IsDecreaseSelected) _selectedDecreases.Add(row.StatId);
        }
    }

    // ── Validación ────────────────────────────────────────────────────
    void ValidateConfirmButton()
    {
        RebuildSelectionsFromRows();

        int upgradeCost = 0;
        foreach (var id in _selectedIncreases)
        {
            var s = PlayerStats.Instance.GetStat(id);
            if (s != null) upgradeCost += s.UpgradeCost;
        }

        int downgradeValue = 0;
        foreach (var id in _selectedDecreases)
        {
            var s = PlayerStats.Instance.GetStat(id);
            if (s != null) downgradeValue += s.DowngradeValue;
        }

        int pointsNeeded = Mathf.Max(0, upgradeCost - downgradeValue);
        bool hasSelection = _selectedIncreases.Count > 0 || _selectedDecreases.Count > 0;
        bool canAfford = pointsNeeded <= PlayerStats.Instance.upgradePoints;

        confirmButton.interactable = hasSelection && canAfford;

        if (balanceText != null)
        {
            balanceText.text = hasSelection
                ? $"Costo: +{upgradeCost} / Compensado: -{downgradeValue} → gastarás {pointsNeeded} punto(s)"
                : "Selecciona qué mejorar y qué sacrificar";
        }
    }

    // ── Confirmar ─────────────────────────────────────────────────────
    void OnConfirm()
    {
        RebuildSelectionsFromRows();

        bool success = PlayerStats.Instance.ApplyTradeoff(
            _selectedIncreases, _selectedDecreases);

        if (!success) return;

        // Limpia selección
        _selectedIncreases.Clear();
        _selectedDecreases.Clear();
        foreach (var row in _rows) row.Deselect();

        ValidateConfirmButton();

        // Auto-guarda al aplicar un cambio de stats
        if (Savesystem.Instance != null && Savesystem.Instance.autoSaveOnCheckpoint)
            Savesystem.Instance.Save();
    }

    // ── Eventos de PlayerStats ────────────────────────────────────────
    void HandleStatChanged(string id, float value)
    {
        var row = _rows.Find(r => r.StatId == id);
        row?.UpdateDisplay();
    }

    void HandlePointsChanged(int points)
    {
        UpdatePointsText();
        ValidateConfirmButton();
    }

    void UpdatePointsText() =>
        pointsText.text = $"Puntos disponibles: {PlayerStats.Instance.upgradePoints}";
}