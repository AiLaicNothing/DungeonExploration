using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Tab de Display: resolución, ventana, vsync, calidad URP, FPS objetivo.
/// Lee y escribe a SettingsManager.Instance.
/// </summary>
public class SettingsTab_Display : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle vsyncToggle;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private TMP_Dropdown fpsDropdown;

    void OnEnable()
    {
        Debug.Log($"[SettingsTab_Display] OnEnable corriendo. Frame: {Time.frameCount}, " +
                  $"SettingsManager.Instance: {(SettingsManager.Instance == null ? "NULL" : "OK")}");
        // Si SettingsManager aún no existe, esperar a que aparezca
        StartCoroutine(WaitForSettingsAndSetup());
    }

    private System.Collections.IEnumerator WaitForSettingsAndSetup()
    {
        // Esperar hasta 5 segundos a que SettingsManager esté disponible
        float timeout = 5f;
        float waited = 0f;
        while (SettingsManager.Instance == null && waited < timeout)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (SettingsManager.Instance == null)
        {
            Debug.LogError("[SettingsTab_Display] SettingsManager no encontrado. " +
                           "Asegúrate de arrancar desde 00_Boot.");
            yield break;
        }

        Debug.Log($"[SettingsTab_Display] Setup ejecutando. " +
                  $"resolutionDropdown={resolutionDropdown != null}, " +
                  $"fullscreenToggle={fullscreenToggle != null}, " +
                  $"vsyncToggle={vsyncToggle != null}, " +
                  $"qualityDropdown={qualityDropdown != null}, " +
                  $"fpsDropdown={fpsDropdown != null}");

        SetupResolution();
        SetupFullscreen();
        SetupVsync();
        SetupQuality();
        SetupFps();

        Debug.Log("[SettingsTab_Display] Setup completado.");
    }

    private void SetupResolution()
    {
        if (resolutionDropdown == null) return;

        var resolutions = SettingsManager.Instance.AvailableResolutions;
        resolutionDropdown.ClearOptions();

        var options = new List<string>();
        for (int i = 0; i < resolutions.Length; i++)
        {
            var r = resolutions[i];
            options.Add($"{r.width} x {r.height} @ {Mathf.RoundToInt((float)r.refreshRateRatio.value)}Hz");
        }
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = SettingsManager.Instance.ResolutionIndex;
        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.onValueChanged.AddListener(v => SettingsManager.Instance.ResolutionIndex = v);
    }

    private void SetupFullscreen()
    {
        if (fullscreenToggle == null) return;
        fullscreenToggle.isOn = SettingsManager.Instance.Fullscreen;
        fullscreenToggle.onValueChanged.RemoveAllListeners();
        fullscreenToggle.onValueChanged.AddListener(v => SettingsManager.Instance.Fullscreen = v);
    }

    private void SetupVsync()
    {
        if (vsyncToggle == null) return;
        vsyncToggle.isOn = SettingsManager.Instance.VSync;
        vsyncToggle.onValueChanged.RemoveAllListeners();
        vsyncToggle.onValueChanged.AddListener(v => SettingsManager.Instance.VSync = v);
    }

    private void SetupQuality()
    {
        if (qualityDropdown == null) return;
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        qualityDropdown.value = SettingsManager.Instance.QualityLevel;
        qualityDropdown.onValueChanged.RemoveAllListeners();
        qualityDropdown.onValueChanged.AddListener(v => SettingsManager.Instance.QualityLevel = v);
    }

    private void SetupFps()
    {
        if (fpsDropdown == null) return;
        fpsDropdown.ClearOptions();
        fpsDropdown.AddOptions(new List<string> { "30", "60", "120", "144", "Ilimitado" });
        fpsDropdown.value = FpsToIndex(SettingsManager.Instance.FPSLimit);
        fpsDropdown.onValueChanged.RemoveAllListeners();
        fpsDropdown.onValueChanged.AddListener(v => SettingsManager.Instance.FPSLimit = IndexToFps(v));
    }

    private int FpsToIndex(int fps) => fps switch { 30 => 0, 60 => 1, 120 => 2, 144 => 3, _ => 4 };
    private int IndexToFps(int idx) => idx switch { 0 => 30, 1 => 60, 2 => 120, 3 => 144, _ => -1 };
}