using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Tab de "Sistema" del menú de pausa. En multiplayer:
///   - Reiniciar escena → solo permitido si estás solo (host sin clientes)
///   - Salir al menú → siempre permitido (sale de la sesión actual)
///   - Salir del juego → cierra la app
///   - Reset settings → resetea las opciones (volumen, FOV, etc.)
///
/// El botón "borrar progreso" se reubica en el menú principal (ResetProgressButton),
/// no aquí — borrar datos en mitad de una partida no tiene sentido.
/// </summary>
public class SettingsTab_System : MonoBehaviour
{
    [Header("Botones")]
    [SerializeField] private Button restartSceneButton;
    [SerializeField] private Button leaveSessionButton;     // ← reemplaza al de "borrar progreso"
    [SerializeField] private Button quitGameButton;
    [SerializeField] private Button resetSettingsButton;

    [Header("Confirmación de salir")]
    [SerializeField] private GameObject confirmLeavePanel;
    [SerializeField] private Button confirmLeaveYesButton;
    [SerializeField] private Button confirmLeaveNoButton;

    void OnEnable()
    {
        if (confirmLeavePanel != null) confirmLeavePanel.SetActive(false);

        if (restartSceneButton != null) restartSceneButton.onClick.AddListener(OnRestartScene);
        if (leaveSessionButton != null) leaveSessionButton.onClick.AddListener(OnLeaveSessionClicked);
        if (quitGameButton != null) quitGameButton.onClick.AddListener(OnQuitGame);
        if (resetSettingsButton != null) resetSettingsButton.onClick.AddListener(OnResetSettings);

        if (confirmLeaveYesButton != null) confirmLeaveYesButton.onClick.AddListener(OnConfirmLeaveYes);
        if (confirmLeaveNoButton != null) confirmLeaveNoButton.onClick.AddListener(OnConfirmLeaveNo);

        UpdateButtonStates();
    }

    void OnDisable()
    {
        if (restartSceneButton != null) restartSceneButton.onClick.RemoveListener(OnRestartScene);
        if (leaveSessionButton != null) leaveSessionButton.onClick.RemoveListener(OnLeaveSessionClicked);
        if (quitGameButton != null) quitGameButton.onClick.RemoveListener(OnQuitGame);
        if (resetSettingsButton != null) resetSettingsButton.onClick.RemoveListener(OnResetSettings);
        if (confirmLeaveYesButton != null) confirmLeaveYesButton.onClick.RemoveListener(OnConfirmLeaveYes);
        if (confirmLeaveNoButton != null) confirmLeaveNoButton.onClick.RemoveListener(OnConfirmLeaveNo);
    }

    /// <summary>
    /// Activa/desactiva botones según si estamos en una sesión multiplayer.
    /// "Reiniciar escena" solo tiene sentido si estás solo (sin clientes conectados).
    /// </summary>
    private void UpdateButtonStates()
    {
        bool inSession = SessionManager.Instance != null && SessionManager.Instance.CurrentSession != null;
        bool isHostAlone = NetworkManager.Singleton != null
                        && NetworkManager.Singleton.IsHost
                        && NetworkManager.Singleton.ConnectedClients.Count <= 1;

        if (restartSceneButton != null)
            restartSceneButton.interactable = !inSession || isHostAlone;

        if (leaveSessionButton != null)
            leaveSessionButton.gameObject.SetActive(inSession);
    }

    // ── Reiniciar escena ──────────────────────────────────────────────
    private void OnRestartScene()
    {
        Time.timeScale = 1f;
        SettingsManager.Instance?.Flush();

        if (SessionManager.Instance != null && SessionManager.Instance.CurrentSession != null)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(
                    SceneManager.GetActiveScene().name,
                    LoadSceneMode.Single);
            }
            else
            {
                Debug.LogWarning("[SettingsTab_System] Solo el host puede reiniciar la escena en multiplayer.");
            }
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ── Salir de la sala ─────────────────────────────────────────────
    private void OnLeaveSessionClicked()
    {
        if (confirmLeavePanel != null) confirmLeavePanel.SetActive(true);
        else DoLeaveSession();
    }

    private void OnConfirmLeaveYes()
    {
        if (confirmLeavePanel != null) confirmLeavePanel.SetActive(false);
        DoLeaveSession();
    }

    private void OnConfirmLeaveNo()
    {
        if (confirmLeavePanel != null) confirmLeavePanel.SetActive(false);
    }

    private async void DoLeaveSession()
    {
        Time.timeScale = 1f;

        if (SessionManager.Instance != null && SessionManager.Instance.CurrentSession != null)
        {
            await SessionManager.Instance.LeaveSession();
        }
        else
        {
            SceneManager.LoadScene("02_MainMenu");
        }
    }

    // ── Salir del juego ───────────────────────────────────────────────
    private void OnQuitGame()
    {
        SettingsManager.Instance?.Flush();

        if (SessionManager.Instance != null && SessionManager.Instance.CurrentSession != null)
        {
            _ = SessionManager.Instance.LeaveSession();
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Reset settings ────────────────────────────────────────────────
    private void OnResetSettings()
    {
        SettingsManager.Instance?.ResetToDefaults();
    }
}