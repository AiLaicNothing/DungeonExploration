using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class PauseMenuUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject confirmDeletePanel;

    [Header("Sensibilidad")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TMP_Text sensitivityValueLabel;

    [Header("Invertir Ejes")]
    [SerializeField] private Toggle invertXToggle;
    [SerializeField] private Toggle invertYToggle;

    [Header("Botones")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartSceneButton;
    [SerializeField] private Button deleteSaveButton;
    [SerializeField] private Button quitGameButton;

    [Header("Confirmación borrar progreso")]
    [SerializeField] private Button confirmDeleteYesButton;
    [SerializeField] private Button confirmDeleteNoButton;

    private bool _isPaused = false;

    void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (confirmDeletePanel != null) confirmDeletePanel.SetActive(false);

        // Cursor siempre visible y libre, sin importar el estado del juego
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SetupSensitivitySlider();
        SetupInvertToggles();
        SetupButtons();
    }

    // ── Pausa ─────────────────────────────────────────────────────────
    /// <summary>
    /// Conectar este método al evento del Action "Pause" en el componente PlayerInput.
    /// (Asignado en el inspector como Unity Event, igual que tu OnInteract).
    /// </summary>
    public void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        TogglePause();
    }

    public void TogglePause()
    {
        if (confirmDeletePanel != null && confirmDeletePanel.activeSelf)
        {
            confirmDeletePanel.SetActive(false);
            return;
        }

        _isPaused = !_isPaused;

        if (pausePanel != null) pausePanel.SetActive(_isPaused);

        Time.timeScale = _isPaused ? 0f : 1f;
    }

    // ── Setup de UI ───────────────────────────────────────────────────
    private void SetupSensitivitySlider()
    {
        if (sensitivitySlider == null || SettingsManager.Instance == null) return;

        var s = SettingsManager.Instance;
        sensitivitySlider.minValue = s.MinSensitivity;
        sensitivitySlider.maxValue = s.MaxSensitivity;
        sensitivitySlider.value = s.Sensitivity;

        UpdateSensitivityLabel(s.Sensitivity);

        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
    }

    private void SetupInvertToggles()
    {
        if (SettingsManager.Instance == null) return;

        if (invertXToggle != null)
        {
            invertXToggle.isOn = SettingsManager.Instance.InvertX;
            invertXToggle.onValueChanged.AddListener(OnInvertXChanged);
        }

        if (invertYToggle != null)
        {
            invertYToggle.isOn = SettingsManager.Instance.InvertY;
            invertYToggle.onValueChanged.AddListener(OnInvertYChanged);
        }
    }

    private void SetupButtons()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(TogglePause);
        if (restartSceneButton != null) restartSceneButton.onClick.AddListener(OnRestartScene);
        if (deleteSaveButton != null) deleteSaveButton.onClick.AddListener(OnDeleteSaveClicked);
        if (quitGameButton != null) quitGameButton.onClick.AddListener(OnQuitGame);

        if (confirmDeleteYesButton != null) confirmDeleteYesButton.onClick.AddListener(OnConfirmDeleteYes);
        if (confirmDeleteNoButton != null) confirmDeleteNoButton.onClick.AddListener(OnConfirmDeleteNo);
    }

    // ── Callbacks ─────────────────────────────────────────────────────
    private void OnSensitivityChanged(float value)
    {
        SettingsManager.Instance.Sensitivity = value;
        UpdateSensitivityLabel(value);
    }

    private void OnInvertXChanged(bool value) => SettingsManager.Instance.InvertX = value;
    private void OnInvertYChanged(bool value) => SettingsManager.Instance.InvertY = value;

    private void UpdateSensitivityLabel(float value)
    {
        if (sensitivityValueLabel != null)
            sensitivityValueLabel.text = value.ToString("F2");
    }

    private void OnRestartScene()
    {
        Time.timeScale = 1f;
        SettingsManager.Instance?.Flush();
        var current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    private void OnDeleteSaveClicked()
    {
        if (confirmDeletePanel != null)
            confirmDeletePanel.SetActive(true);
        else
            DoDeleteSave();
    }

    private void OnConfirmDeleteYes()
    {
        if (confirmDeletePanel != null) confirmDeletePanel.SetActive(false);
        DoDeleteSave();
    }

    private void OnConfirmDeleteNo()
    {
        if (confirmDeletePanel != null) confirmDeletePanel.SetActive(false);
    }

    private void DoDeleteSave()
    {
        Time.timeScale = 1f;
        if (Savesystem.Instance != null)
            Savesystem.Instance.ResetAndReloadScene();
        else
            Debug.LogWarning("[PauseMenuUI] No hay Savesystem en la escena.");
    }

    private void OnQuitGame()
    {
        SettingsManager.Instance?.Flush();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}