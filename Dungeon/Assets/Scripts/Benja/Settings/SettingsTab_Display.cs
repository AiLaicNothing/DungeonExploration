using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsTab_Display : MonoBehaviour
{
    [Header("Resolución")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Calidad")]
    [SerializeField] private Toggle vsyncToggle;
    [SerializeField] private TMP_Dropdown qualityDropdown;

    [Header("FPS")]
    [SerializeField] private TMP_Dropdown fpsLimitDropdown;
    // Opciones del dropdown FPS (orden importa): -1=ilimitado, 30, 60, 120, 144
    private readonly int[] _fpsOptions = { -1, 30, 60, 120, 144 };

    void OnEnable()
    {
        if (SettingsManager.Instance == null) return;

        SetupResolutionDropdown();
        SetupQualityDropdown();
        SetupFPSDropdown();

        fullscreenToggle.SetIsOnWithoutNotify(SettingsManager.Instance.Fullscreen);
        vsyncToggle.SetIsOnWithoutNotify(SettingsManager.Instance.VSync);

        fullscreenToggle.onValueChanged.AddListener(v => SettingsManager.Instance.Fullscreen = v);
        vsyncToggle.onValueChanged.AddListener(v => SettingsManager.Instance.VSync = v);
    }

    void OnDisable()
    {
        fullscreenToggle.onValueChanged.RemoveAllListeners();
        vsyncToggle.onValueChanged.RemoveAllListeners();
        resolutionDropdown.onValueChanged.RemoveAllListeners();
        qualityDropdown.onValueChanged.RemoveAllListeners();
        fpsLimitDropdown.onValueChanged.RemoveAllListeners();
    }

    private void SetupResolutionDropdown()
    {
        resolutionDropdown.ClearOptions();
        var resolutions = SettingsManager.Instance.AvailableResolutions;
        var options = new List<string>();
        for (int i = 0; i < resolutions.Length; i++)
        {
            var r = resolutions[i];
            options.Add($"{r.width} x {r.height} @ {r.refreshRateRatio.value:F0}Hz");
        }
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.SetValueWithoutNotify(SettingsManager.Instance.ResolutionIndex);
        resolutionDropdown.onValueChanged.AddListener(v => SettingsManager.Instance.ResolutionIndex = v);
    }

    private void SetupQualityDropdown()
    {
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        qualityDropdown.SetValueWithoutNotify(SettingsManager.Instance.QualityLevel);
        qualityDropdown.onValueChanged.AddListener(v => SettingsManager.Instance.QualityLevel = v);
    }

    private void SetupFPSDropdown()
    {
        fpsLimitDropdown.ClearOptions();
        var labels = new List<string>();
        foreach (int fps in _fpsOptions)
            labels.Add(fps == -1 ? "Ilimitado" : fps.ToString());
        fpsLimitDropdown.AddOptions(labels);

        int currentFps = SettingsManager.Instance.FPSLimit;
        int idx = System.Array.IndexOf(_fpsOptions, currentFps);
        if (idx < 0) idx = 0;
        fpsLimitDropdown.SetValueWithoutNotify(idx);
        fpsLimitDropdown.onValueChanged.AddListener(v => SettingsManager.Instance.FPSLimit = _fpsOptions[v]);
    }
}