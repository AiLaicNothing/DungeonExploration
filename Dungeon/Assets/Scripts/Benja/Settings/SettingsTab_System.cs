using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Tab "Sistema" del menú.
///
/// Funciones:
/// - Guardar y salir
/// - Salir sin guardar
/// - Salir del juego
/// - Reset settings
/// - Confirmaciones
///
/// Multiplayer-safe:
/// - SOLO el host guarda
/// - Se prepara shutdown antes de salir
/// - SessionManager limpia correctamente la sesión
/// </summary>
public class SettingsTab_System : MonoBehaviour
{
    [Header("Botones")]
    [SerializeField] private Button saveAndLeaveButton;
    [SerializeField] private Button leaveWithoutSavingButton;
    [SerializeField] private Button quitGameButton;
    [SerializeField] private Button resetSettingsButton;

    [Header("Confirmación")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TMP_Text confirmText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    private System.Action _pendingAction;

    // ════════════════════════════════════════════════════════════════
    // ENABLE / DISABLE
    // ════════════════════════════════════════════════════════════════

    private void OnEnable()
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        // ─────────────────────────────────────────────
        // BOTONES
        // ─────────────────────────────────────────────

        if (saveAndLeaveButton != null)
            saveAndLeaveButton.onClick.AddListener(OnSaveAndLeaveClicked);

        if (leaveWithoutSavingButton != null)
            leaveWithoutSavingButton.onClick.AddListener(OnLeaveWithoutSavingClicked);

        if (quitGameButton != null)
            quitGameButton.onClick.AddListener(OnQuitClicked);

        if (resetSettingsButton != null)
            resetSettingsButton.onClick.AddListener(OnResetSettings);

        // ─────────────────────────────────────────────
        // CONFIRM PANEL
        // ─────────────────────────────────────────────

        if (confirmYesButton != null)
            confirmYesButton.onClick.AddListener(OnConfirmYes);

        if (confirmNoButton != null)
            confirmNoButton.onClick.AddListener(OnConfirmNo);

        // ─────────────────────────────────────────────
        // VISIBILIDAD
        // ─────────────────────────────────────────────

        bool inSession =
            SessionManager.Instance != null &&
            SessionManager.Instance.CurrentSession != null;

        if (saveAndLeaveButton != null)
            saveAndLeaveButton.gameObject.SetActive(inSession);

        if (leaveWithoutSavingButton != null)
            leaveWithoutSavingButton.gameObject.SetActive(inSession);
    }

    private void OnDisable()
    {
        if (saveAndLeaveButton != null)
            saveAndLeaveButton.onClick.RemoveListener(OnSaveAndLeaveClicked);

        if (leaveWithoutSavingButton != null)
            leaveWithoutSavingButton.onClick.RemoveListener(OnLeaveWithoutSavingClicked);

        if (quitGameButton != null)
            quitGameButton.onClick.RemoveListener(OnQuitClicked);

        if (resetSettingsButton != null)
            resetSettingsButton.onClick.RemoveListener(OnResetSettings);

        if (confirmYesButton != null)
            confirmYesButton.onClick.RemoveListener(OnConfirmYes);

        if (confirmNoButton != null)
            confirmNoButton.onClick.RemoveListener(OnConfirmNo);
    }

    // ════════════════════════════════════════════════════════════════
    // BOTONES
    // ════════════════════════════════════════════════════════════════

    private void OnSaveAndLeaveClicked()
    {
        ShowConfirm(
            "¿Guardar la partida y salir de la sesión?",
            DoSaveAndLeave
        );
    }

    private void OnLeaveWithoutSavingClicked()
    {
        ShowConfirm(
            "¿Salir sin guardar?",
            DoLeaveWithoutSaving
        );
    }

    private void OnQuitClicked()
    {
        bool inSession =
            SessionManager.Instance != null &&
            SessionManager.Instance.CurrentSession != null;

        string msg = inSession
            ? "¿Salir del juego? El progreso será guardado."
            : "¿Salir del juego?";

        ShowConfirm(msg, DoQuit);
    }

    private void OnResetSettings()
    {
        ShowConfirm(
            "¿Restaurar todas las opciones a sus valores por defecto?",
            DoResetSettings
        );
    }

    // ════════════════════════════════════════════════════════════════
    // CONFIRM PANEL
    // ════════════════════════════════════════════════════════════════

    private void ShowConfirm(
        string text,
        System.Action action
    )
    {
        if (confirmPanel == null)
        {
            action?.Invoke();
            return;
        }

        if (confirmText != null)
            confirmText.text = text;

        _pendingAction = action;

        confirmPanel.SetActive(true);
    }

    private void OnConfirmYes()
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        _pendingAction?.Invoke();
        _pendingAction = null;
    }

    private void OnConfirmNo()
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        _pendingAction = null;
    }

    // ════════════════════════════════════════════════════════════════
    // IMPLEMENTACIONES
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Guardar y salir.
    /// SOLO el host guarda.
    /// </summary>
    private async void DoSaveAndLeave()
    {
        Time.timeScale = 1f;

        SettingsManager.Instance?.Flush();

        bool isServer =
            SaveGameIntegration.Instance != null &&
            SaveGameIntegration.Instance.IsServer;

        // SOLO EL HOST/SERVIDOR GUARDA
        if (isServer)
        {
            SaveGameIntegration.Instance.PerformManualSave();
            SaveGameIntegration.Instance.PrepareForShutdown();
        }

        // Salir de sesión
        if (SessionManager.Instance != null &&
            SessionManager.Instance.CurrentSession != null)
        {
            await SessionManager.Instance.LeaveSession();
        }
        else
        {
            SceneManager.LoadScene("02_MainMenu");
        }
    }

    /// <summary>
    /// Salir sin guardar.
    /// </summary>
    private async void DoLeaveWithoutSaving()
    {
        Time.timeScale = 1f;

        SettingsManager.Instance?.Flush();

        if (SaveGameIntegration.Instance != null)
        {
            SaveGameIntegration.Instance.PrepareForShutdown();
        }

        if (SessionManager.Instance != null &&
            SessionManager.Instance.CurrentSession != null)
        {
            await SessionManager.Instance.LeaveSession();
        }
        else
        {
            SceneManager.LoadScene("02_MainMenu");
        }
    }

    /// <summary>
    /// Salir del juego.
    /// </summary>
    private async void DoQuit()
    {
        Time.timeScale = 1f;

        SettingsManager.Instance?.Flush();

        bool isHost =
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsServer;

        // Preparar shutdown
        if (SaveGameIntegration.Instance != null)
        {
            SaveGameIntegration.Instance.PrepareForShutdown();
        }

        // Guardar si eres host
        if (isHost)
        {
            SaveGameIntegration.Instance?.PerformManualSave();
        }

        // Salir sesión
        if (SessionManager.Instance != null &&
            SessionManager.Instance.CurrentSession != null)
        {
            try
            {
                await SessionManager.Instance.LeaveSession();
            }
            catch
            {
                // ignorar
            }
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Restaurar settings default.
    /// </summary>
    private void DoResetSettings()
    {
        SettingsManager.Instance?.ResetToDefaults();
    }
}