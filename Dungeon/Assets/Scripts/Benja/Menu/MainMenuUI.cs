using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI del menú principal. Muestra el perfil del jugador (nombre + ID) y los botones
/// para crear sala, abrir browser, unirse por código, salir.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Perfil")]
    [SerializeField] private TMP_Text welcomeText;
    [SerializeField] private TMP_Text playerIdText;
    [SerializeField] private Button changeNameButton;

    [Header("Cambio de nombre (panel)")]
    [SerializeField] private GameObject changeNamePanel;
    [SerializeField] private TMP_InputField changeNameInput;
    [SerializeField] private Button changeNameConfirmButton;
    [SerializeField] private Button changeNameCancelButton;

    [Header("Paneles principales")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject createPanel;
    [SerializeField] private GameObject browserPanel;
    [SerializeField] private GameObject joinByCodePanel;

    [Header("Botones del panel principal")]
    [SerializeField] private Button createButton;
    [SerializeField] private Button browseButton;
    [SerializeField] private Button joinByCodeButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button rejoinButton; // ← NUEVO: solo visible si hay sesión reciente

    [Header("Crear sala")]
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private Toggle privateToggle;
    [SerializeField] private Button createConfirmButton;
    [SerializeField] private Button createCancelButton;

    [Header("Unirse por código")]
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private Button joinCodeConfirmButton;
    [SerializeField] private Button joinCodeCancelButton;

    [Header("Feedback")]
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private GameObject loadingPanel;

    void Start()
    {
        RefreshProfileHeader();

        // Botones del panel principal
        createButton.onClick.AddListener(() => ShowPanel(createPanel));
        browseButton.onClick.AddListener(() => ShowPanel(browserPanel));
        joinByCodeButton.onClick.AddListener(() => ShowPanel(joinByCodePanel));
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

        // Rejoin: solo visible si hay sesión reciente guardada
        if (rejoinButton != null)
        {
            bool canRejoin = RejoinManager.Instance != null && RejoinManager.Instance.HasRecentSession;
            rejoinButton.gameObject.SetActive(canRejoin);
            if (canRejoin)
            {
                rejoinButton.onClick.AddListener(OnRejoinClicked);
                var label = rejoinButton.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = $"Reconectar a {RejoinManager.Instance.LastSessionName}";
            }
        }

        if (changeNameButton != null) changeNameButton.onClick.AddListener(OpenChangeName);
        if (changeNameConfirmButton != null) changeNameConfirmButton.onClick.AddListener(OnChangeNameConfirm);
        if (changeNameCancelButton != null) changeNameCancelButton.onClick.AddListener(CloseChangeName);

        createConfirmButton.onClick.AddListener(OnCreateConfirm);
        createCancelButton.onClick.AddListener(() => ShowPanel(mainPanel));

        joinCodeConfirmButton.onClick.AddListener(OnJoinByCodeConfirm);
        joinCodeCancelButton.onClick.AddListener(() => ShowPanel(mainPanel));

        SessionManager.Instance.OnError += OnSessionError;

        ShowPanel(mainPanel);
        if (changeNamePanel != null) changeNamePanel.SetActive(false);
        if (errorText != null) errorText.text = "";
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (SessionManager.Instance != null)
            SessionManager.Instance.OnError -= OnSessionError;
    }

    private void RefreshProfileHeader()
    {
        if (welcomeText != null)
            welcomeText.text = $"¡Bienvenido, {PlayerProfile.Name}!";

        if (playerIdText != null)
            playerIdText.text = $"ID: {PlayerProfile.PlayerId}";
    }

    private void ShowPanel(GameObject target)
    {
        mainPanel.SetActive(target == mainPanel);
        createPanel.SetActive(target == createPanel);
        browserPanel.SetActive(target == browserPanel);
        joinByCodePanel.SetActive(target == joinByCodePanel);

        if (errorText != null) errorText.text = "";
    }

    // ── Cambiar nombre ────────────────────────────────────────────────
    private void OpenChangeName()
    {
        if (changeNamePanel == null) return;
        if (changeNameInput != null) changeNameInput.text = PlayerProfile.Name;
        changeNamePanel.SetActive(true);
    }

    private void CloseChangeName()
    {
        if (changeNamePanel != null) changeNamePanel.SetActive(false);
    }

    private void OnChangeNameConfirm()
    {
        string trimmed = changeNameInput != null ? changeNameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(trimmed)) return;

        PlayerProfile.Name = trimmed;
        RefreshProfileHeader();
        CloseChangeName();
    }

    // ── Crear sala ────────────────────────────────────────────────────
    private async void OnCreateConfirm()
    {
        string name = string.IsNullOrWhiteSpace(roomNameInput.text)
            ? $"Sala de {PlayerProfile.Name}"
            : roomNameInput.text;

        bool isPrivate = privateToggle != null && privateToggle.isOn;

        SetLoading(true);
        await SessionManager.Instance.CreateSession(name, isPrivate);
        SetLoading(false);
    }

    // ── Unirse por código ─────────────────────────────────────────────
    private async void OnJoinByCodeConfirm()
    {
        string code = codeInput.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            ShowError("Introduce un código.");
            return;
        }

        SetLoading(true);
        await SessionManager.Instance.JoinSessionByCode(code);
        SetLoading(false);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnSessionError(string message) => ShowError(message);

    private async void OnRejoinClicked()
    {
        SetLoading(true);
        ShowError("");
        bool ok = await RejoinManager.Instance.TryRejoin();
        SetLoading(false);

        if (!ok)
        {
            ShowError("La sala ya no existe o ha sido cerrada.");
            // Ocultar el botón ya que la sesión expiró
            if (rejoinButton != null) rejoinButton.gameObject.SetActive(false);
        }
    }

    private void ShowError(string msg)
    {
        if (errorText != null) errorText.text = msg;
    }

    private void SetLoading(bool loading)
    {
        if (loadingPanel != null) loadingPanel.SetActive(loading);
    }
}