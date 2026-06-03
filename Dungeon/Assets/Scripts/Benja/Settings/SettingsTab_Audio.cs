using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsTab_Audio : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider uiSlider;
    [SerializeField] private Slider ambientSlider;

    [SerializeField] private TMP_Text masterText;
    [SerializeField] private TMP_Text musicText;
    [SerializeField] private TMP_Text sfxText;
    [SerializeField] private TMP_Text uiText;
    [SerializeField] private TMP_Text ambientText;

    void OnEnable()
    {
        if (SettingsManager.Instance == null) return;

        BindSlider(masterSlider, SettingsManager.Instance.VolumeMaster, v => SettingsManager.Instance.VolumeMaster = v, masterText);
        BindSlider(musicSlider, SettingsManager.Instance.VolumeMusic, v => SettingsManager.Instance.VolumeMusic = v, musicText);
        BindSlider(sfxSlider, SettingsManager.Instance.VolumeSFX, v => SettingsManager.Instance.VolumeSFX = v, sfxText);
        BindSlider(uiSlider, SettingsManager.Instance.VolumeUI, v => SettingsManager.Instance.VolumeUI = v, uiText);
        BindSlider(ambientSlider, SettingsManager.Instance.VolumeAmbient, v => SettingsManager.Instance.VolumeAmbient = v, ambientText);
    }

    private void BindSlider(Slider slider, float initialValue, System.Action<float> onChange, TMP_Text valueText = null)
    {
        if (slider == null) return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        initialValue = Mathf.Clamp01(initialValue);
        slider.value = initialValue;

        if (valueText != null)
            valueText.text = Mathf.RoundToInt(initialValue * 100f) + "%";

        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(v =>
        {
            v = Mathf.Clamp01(v);
            onChange(v);

            if (valueText != null)
                valueText.text = Mathf.RoundToInt(v * 100f) + "%";
        });
    }
}