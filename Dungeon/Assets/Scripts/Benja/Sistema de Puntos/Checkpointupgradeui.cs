using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel completo de mejora de stats: muestra todas las filas de stats,
/// el balance de puntos según las selecciones del jugador, y aplica el tradeoff
/// vía ServerRpc cuando el jugador confirma.
///
/// Este panel se abre típicamente al interactuar con un Checkpoint (cuando ya está
/// activado) o desde otra UI.
/// </summary>
public class CheckpointUpgradeUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text errorText;

    [Header("Filas de stats")]
    [Tooltip("Las filas StatRowUI se buscan automáticamente como hijos del panel.")]
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
    }

    private void OnLocalPlayerReady(PlayerController controller)
    {
        _stats = controller.Stats;
    }

    public void Open()
    {
        if (panelRoot == null) return;
        panelRoot.SetActive(true);

        // Indexar las filas de stats al abrir
        _rows.Clear();
        if (statRowsContainer != null)
            statRowsContainer.GetComponentsInChildren(true, _rows);

        // Suscribir actualizaciones de balance al cambiar toggles
        foreach (var row in _rows)
            row.gameObject.SetActive(true);

        UpdateBalance();
        ClearError();
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        ClearAllToggles();
    }

    /// <summary>
    /// Recalcula el balance: cuántos puntos cuesta lo seleccionado y cuántos
    /// se recuperan de las stats que se bajan.
    /// </summary>
    void Update()
    {
        if (panelRoot != null && panelRoot.activeSelf)
            UpdateBalance();
    }

    private void UpdateBalance()
    {
        if (_stats == null) return;

        int costoSubidas = 0;
        int valorBajadas = 0;

        foreach (var row in _rows)
        {
            var stat = _stats.GetStat(row.StatId);
            if (stat == null) continue;

            if (row.WantsIncrease) costoSubidas += stat.UpgradeCost;
            if (row.WantsDecrease) valorBajadas += stat.DowngradeValue;
        }

        int needed = Mathf.Max(0, costoSubidas - valorBajadas);
        int available = _stats.UpgradePoints;

        if (balanceText != null)
            balanceText.text = $"Costo: {costoSubidas} | Recuperas: {valorBajadas} | Necesitas: {needed} | Tienes: {available}";

        if (confirmButton != null)
            confirmButton.interactable = (needed <= available) && (costoSubidas > 0 || valorBajadas > 0);
    }

    private void OnConfirm()
    {
        if (_stats == null) return;

        // Recolectar IDs seleccionados
        var increaseIds = new List<string>();
        var decreaseIds = new List<string>();

        foreach (var row in _rows)
        {
            if (row.WantsIncrease) increaseIds.Add(row.StatId);
            if (row.WantsDecrease) decreaseIds.Add(row.StatId);
        }

        if (increaseIds.Count == 0 && decreaseIds.Count == 0)
        {
            ShowError("Selecciona al menos una stat.");
            return;
        }

        // Pedir al servidor que aplique el tradeoff (autoritativo)
        _stats.RequestApplyTradeoff(increaseIds.ToArray(), decreaseIds.ToArray());

        ClearAllToggles();
        ClearError();

        // Opcional: cerrar tras aplicar
        Close();
    }

    private void ClearAllToggles()
    {
        foreach (var row in _rows) row.ClearToggles();
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