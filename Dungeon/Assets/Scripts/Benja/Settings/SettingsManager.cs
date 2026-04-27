using System;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    private const string KEY_SENSITIVITY = "settings_sensitivity";
    private const string KEY_INVERT_X = "settings_invert_x";
    private const string KEY_INVERT_Y = "settings_invert_y";

    private const float DEFAULT_SENSITIVITY = 1.0f;
    private const bool DEFAULT_INVERT_X = false;
    private const bool DEFAULT_INVERT_Y = false;

    [Header("Rango del slider de sensibilidad")]
    [SerializeField] private float minSensitivity = 0.1f;
    [SerializeField] private float maxSensitivity = 5f;

    public float MinSensitivity => minSensitivity;
    public float MaxSensitivity => maxSensitivity;

    private float _sensitivity;
    private bool _invertX;
    private bool _invertY;

    public float Sensitivity
    {
        get => _sensitivity;
        set
        {
            _sensitivity = Mathf.Clamp(value, minSensitivity, maxSensitivity);
            PlayerPrefs.SetFloat(KEY_SENSITIVITY, _sensitivity);
            OnSettingsChanged?.Invoke();
        }
    }

    public bool InvertX
    {
        get => _invertX;
        set
        {
            _invertX = value;
            PlayerPrefs.SetInt(KEY_INVERT_X, value ? 1 : 0);
            OnSettingsChanged?.Invoke();
        }
    }

    public bool InvertY
    {
        get => _invertY;
        set
        {
            _invertY = value;
            PlayerPrefs.SetInt(KEY_INVERT_Y, value ? 1 : 0);
            OnSettingsChanged?.Invoke();
        }
    }

    public event Action OnSettingsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        LoadFromPrefs();
    }

    private void LoadFromPrefs()
    {
        _sensitivity = PlayerPrefs.GetFloat(KEY_SENSITIVITY, DEFAULT_SENSITIVITY);
        _invertX = PlayerPrefs.GetInt(KEY_INVERT_X, DEFAULT_INVERT_X ? 1 : 0) == 1;
        _invertY = PlayerPrefs.GetInt(KEY_INVERT_Y, DEFAULT_INVERT_Y ? 1 : 0) == 1;

        OnSettingsChanged?.Invoke();
    }

    public void Flush() => PlayerPrefs.Save();

    public void ResetToDefaults()
    {
        Sensitivity = DEFAULT_SENSITIVITY;
        InvertX = DEFAULT_INVERT_X;
        InvertY = DEFAULT_INVERT_Y;
        Flush();
    }
}
