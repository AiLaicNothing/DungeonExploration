using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fila UI de una stat. Muestra nombre, valor actual/max, puntos asignados,
/// y permite seleccionarla con un toggle "+" para subir o "-" para bajar
/// (la aplicación real la hace el panel padre con un tradeoff).
///
/// En multiplayer, lee del PlayerStats del player local (LocalPlayer.Stats).
/// </summary>
public class StatRowUI : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("ID exacto de la stat tal como aparece en PlayerStatsData (ej: 'health', 'mana').")]
    [SerializeField] private string statId;

    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private Toggle increaseToggle;
    [SerializeField] private Toggle decreaseToggle;
    [SerializeField] private TMP_Text costText;

    public string StatId => statId;
    public bool WantsIncrease => increaseToggle != null && increaseToggle.isOn;
    public bool WantsDecrease => decreaseToggle != null && decreaseToggle.isOn;

    private PlayerStats _stats;

    void OnEnable()
    {
        LocalPlayer.SubscribeOrInvokeIfReady(OnLocalPlayerReady);
    }

    void OnDisable()
    {
        LocalPlayer.Unsubscribe(OnLocalPlayerReady);
        if (_stats != null) _stats.OnStatChanged -= HandleStatChanged;
        _stats = null;

        if (increaseToggle != null) increaseToggle.onValueChanged.RemoveListener(OnIncreaseToggle);
        if (decreaseToggle != null) decreaseToggle.onValueChanged.RemoveListener(OnDecreaseToggle);
    }

    private void OnLocalPlayerReady(PlayerController controller)
    {
        _stats = controller.Stats;
        _stats.OnStatChanged += HandleStatChanged;

        if (increaseToggle != null) increaseToggle.onValueChanged.AddListener(OnIncreaseToggle);
        if (decreaseToggle != null) decreaseToggle.onValueChanged.AddListener(OnDecreaseToggle);

        Refresh();
    }

    private void HandleStatChanged(string id, float value)
    {
        if (id == statId) Refresh();
    }

    private void OnIncreaseToggle(bool on)
    {
        if (on && decreaseToggle != null) decreaseToggle.isOn = false;
    }

    private void OnDecreaseToggle(bool on)
    {
        if (on && increaseToggle != null) increaseToggle.isOn = false;
    }

    private void Refresh()
    {
        if (_stats == null) return;
        var stat = _stats.GetStat(statId);
        if (stat == null) return;

        if (nameText != null) nameText.text = stat.DisplayName;
        if (valueText != null) valueText.text = $"{_stats.GetCurrentValue(statId):F0} / {_stats.GetMaxValue(statId):F0}";
        if (pointsText != null) pointsText.text = $"Pts: {_stats.GetPointsAssigned(statId)}";
        if (costText != null) costText.text = $"+{stat.UpgradeCost} / -{stat.DowngradeValue}";
    }

    /// <summary>Resetea los toggles (llamado por el panel padre tras aplicar el tradeoff).</summary>
    public void ClearToggles()
    {
        if (increaseToggle != null) increaseToggle.isOn = false;
        if (decreaseToggle != null) decreaseToggle.isOn = false;
    }
}