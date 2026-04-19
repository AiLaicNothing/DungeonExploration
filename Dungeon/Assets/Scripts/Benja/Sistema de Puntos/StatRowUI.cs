using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Fila individual de una stat en el panel de upgrade.
/// Permite seleccionar "+" o "-" (toggles mutuamente excluyentes dentro de la fila).
/// </summary>
public class StatRowUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text valueLabel;
    [SerializeField] private TMP_Text costLabel;
    [SerializeField] private Toggle increaseToggle;
    [SerializeField] private Toggle decreaseToggle;

    public string StatId => _stat != null ? _stat.Id : null;
    public bool IsIncreaseSelected => increaseToggle != null && increaseToggle.isOn;
    public bool IsDecreaseSelected => decreaseToggle != null && decreaseToggle.isOn;

    private PlayerStat _stat;
    private Action<string> _onSelectIncrease;
    private Action<string> _onSelectDecrease;

    public void Setup(PlayerStat stat, Action<string> onSelectIncrease, Action<string> onSelectDecrease)
    {
        _stat = stat;
        _onSelectIncrease = onSelectIncrease;
        _onSelectDecrease = onSelectDecrease;

        if (nameLabel != null) nameLabel.text = stat.DisplayName;
        if (costLabel != null)
            costLabel.text = $"(+{stat.UpgradeCost} / -{stat.DowngradeValue})";

        // Reset toggles
        increaseToggle.SetIsOnWithoutNotify(false);
        decreaseToggle.SetIsOnWithoutNotify(false);

        increaseToggle.onValueChanged.RemoveAllListeners();
        decreaseToggle.onValueChanged.RemoveAllListeners();

        increaseToggle.onValueChanged.AddListener(OnIncreaseChanged);
        decreaseToggle.onValueChanged.AddListener(OnDecreaseChanged);

        UpdateDisplay();
    }

    private void OnIncreaseChanged(bool isOn)
    {
        if (isOn)
        {
            // Mutuamente excluyente con decrease
            decreaseToggle.SetIsOnWithoutNotify(false);
            _onSelectIncrease?.Invoke(_stat.Id);
        }
        else
        {
            _onSelectIncrease?.Invoke(null);
        }
    }

    private void OnDecreaseChanged(bool isOn)
    {
        if (isOn)
        {
            increaseToggle.SetIsOnWithoutNotify(false);
            _onSelectDecrease?.Invoke(_stat.Id);
        }
        else
        {
            _onSelectDecrease?.Invoke(null);
        }
    }

    /// <summary>Actualiza el texto con el valor actual de la stat.</summary>
    public void UpdateDisplay()
    {
        if (_stat == null || valueLabel == null) return;
        valueLabel.text = $"{_stat.CurrentValue:F0} / {_stat.Max:F0}";
    }

    /// <summary>Desmarca los toggles sin disparar eventos (llamado tras confirmar).</summary>
    public void Deselect()
    {
        increaseToggle.SetIsOnWithoutNotify(false);
        decreaseToggle.SetIsOnWithoutNotify(false);
    }
}
