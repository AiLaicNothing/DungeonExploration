using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel completo de mejora de stats. Coordina varias StatRowUI:
///   - Lee los "PendingDelta" de cada fila
///   - Calcula puntos disponibles después de los cambios pendientes
///   - Habilita/deshabilita el botón Confirmar según si los puntos alcanzan
///   - Al confirmar, manda un único RPC al servidor con todos los cambios
///   - Al cancelar, resetea los deltas de todas las filas
/// </summary>
public class CheckpointUpgradeUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text pointsHeaderText;     // "Puntos disponibles: 15 → 12"
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text errorText;

    [Header("Filas de stats")]
    [Tooltip("Las filas StatRowUI se buscan automáticamente como hijos de este Transform.")]
    [SerializeField] private Transform statRowsContainer;

    private List<StatRowUI> _rows = new();
    private PlayerStats _stats;

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        if (cancelButton != null) cancelButton.onClick.AddListener(Close);
    }

    void OnEnable()
    {
        LocalPlayer.SubscribeOrInvokeIfReady(OnLocalPlayerReady);
    }

    void OnDisable()
    {
        LocalPlayer.Unsubscribe(OnLocalPlayerReady);

        // Desuscribir de eventos de delta de filas
        foreach (var row in _rows)
            row.OnDeltaChanged -= UpdateHeader;
    }

    private void OnLocalPlayerReady(PlayerController controller)
    {
        _stats = controller.Stats;
        if (_stats != null)
            _stats.OnPointsChanged += _ => UpdateHeader();
    }

    public void Open()
    {
        if (panelRoot == null) return;
        panelRoot.SetActive(true);

        // Indexar las filas de stats al abrir
        _rows.Clear();
        if (statRowsContainer != null)
            statRowsContainer.GetComponentsInChildren(true, _rows);

        // Reset de deltas y suscripción a sus eventos
        foreach (var row in _rows)
        {
            row.ResetDelta();
            row.OnDeltaChanged -= UpdateHeader;
            row.OnDeltaChanged += UpdateHeader;
        }

        UpdateHeader();
        ClearError();
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        // Cancelar todos los cambios pendientes
        foreach (var row in _rows) row.ResetDelta();
    }

    /// <summary>
    /// Recalcula el balance global y actualiza la cabecera.
    /// </summary>
    private void UpdateHeader()
    {
        if (_stats == null) return;

        int totalCost = 0;
        foreach (var row in _rows)
            totalCost += row.PendingCost;

        int available = _stats.UpgradePoints;
        int afterChanges = available - totalCost;

        // Texto de cabecera
        if (pointsHeaderText != null)
        {
            if (totalCost == 0)
            {
                pointsHeaderText.text = $"Puntos disponibles: {available}";
                pointsHeaderText.color = Color.white;
            }
            else
            {
                string arrow = totalCost > 0 ? "→" : "→";
                pointsHeaderText.text = $"Puntos disponibles: {available} {arrow} {afterChanges}";
                pointsHeaderText.color = afterChanges < 0 ? new Color(1f, 0.4f, 0.4f) : new Color(0.9f, 0.9f, 0.4f);
            }
        }

        // Habilitar Confirmar solo si hay cambios Y los puntos alcanzan
        if (confirmButton != null)
        {
            bool hasChanges = totalCost != 0 || HasAnyDelta();
            bool canAfford = afterChanges >= 0;
            confirmButton.interactable = hasChanges && canAfford;
        }
    }

    private bool HasAnyDelta()
    {
        foreach (var row in _rows)
            if (row.PendingDelta != 0) return true;
        return false;
    }

    private void OnConfirm()
    {
        if (_stats == null) return;

        // Construir las listas de IDs a subir/bajar repitiendo según el delta
        var increaseIds = new List<string>();
        var decreaseIds = new List<string>();

        foreach (var row in _rows)
        {
            int delta = row.PendingDelta;
            if (delta > 0)
            {
                for (int i = 0; i < delta; i++)
                    increaseIds.Add(row.StatId);
            }
            else if (delta < 0)
            {
                for (int i = 0; i < -delta; i++)
                    decreaseIds.Add(row.StatId);
            }
        }

        if (increaseIds.Count == 0 && decreaseIds.Count == 0)
        {
            ShowError("No hay cambios que aplicar.");
            return;
        }

        // Enviar al servidor (autoritativo)
        _stats.RequestApplyTradeoff(increaseIds.ToArray(), decreaseIds.ToArray());

        // Limpiar deltas tras enviar
        foreach (var row in _rows) row.ResetDelta();

        ClearError();
        Close();
    }

    private void ShowError(string msg)
    {
        if (errorText != null) errorText.text = msg;
    }

    private void ClearError()
    {
        if (errorText != null) errorText.text = "";
    }
}