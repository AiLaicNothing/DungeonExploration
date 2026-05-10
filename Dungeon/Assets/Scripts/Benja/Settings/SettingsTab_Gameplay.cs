using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tab de Gameplay: FOV, ocultar HUD, idioma, dificultad personal.
/// El SettingsManager usa Language como string ("es", "en") y Difficulty como int.
/// </summary>
public class SettingsTab_Gameplay : MonoBehaviour
{
    [Header("FOV")]
    [SerializeField] private Slider fovSlider;
    [SerializeField] private TMP_Text fovLabel;

    [Header("HUD")]
    [SerializeField] private Toggle hideHudToggle;

    [Header("Idioma")]
    [SerializeField] private TMP_Dropdown languageDropdown;
    [Tooltip("Códigos de idioma alineados al orden del dropdown. Ej: ['es', 'en', 'pt']")]
    [SerializeField] private string[] languageCodes = new[] { "es", "en" };

    [Header("Dificultad")]
    [SerializeField] private TMP_Dropdown difficultyDropdown;

    void OnEnable()
    {
        if (SettingsManager.Instance == null) return;

        SetupFov();
        SetupHud();
        SetupLanguage();
        SetupDifficulty();
    }

    private void SetupFov()
    {
        if (fovSlider == null) return;
        fovSlider.minValue = SettingsManager.Instance.MinFOV;
        fovSlider.maxValue = SettingsManager.Instance.MaxFOV;
        fovSlider.value = SettingsManager.Instance.FOV;
        UpdateFovLabel(fovSlider.value);

        fovSlider.onValueChanged.RemoveAllListeners();
        fovSlider.onValueChanged.AddListener(v =>
        {
            SettingsManager.Instance.FOV = v;
            UpdateFovLabel(v);
        });
    }

    private void SetupHud()
    {
        if (hideHudToggle == null) return;
        hideHudToggle.isOn = SettingsManager.Instance.HideHud;
        hideHudToggle.onValueChanged.RemoveAllListeners();
        hideHudToggle.onValueChanged.AddListener(v => SettingsManager.Instance.HideHud = v);
    }

    private void SetupLanguage()
    {
        if (languageDropdown == null) return;
        // El dropdown ya debería tener las opciones definidas en el inspector (Español, English, etc.)
        // El array languageCodes mapea el índice del dropdown al código de idioma.
        int currentIdx = LanguageCodeToIndex(SettingsManager.Instance.Language);
        languageDropdown.value = currentIdx;
        languageDropdown.onValueChanged.RemoveAllListeners();
        languageDropdown.onValueChanged.AddListener(v =>
        {
            if (v >= 0 && v < languageCodes.Length)
                SettingsManager.Instance.Language = languageCodes[v];
        });
    }

    private void SetupDifficulty()
    {
        if (difficultyDropdown == null) return;
        // El dropdown debería tener: Fácil, Normal, Difícil (en el inspector)
        difficultyDropdown.value = SettingsManager.Instance.Difficulty;
        difficultyDropdown.onValueChanged.RemoveAllListeners();
        difficultyDropdown.onValueChanged.AddListener(v => SettingsManager.Instance.Difficulty = v);
    }

    private int LanguageCodeToIndex(string code)
    {
        for (int i = 0; i < languageCodes.Length; i++)
            if (languageCodes[i] == code) return i;
        return 0;
    }

    private void UpdateFovLabel(float v)
    {
        if (fovLabel != null) fovLabel.text = $"FOV: {Mathf.RoundToInt(v)}°";
    }
}