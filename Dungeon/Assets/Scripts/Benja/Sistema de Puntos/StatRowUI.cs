using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Fila UI de una stat con estilo Diablo/PoE:
///   - Barra visual del progreso (current/hardMax)
///   - Botones [-] y [+] para sumar/restar 1 punto pendiente (no aplica todavía)
///   - Texto que muestra "actual → previsto" si hay cambios pendientes
///   - El delta pendiente se aplica al confirmar el panel padre
///
/// El panel padre (CheckpointUpgradeUI) lee estos deltas y los manda al servidor
/// en un solo RPC al confirmar.
/// </summary>
public class StatRowUI : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("ID exacto de la stat tal como aparece en PlayerStatsData (ej: 'health', 'mana').")]
    [SerializeField] private string statId;

    [Tooltip("Icono opcional para mostrar al lado del nombre.")]
    [SerializeField] private Sprite icon;

    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image fillBar;          // barra visual (Image con Fill Method = Horizontal)
    [SerializeField] private TMP_Text valueText;     // "120 / 200" o "120 → 130 / 200"
    [SerializeField] private TMP_Text deltaText;     // "+1", "-1", o "" (vacío si no hay cambio)
    [SerializeField] private TMP_Text costText;     // "Costo: 2 pts" / "Recuperas: 1 pt"
    [SerializeField] private Button increaseButton;
    [SerializeField] private Button decreaseButton;

    [Header("Colores")]
    [SerializeField] private Color colorNormal = Color.white;
    [SerializeField] private Color colorIncrease = new Color(0.4f, 0.9f, 0.4f);  // verde
    [SerializeField] private Color colorDecrease = new Color(0.9f, 0.4f, 0.4f);  // rojo
    [SerializeField] private Color colorBarNormal = new Color(0.3f, 0.7f, 1f);   // azul
    [SerializeField] private Color colorBarPreview = new Color(0.4f, 0.9f, 0.4f); // verde

    public string StatId => statId;

    /// <summary>Puntos pendientes a sumar (positivo = sumar, negativo = restar).</summary>
    public int PendingDelta { get; private set; }

    /// <summary>Costo en puntos del delta pendiente (puede ser negativo si recuperas).</summary>
    public int PendingCost
    {
        get
        {
            if (_stats == null) return 0;
            var stat = _stats.GetStat(statId);
            if (stat == null) return 0;

            if (PendingDelta > 0) return stat.UpgradeCost * PendingDelta;
            if (PendingDelta < 0) return -stat.DowngradeValue * (-PendingDelta);
            return 0;
        }
    }

    /// <summary>Evento que el panel padre escucha para recalcular el balance global.</summary>
    public event Action OnDeltaChanged;

    private PlayerStats _stats;

    void OnEnable()
    {
        if (iconImage != null && icon != null) iconImage.sprite = icon;

        LocalPlayer.SubscribeOrInvokeIfReady(OnLocalPlayerReady);

        if (increaseButton != null) increaseButton.onClick.AddListener(OnIncrease);
        if (decreaseButton != null) decreaseButton.onClick.AddListener(OnDecrease);
    }

    void OnDisable()
    {
        LocalPlayer.Unsubscribe(OnLocalPlayerReady);
        if (_stats != null) _stats.OnStatChanged -= HandleStatChanged;
        _stats = null;

        if (increaseButton != null) increaseButton.onClick.RemoveListener(OnIncrease);
        if (decreaseButton != null) decreaseButton.onClick.RemoveListener(OnDecrease);
    }

    private void OnLocalPlayerReady(PlayerController controller)
    {
        if (controller.Stats == null) return;

        _stats = controller.Stats;

        // Esperar a que las stats estén sincronizadas
        _stats.SubscribeOrInvokeWhenReady(OnStatsReady);
    }

    private void OnStatsReady()
    {
        if (_stats == null) return;
        _stats.OnStatChanged += HandleStatChanged;
        Refresh();
    }

    private void HandleStatChanged(string id, float value)
    {
        if (id == statId) Refresh();
    }

    // ── Botones ───────────────────────────────────────────────────────
    private void OnIncrease()
    {
        if (!CanIncrease()) return;
        PendingDelta++;
        OnDeltaChanged?.Invoke();
        Refresh();
    }

    private void OnDecrease()
    {
        if (!CanDecrease()) return;
        PendingDelta--;
        OnDeltaChanged?.Invoke();
        Refresh();
    }

    /// <summary>El panel padre llama esto al cancelar/confirmar para volver a 0.</summary>
    public void ResetDelta()
    {
        PendingDelta = 0;
        Refresh();
    }

    private bool CanIncrease()
    {
        if (_stats == null) return false;
        var stat = _stats.GetStat(statId);
        if (stat == null) return false;

        // No exceder el hardMax
        float currentMaxWithDelta = _stats.GetMaxValue(statId) + (stat.ValuePerPoint * PendingDelta);
        if (currentMaxWithDelta + stat.ValuePerPoint > stat.HardMax) return false;

        return true;
    }

    private bool CanDecrease()
    {
        if (_stats == null) return false;
        var stat = _stats.GetStat(statId);
        if (stat == null) return false;

        // No bajar de los puntos asignados (puedes "deshacer" puntos pero no quitar más de los que tienes)
        int totalAssigned = _stats.GetPointsAssigned(statId) + PendingDelta;
        if (totalAssigned <= 0) return false;

        // Tampoco bajar del minValue
        float currentMaxWithDelta = _stats.GetMaxValue(statId) + (stat.ValuePerPoint * PendingDelta);
        if (currentMaxWithDelta - stat.ValuePerPoint < stat.MinValue) return false;

        return true;
    }

    // ── Refresh visual ────────────────────────────────────────────────
    private void Refresh()
    {
        if (_stats == null) return;
        var stat = _stats.GetStat(statId);
        if (stat == null) return;

        // Nombre
        if (nameText != null) nameText.text = stat.DisplayName;

        // Valores actuales
        float currentMax = _stats.GetMaxValue(statId);
        float previewMax = currentMax + (stat.ValuePerPoint * PendingDelta);

        // Texto del valor: cambia según haya delta o no
        if (valueText != null)
        {
            if (PendingDelta != 0)
            {
                valueText.text = $"{currentMax:F0} → {previewMax:F0} / {stat.HardMax:F0}";
                valueText.color = PendingDelta > 0 ? colorIncrease : colorDecrease;
            }
            else
            {
                valueText.text = $"{currentMax:F0} / {stat.HardMax:F0}";
                valueText.color = colorNormal;
            }
        }

        // Texto de delta junto a los botones (estilo "+1", "-2", o "")
        if (deltaText != null)
        {
            if (PendingDelta > 0) { deltaText.text = $"+{PendingDelta}"; deltaText.color = colorIncrease; }
            else if (PendingDelta < 0) { deltaText.text = PendingDelta.ToString(); deltaText.color = colorDecrease; }
            else { deltaText.text = ""; }
        }

        // Texto de costo
        if (costText != null)
        {
            int cost = PendingCost;
            if (cost > 0) { costText.text = $"Costo: {cost} pts"; costText.color = colorIncrease; }
            else if (cost < 0) { costText.text = $"Recuperas: {-cost} pts"; costText.color = colorDecrease; }
            else { costText.text = $"Costo: {stat.UpgradeCost} pts"; costText.color = colorNormal; }
        }

        // Barra visual
        if (fillBar != null)
        {
            float ratio = stat.HardMax > 0f ? previewMax / stat.HardMax : 0f;
            fillBar.fillAmount = Mathf.Clamp01(ratio);
            fillBar.color = (PendingDelta != 0) ? colorBarPreview : colorBarNormal;
        }

        // Habilitar/deshabilitar botones
        if (increaseButton != null) increaseButton.interactable = CanIncrease();
        if (decreaseButton != null) decreaseButton.interactable = CanDecrease();
    }
}