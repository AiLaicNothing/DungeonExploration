using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tab de Audio: sliders de Master, Music, SFX, UI.
/// Lee y escribe a SettingsManager.Instance.
/// </summary>
public class SettingsTab_Audio : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider uiSlider;

    void OnEnable()
    {
        if (SettingsManager.Instance == null) return;

        BindSlider(masterSlider, SettingsManager.Instance.VolumeMaster, v => SettingsManager.Instance.VolumeMaster = v);
        BindSlider(musicSlider, SettingsManager.Instance.VolumeMusic, v => SettingsManager.Instance.VolumeMusic = v);
        BindSlider(sfxSlider, SettingsManager.Instance.VolumeSFX, v => SettingsManager.Instance.VolumeSFX = v);
        BindSlider(uiSlider, SettingsManager.Instance.VolumeUI, v => SettingsManager.Instance.VolumeUI = v);
    }

    private void BindSlider(Slider slider, float initialValue, System.Action<float> onChange)
    {
        if (slider == null) return;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = initialValue;
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(v => onChange(v));
    }
}