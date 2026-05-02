using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsTab_Audio : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider uiSlider;

    [Header("Labels (opcional)")]
    [SerializeField] private TMP_Text masterLabel;
    [SerializeField] private TMP_Text musicLabel;
    [SerializeField] private TMP_Text sfxLabel;
    [SerializeField] private TMP_Text uiLabel;

    void OnEnable()
    {
        if (SettingsManager.Instance == null) return;
        var s = SettingsManager.Instance;

        SetupSlider(masterSlider, s.VolumeMaster, v => s.VolumeMaster = v, masterLabel);
        SetupSlider(musicSlider, s.VolumeMusic, v => s.VolumeMusic = v, musicLabel);
        SetupSlider(sfxSlider, s.VolumeSFX, v => s.VolumeSFX = v, sfxLabel);
        SetupSlider(uiSlider, s.VolumeUI, v => s.VolumeUI = v, uiLabel);
    }

    void OnDisable()
    {
        if (masterSlider != null) masterSlider.onValueChanged.RemoveAllListeners();
        if (musicSlider != null) musicSlider.onValueChanged.RemoveAllListeners();
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveAllListeners();
        if (uiSlider != null) uiSlider.onValueChanged.RemoveAllListeners();
    }

    private void SetupSlider(Slider slider, float currentValue, System.Action<float> setter, TMP_Text label)
    {
        if (slider == null) return;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(currentValue);
        UpdateLabel(label, currentValue);

        slider.onValueChanged.AddListener(v =>
        {
            setter(v);
            UpdateLabel(label, v);
        });
    }

    private void UpdateLabel(TMP_Text label, float value)
    {
        if (label != null) label.text = $"{Mathf.RoundToInt(value * 100)}%";
    }
}
