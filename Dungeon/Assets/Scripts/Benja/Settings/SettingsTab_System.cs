using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Tab "Sistema" del menú. En multiplayer:
///   - NO hay "borrar progreso" (eso solo en el menú principal)
///   - NO hay "reiniciar escena" para clientes (rompería la sesión)
///   - SÍ hay "salir de la sala" — vuelve al menú principal limpiamente
///   - SÍ hay "reset settings" — devuelve las opciones a sus defaults
///   - SÍ hay "salir del juego" — cierra la app
/// </summary>
public class SettingsTab_System : MonoBehaviour
{
    [Header("Botones")]
    [SerializeField] private Button leaveSessionButton;
    [SerializeField] private Button quitGameButton;
    [SerializeField] private Button resetSettingsButton;

    [Header("Confirmación")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TMP_Text confirmText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    private System.Action _pendingAction;

    void OnEnable()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);

        if (leaveSessionButton != null) leaveSessionButton.onClick.AddListener(OnLeaveSessionClicked);
        if (quitGameButton != null) quitGameButton.onClick.AddListener(OnQuitClicked);
        if (resetSettingsButton != null) resetSettingsButton.onClick.AddListener(OnResetSettings);

        if (confirmYesButton != null) confirmYesButton.onClick.AddListener(OnConfirmYes);
        if (confirmNoButton != null) confirmNoButton.onClick.AddListener(OnConfirmNo);

        // Visibilidad según contexto
        bool inSession = SessionManager.Instance != null && SessionManager.Instance.CurrentSession != null;
        if (leaveSessionButton != null)
            leaveSessionButton.gameObject.SetActive(inSession);
    }

    void OnDisable()
    {
        if (leaveSessionButton != null) leaveSessionButton.onClick.RemoveListener(OnLeaveSessionClicked);
        if (quitGameButton != null) quitGameButton.onClick.RemoveListener(OnQuitClicked);
        if (resetSettingsButton != null) resetSettingsButton.onClick.RemoveListener(OnResetSettings);
        if (confirmYesButton != null) confirmYesButton.onClick.RemoveListener(OnConfirmYes);
        if (confirmNoButton != null) confirmNoButton.onClick.RemoveListener(OnConfirmNo);
    }

    // ── Acciones ──────────────────────────────────────────────────────
    private void OnLeaveSessionClicked()
    {
        ShowConfirm("¿Salir de la sala? Tu progreso se guardará y podrás reconectar desde el menú principal.", DoLeaveSession);
    }

    private void OnQuitClicked()
    {
        bool inSession = SessionManager.Instance != null && SessionManager.Instance.CurrentSession != null;
        string msg = inSession
            ? "¿Salir del juego? Tu progreso se guardará."
            : "¿Salir del juego?";
        ShowConfirm(msg, DoQuit);
    }

    private void OnResetSettings()
    {
        ShowConfirm("¿Restaurar todas las opciones a sus valores por defecto?", DoResetSettings);
    }

    // ── Confirmación ──────────────────────────────────────────────────
    private void ShowConfirm(string text, System.Action action)
    {
        if (confirmPanel == null)
        {
            action?.Invoke();
            return;
        }

        if (confirmText != null) confirmText.text = text;
        _pendingAction = action;
        confirmPanel.SetActive(true);
    }

    private void OnConfirmYes()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
        _pendingAction?.Invoke();
        _pendingAction = null;
    }

    private void OnConfirmNo()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
        _pendingAction = null;
    }

    // ── Implementaciones ──────────────────────────────────────────────
    private async void DoLeaveSession()
    {
        Time.timeScale = 1f;
        SettingsManager.Instance?.Flush();

        if (SessionManager.Instance != null && SessionManager.Instance.CurrentSession != null)
        {
            await SessionManager.Instance.LeaveSession();
            // SessionManager carga el menú principal automáticamente
        }
        else
        {
            SceneManager.LoadScene("02_MainMenu");
        }
    }

    private async void DoQuit()
    {
        Time.timeScale = 1f;
        SettingsManager.Instance?.Flush();

        if (SessionManager.Instance != null && SessionManager.Instance.CurrentSession != null)
        {
            try { await SessionManager.Instance.LeaveSession(); } catch { }
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void DoResetSettings()
    {
        SettingsManager.Instance?.ResetToDefaults();
    }
}