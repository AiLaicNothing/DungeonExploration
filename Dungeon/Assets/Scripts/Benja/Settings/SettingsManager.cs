using System;
using UnityEngine;
using UnityEngine.Audio;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    // ── Claves PlayerPrefs ────────────────────────────────────────────
    private const string KEY_SENSITIVITY = "settings_sensitivity";
    private const string KEY_INVERT_X = "settings_invert_x";
    private const string KEY_INVERT_Y = "settings_invert_y";
    private const string KEY_RESOLUTION_INDEX = "settings_resolution_idx";
    private const string KEY_FULLSCREEN = "settings_fullscreen";
    private const string KEY_VSYNC = "settings_vsync";
    private const string KEY_QUALITY = "settings_quality";
    private const string KEY_FPS_LIMIT = "settings_fps_limit";
    private const string KEY_VOL_MASTER = "settings_vol_master";
    private const string KEY_VOL_MUSIC = "settings_vol_music";
    private const string KEY_VOL_SFX = "settings_vol_sfx";
    private const string KEY_VOL_UI = "settings_vol_ui";
    private const string KEY_HIDE_HUD = "settings_hide_hud";
    private const string KEY_DIFFICULTY = "settings_difficulty";
    private const string KEY_LANGUAGE = "settings_language";
    private const string KEY_FOV = "settings_fov";

    // ── Defaults ──────────────────────────────────────────────────────
    private const float DEFAULT_SENSITIVITY = 1.0f;
    private const float DEFAULT_VOLUME = 0.8f;
    private const float DEFAULT_FOV = 60f;
    private const int DEFAULT_FPS_LIMIT = -1;
    private const int DEFAULT_DIFFICULTY = 1;
    private const string DEFAULT_LANGUAGE = "es";

    [Header("Referencias")]
    [Tooltip("AudioMixer con parámetros expuestos: MasterVolume, MusicVolume, SFXVolume, UIVolume.")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Rangos")]
    [SerializeField] private float minSensitivity = 0.1f;
    [SerializeField] private float maxSensitivity = 5f;
    [SerializeField] private float minFOV = 40f;
    [SerializeField] private float maxFOV = 100f;

    public float MinSensitivity => minSensitivity;
    public float MaxSensitivity => maxSensitivity;
    public float MinFOV => minFOV;
    public float MaxFOV => maxFOV;

    public Resolution[] AvailableResolutions { get; private set; }

    // ── Valores actuales ──────────────────────────────────────────────
    private float _sensitivity;
    private bool _invertX, _invertY;
    private int _resolutionIndex;
    private bool _fullscreen;
    private bool _vsync;
    private int _qualityLevel;
    private int _fpsLimit;
    private float _volMaster, _volMusic, _volSFX, _volUI;
    private bool _hideHud;
    private int _difficulty;
    private string _language;
    private float _fov;

    // ── Eventos ───────────────────────────────────────────────────────
    public event Action OnSettingsChanged;
    public event Action<bool> OnHudVisibilityChanged;
    public event Action<float> OnFOVChanged;
    public event Action<int> OnDifficultyChanged;
    public event Action<string> OnLanguageChanged;

    // ── Properties ────────────────────────────────────────────────────
    public float Sensitivity { get => _sensitivity; set { _sensitivity = Mathf.Clamp(value, minSensitivity, maxSensitivity); PlayerPrefs.SetFloat(KEY_SENSITIVITY, _sensitivity); OnSettingsChanged?.Invoke(); } }
    public bool InvertX { get => _invertX; set { _invertX = value; PlayerPrefs.SetInt(KEY_INVERT_X, value ? 1 : 0); OnSettingsChanged?.Invoke(); } }
    public bool InvertY { get => _invertY; set { _invertY = value; PlayerPrefs.SetInt(KEY_INVERT_Y, value ? 1 : 0); OnSettingsChanged?.Invoke(); } }
    public int ResolutionIndex { get => _resolutionIndex; set { _resolutionIndex = value; PlayerPrefs.SetInt(KEY_RESOLUTION_INDEX, value); ApplyResolution(); } }
    public bool Fullscreen { get => _fullscreen; set { _fullscreen = value; PlayerPrefs.SetInt(KEY_FULLSCREEN, value ? 1 : 0); ApplyResolution(); } }
    public bool VSync { get => _vsync; set { _vsync = value; PlayerPrefs.SetInt(KEY_VSYNC, value ? 1 : 0); QualitySettings.vSyncCount = value ? 1 : 0; } }
    public int QualityLevel { get => _qualityLevel; set { _qualityLevel = Mathf.Clamp(value, 0, QualitySettings.names.Length - 1); PlayerPrefs.SetInt(KEY_QUALITY, _qualityLevel); QualitySettings.SetQualityLevel(_qualityLevel, true); } }
    public int FPSLimit { get => _fpsLimit; set { _fpsLimit = value; PlayerPrefs.SetInt(KEY_FPS_LIMIT, value); Application.targetFrameRate = value; } }
    public float VolumeMaster { get => _volMaster; set { _volMaster = Mathf.Clamp01(value); PlayerPrefs.SetFloat(KEY_VOL_MASTER, _volMaster); ApplyMixerVolume("MasterVolume", _volMaster); } }
    public float VolumeMusic { get => _volMusic; set { _volMusic = Mathf.Clamp01(value); PlayerPrefs.SetFloat(KEY_VOL_MUSIC, _volMusic); ApplyMixerVolume("MusicVolume", _volMusic); } }
    public float VolumeSFX { get => _volSFX; set { _volSFX = Mathf.Clamp01(value); PlayerPrefs.SetFloat(KEY_VOL_SFX, _volSFX); ApplyMixerVolume("SFXVolume", _volSFX); } }
    public float VolumeUI { get => _volUI; set { _volUI = Mathf.Clamp01(value); PlayerPrefs.SetFloat(KEY_VOL_UI, _volUI); ApplyMixerVolume("UIVolume", _volUI); } }
    public bool HideHud { get => _hideHud; set { _hideHud = value; PlayerPrefs.SetInt(KEY_HIDE_HUD, value ? 1 : 0); OnHudVisibilityChanged?.Invoke(!_hideHud); } }
    public int Difficulty { get => _difficulty; set { _difficulty = value; PlayerPrefs.SetInt(KEY_DIFFICULTY, value); OnDifficultyChanged?.Invoke(value); } }
    public string Language { get => _language; set { _language = value; PlayerPrefs.SetString(KEY_LANGUAGE, value); OnLanguageChanged?.Invoke(value); } }
    public float FOV { get => _fov; set { _fov = Mathf.Clamp(value, minFOV, maxFOV); PlayerPrefs.SetFloat(KEY_FOV, _fov); OnFOVChanged?.Invoke(_fov); } }

    // ── Lifecycle ─────────────────────────────────────────────────────
    private void Awake()
    {
        // Singleton con persistencia entre escenas
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[SettingsManager] Ya existe una instancia (ID: {Instance.GetInstanceID()}), destruyendo duplicado de la escena {gameObject.scene.name} (ID: {GetInstanceID()}).");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        AvailableResolutions = Screen.resolutions;
        LoadFromPrefs();
        ApplyAll();

        Debug.Log($"[SettingsManager] Inicializado y persistente. ID: {GetInstanceID()}");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Debug.LogWarning($"[SettingsManager] DESTRUYENDO la instancia activa (ID: {GetInstanceID()}).");
            Instance = null;
        }
    }

    private void LoadFromPrefs()
    {
        _sensitivity = PlayerPrefs.GetFloat(KEY_SENSITIVITY, DEFAULT_SENSITIVITY);
        _invertX = PlayerPrefs.GetInt(KEY_INVERT_X, 0) == 1;
        _invertY = PlayerPrefs.GetInt(KEY_INVERT_Y, 0) == 1;

        int currentIdx = FindCurrentResolutionIndex();
        _resolutionIndex = PlayerPrefs.GetInt(KEY_RESOLUTION_INDEX, currentIdx);
        _resolutionIndex = Mathf.Clamp(_resolutionIndex, 0, AvailableResolutions.Length - 1);

        _fullscreen = PlayerPrefs.GetInt(KEY_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
        _vsync = PlayerPrefs.GetInt(KEY_VSYNC, 0) == 1;
        _qualityLevel = PlayerPrefs.GetInt(KEY_QUALITY, QualitySettings.GetQualityLevel());
        _fpsLimit = PlayerPrefs.GetInt(KEY_FPS_LIMIT, DEFAULT_FPS_LIMIT);

        _volMaster = PlayerPrefs.GetFloat(KEY_VOL_MASTER, DEFAULT_VOLUME);
        _volMusic = PlayerPrefs.GetFloat(KEY_VOL_MUSIC, DEFAULT_VOLUME);
        _volSFX = PlayerPrefs.GetFloat(KEY_VOL_SFX, DEFAULT_VOLUME);
        _volUI = PlayerPrefs.GetFloat(KEY_VOL_UI, DEFAULT_VOLUME);

        _hideHud = PlayerPrefs.GetInt(KEY_HIDE_HUD, 0) == 1;
        _difficulty = PlayerPrefs.GetInt(KEY_DIFFICULTY, DEFAULT_DIFFICULTY);
        _language = PlayerPrefs.GetString(KEY_LANGUAGE, DEFAULT_LANGUAGE);
        _fov = PlayerPrefs.GetFloat(KEY_FOV, DEFAULT_FOV);
    }

    private void ApplyAll()
    {
        ApplyResolution();
        QualitySettings.vSyncCount = _vsync ? 1 : 0;
        QualitySettings.SetQualityLevel(_qualityLevel, true);
        Application.targetFrameRate = _fpsLimit;

        ApplyMixerVolume("MasterVolume", _volMaster);
        ApplyMixerVolume("MusicVolume", _volMusic);
        ApplyMixerVolume("SFXVolume", _volSFX);
        ApplyMixerVolume("UIVolume", _volUI);

        OnSettingsChanged?.Invoke();
        OnHudVisibilityChanged?.Invoke(!_hideHud);
        OnFOVChanged?.Invoke(_fov);
        OnDifficultyChanged?.Invoke(_difficulty);
        OnLanguageChanged?.Invoke(_language);
    }

    private void ApplyResolution()
    {
        if (AvailableResolutions == null || AvailableResolutions.Length == 0) return;
        var res = AvailableResolutions[Mathf.Clamp(_resolutionIndex, 0, AvailableResolutions.Length - 1)];
        Screen.SetResolution(res.width, res.height, _fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed, res.refreshRateRatio);
    }

    private void ApplyMixerVolume(string parameter, float linearValue)
    {
        if (audioMixer == null) return;
        float db = linearValue > 0.0001f ? Mathf.Log10(linearValue) * 20f : -80f;
        audioMixer.SetFloat(parameter, db);
    }

    private int FindCurrentResolutionIndex()
    {
        for (int i = 0; i < AvailableResolutions.Length; i++)
        {
            var r = AvailableResolutions[i];
            if (r.width == Screen.currentResolution.width && r.height == Screen.currentResolution.height)
                return i;
        }
        return AvailableResolutions.Length - 1;
    }

    public void Flush() => PlayerPrefs.Save();

    public void ResetToDefaults()
    {
        Sensitivity = DEFAULT_SENSITIVITY;
        InvertX = false; InvertY = false;
        ResolutionIndex = FindCurrentResolutionIndex();
        Fullscreen = true;
        VSync = false;
        QualityLevel = QualitySettings.GetQualityLevel();
        FPSLimit = DEFAULT_FPS_LIMIT;
        VolumeMaster = DEFAULT_VOLUME; VolumeMusic = DEFAULT_VOLUME;
        VolumeSFX = DEFAULT_VOLUME; VolumeUI = DEFAULT_VOLUME;
        HideHud = false;
        Difficulty = DEFAULT_DIFFICULTY;
        Language = DEFAULT_LANGUAGE;
        FOV = DEFAULT_FOV;
        Flush();
    }
}