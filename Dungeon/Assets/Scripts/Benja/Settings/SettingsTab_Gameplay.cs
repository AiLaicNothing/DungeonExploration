using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class SettingsTab_Gameplay : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private Toggle hideHudToggle;

    [Header("Dificultad")]
    [SerializeField] private TMP_Dropdown difficultyDropdown;
    private readonly string[] _difficultyLabels = { "Fácil", "Normal", "Difícil" };

    [Header("Idioma")]
    [SerializeField] private TMP_Dropdown languageDropdown;
    // Pares (código, etiqueta visible). Añade más cuando añadas idiomas.
    private readonly (string code, string label)[] _languages =
    {
        ("es", "Español"),
        ("en", "English"),
    };

    [Header("FOV")]
    [SerializeField] private Slider fovSlider;
    [SerializeField] private TMP_Text fovLabel;

    void OnEnable()
    {
        if (SettingsManager.Instance == null) return;
        var s = SettingsManager.Instance;

        // HUD
        hideHudToggle.SetIsOnWithoutNotify(s.HideHud);
        hideHudToggle.onValueChanged.AddListener(v => s.HideHud = v);

        // Dificultad
        difficultyDropdown.ClearOptions();
        difficultyDropdown.AddOptions(new List<string>(_difficultyLabels));
        difficultyDropdown.SetValueWithoutNotify(Mathf.Clamp(s.Difficulty, 0, _difficultyLabels.Length - 1));
        difficultyDropdown.onValueChanged.AddListener(v => s.Difficulty = v);

        // Idioma
        languageDropdown.ClearOptions();
        var langLabels = new List<string>();
        foreach (var l in _languages) langLabels.Add(l.label);
        languageDropdown.AddOptions(langLabels);
        int langIdx = System.Array.FindIndex(_languages, l => l.code == s.Language);
        if (langIdx < 0) langIdx = 0;
        languageDropdown.SetValueWithoutNotify(langIdx);
        languageDropdown.onValueChanged.AddListener(v => s.Language = _languages[v].code);

        // FOV
        fovSlider.minValue = s.MinFOV;
        fovSlider.maxValue = s.MaxFOV;
        fovSlider.SetValueWithoutNotify(s.FOV);
        UpdateFOVLabel(s.FOV);
        fovSlider.onValueChanged.AddListener(OnFOVChanged);
    }

    void OnDisable()
    {
        if (hideHudToggle != null) hideHudToggle.onValueChanged.RemoveAllListeners();
        if (difficultyDropdown != null) difficultyDropdown.onValueChanged.RemoveAllListeners();
        if (languageDropdown != null) languageDropdown.onValueChanged.RemoveAllListeners();
        if (fovSlider != null) fovSlider.onValueChanged.RemoveAllListeners();
    }

    private void OnFOVChanged(float value)
    {
        SettingsManager.Instance.FOV = value;
        UpdateFOVLabel(value);
    }

    private void UpdateFOVLabel(float value)
    {
        if (fovLabel != null) fovLabel.text = $"{value:F0}°";
    }
}