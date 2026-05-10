using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tab de Controles: sensibilidad de la cámara y invertir ejes X/Y.
/// El SettingsManager tiene una sola Sensitivity (no separada en X/Y),
/// así que aquí usamos un único slider.
/// </summary>
public class SettingsTab_Controls : MonoBehaviour
{
    [Header("Sensibilidad")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TMP_Text sensitivityLabel;

    [Header("Invertir ejes")]
    [SerializeField] private Toggle invertXToggle;
    [SerializeField] private Toggle invertYToggle;

    void OnEnable()
    {
        if (SettingsManager.Instance == null) return;

        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = SettingsManager.Instance.MinSensitivity;
            sensitivitySlider.maxValue = SettingsManager.Instance.MaxSensitivity;
            sensitivitySlider.value = SettingsManager.Instance.Sensitivity;
            UpdateSensLabel(sensitivitySlider.value);

            sensitivitySlider.onValueChanged.RemoveAllListeners();
            sensitivitySlider.onValueChanged.AddListener(v =>
            {
                SettingsManager.Instance.Sensitivity = v;
                UpdateSensLabel(v);
            });
        }

        if (invertXToggle != null)
        {
            invertXToggle.isOn = SettingsManager.Instance.InvertX;
            invertXToggle.onValueChanged.RemoveAllListeners();
            invertXToggle.onValueChanged.AddListener(v => SettingsManager.Instance.InvertX = v);
        }

        if (invertYToggle != null)
        {
            invertYToggle.isOn = SettingsManager.Instance.InvertY;
            invertYToggle.onValueChanged.RemoveAllListeners();
            invertYToggle.onValueChanged.AddListener(v => SettingsManager.Instance.InvertY = v);
        }
    }

    private void UpdateSensLabel(float v)
    {
        if (sensitivityLabel != null) sensitivityLabel.text = $"Sensibilidad: {v:F2}";
    }
}