using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class SettingsTab_Controls : MonoBehaviour
{
    [Header("Sensibilidad")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TMP_Text sensitivityLabel;

    [Header("Invertir ejes")]
    [SerializeField] private Toggle invertXToggle;
    [SerializeField] private Toggle invertYToggle;

    // ── Espacio reservado para opciones de mando ──────────────────────
    // Cuando quieras añadir opciones específicas de gamepad (PS5/Xbox), por ejemplo:
    //
    // [Header("Gamepad")]
    // [SerializeField] private Slider deadZoneSlider;
    // [SerializeField] private Toggle vibrationToggle;
    // [SerializeField] private TMP_Dropdown buttonLayoutDropdown;
    //
    // Y añade las propiedades correspondientes en SettingsManager.

    void OnEnable()
    {
        if (SettingsManager.Instance == null) return;
        var s = SettingsManager.Instance;

        sensitivitySlider.minValue = s.MinSensitivity;
        sensitivitySlider.maxValue = s.MaxSensitivity;
        sensitivitySlider.SetValueWithoutNotify(s.Sensitivity);
        UpdateSensitivityLabel(s.Sensitivity);

        invertXToggle.SetIsOnWithoutNotify(s.InvertX);
        invertYToggle.SetIsOnWithoutNotify(s.InvertY);

        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        invertXToggle.onValueChanged.AddListener(v => s.InvertX = v);
        invertYToggle.onValueChanged.AddListener(v => s.InvertY = v);
    }

    void OnDisable()
    {
        if (sensitivitySlider != null) sensitivitySlider.onValueChanged.RemoveAllListeners();
        if (invertXToggle != null) invertXToggle.onValueChanged.RemoveAllListeners();
        if (invertYToggle != null) invertYToggle.onValueChanged.RemoveAllListeners();
    }

    private void OnSensitivityChanged(float value)
    {
        SettingsManager.Instance.Sensitivity = value;
        UpdateSensitivityLabel(value);
    }

    private void UpdateSensitivityLabel(float value)
    {
        if (sensitivityLabel != null) sensitivityLabel.text = value.ToString("F2");
    }
}