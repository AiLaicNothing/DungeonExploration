using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main Menu completo.
/// 
/// Flujo UX:
/// 
/// CONTINUE
///     -> Carga último save
///     -> Abre PlayModeScreen
///
/// NEW GAME
///     -> Crear save
///     -> Abre PlayModeScreen
///
/// LOAD GAME
///     -> Seleccionar save
///     -> Abre PlayModeScreen
///
/// MULTIPLAYER
///     -> Abre directamente navegador multiplayer
///
/// Luego PlayModeScreen:
///     -> Host Game
///     -> Join By Code
///     -> Browse Lobbies
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    // ════════════════════════════════════════════════════════════════
    // PANTALLAS
    // ════════════════════════════════════════════════════════════════

    [Header("Pantallas")]
    [SerializeField] private GameObject mainScreen;
    [SerializeField] private GameObject newGameScreen;
    [SerializeField] private GameObject loadGameScreen;

    // NUEVA
    [SerializeField] private GameObject playModeScreen;

    [SerializeField] private GameObject multiplayerScreen;
    [SerializeField] private GameObject createSessionScreen;
    [SerializeField] private GameObject browserScreen;
    [SerializeField] private GameObject joinByCodeScreen;
    [SerializeField] private GameObject optionsScreen;

    // ════════════════════════════════════════════════════════════════
    // PERFIL
    // ════════════════════════════════════════════════════════════════

    [Header("Perfil")]
    [SerializeField] private TMP_Text welcomeText;
    [SerializeField] private TMP_Text playerIdText;
    [SerializeField] private Button changeNameButton;

    [Header("Cambio de nombre")]
    [SerializeField] private GameObject changeNamePanel;
    [SerializeField] private TMP_InputField changeNameInput;
    [SerializeField] private Button changeNameConfirmButton;
    [SerializeField] private Button changeNameCancelButton;

    // ════════════════════════════════════════════════════════════════
    // MAIN SCREEN
    // ════════════════════════════════════════════════════════════════

    [Header("Main Screen")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button multiplayerButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;

    // ════════════════════════════════════════════════════════════════
    // NEW GAME
    // ════════════════════════════════════════════════════════════════

    [Header("Nueva Partida")]
    [SerializeField] private TMP_InputField saveNameInput;
    [SerializeField] private Button createNewGameButton;
    [SerializeField] private Button cancelNewGameButton;

    // ════════════════════════════════════════════════════════════════
    // LOAD GAME
    // ════════════════════════════════════════════════════════════════

    [Header("Cargar Partida")]
    [SerializeField] private Transform loadGameContainer;
    [SerializeField] private GameObject saveSlotEntryPrefab;
    [SerializeField] private Button backFromLoadButton;

    // ════════════════════════════════════════════════════════════════
    // PLAY MODE SCREEN (NUEVO)
    // ════════════════════════════════════════════════════════════════

    [Header("Play Mode Screen")]
    [SerializeField] private Button hostGameButton;
    [SerializeField] private Button browseGamesButton;
    [SerializeField] private Button joinCodeButton;
    [SerializeField] private Button backFromPlayModeButton;

    // ════════════════════════════════════════════════════════════════
    // MULTIPLAYER SCREEN
    // ════════════════════════════════════════════════════════════════

    [Header("Multiplayer")]
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button browseLobbyButton;
    [SerializeField] private Button joinByCodeLobbyButton;
    [SerializeField] private Button rejoinButton;
    [SerializeField] private Button backFromMultiplayerButton;

    // ════════════════════════════════════════════════════════════════
    // CREATE SESSION
    // ════════════════════════════════════════════════════════════════

    [Header("Crear Sesión")]
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private Toggle privateToggle;
    [SerializeField] private Button createSessionConfirmButton;
    [SerializeField] private Button createSessionCancelButton;

    // ════════════════════════════════════════════════════════════════
    // JOIN CODE
    // ════════════════════════════════════════════════════════════════

    [Header("Join By Code")]
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private Button joinCodeConfirmButton;
    [SerializeField] private Button joinCodeCancelButton;

    // ════════════════════════════════════════════════════════════════
    // OPTIONS
    // ════════════════════════════════════════════════════════════════

    [Header("Opciones")]
    [SerializeField] private Button backFromOptionsButton;

    // ════════════════════════════════════════════════════════════════
    // FEEDBACK
    // ════════════════════════════════════════════════════════════════

    [Header("Feedback")]
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private GameObject loadingPanel;

    // ════════════════════════════════════════════════════════════════
    // UNITY
    // ════════════════════════════════════════════════════════════════

    private void Start()
    {
        RefreshProfileHeader();

        RegisterButtons();

        ShowScreen(mainScreen);

        RefreshContinueButton();
        RefreshRejoinButton();

        if (changeNamePanel != null)
            changeNamePanel.SetActive(false);

        SetLoading(false);
        ShowError("");

        if (SessionManager.Instance != null)
            SessionManager.Instance.OnError += OnSessionError;
    }

    private void OnDestroy()
    {
        if (SessionManager.Instance != null)
            SessionManager.Instance.OnError -= OnSessionError;
    }

    // ════════════════════════════════════════════════════════════════
    // BUTTONS
    // ════════════════════════════════════════════════════════════════

    private void RegisterButtons()
    {
        // MAIN
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);

        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnNewGameClicked);

        if (loadGameButton != null)
            loadGameButton.onClick.AddListener(OnLoadGameClicked);

        if (multiplayerButton != null)
            multiplayerButton.onClick.AddListener(() => ShowScreen(multiplayerScreen));

        if (optionsButton != null)
            optionsButton.onClick.AddListener(() => ShowScreen(optionsScreen));

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        // NEW GAME
        if (createNewGameButton != null)
            createNewGameButton.onClick.AddListener(OnCreateNewGameConfirmed);

        if (cancelNewGameButton != null)
            cancelNewGameButton.onClick.AddListener(() => ShowScreen(mainScreen));

        // LOAD
        if (backFromLoadButton != null)
            backFromLoadButton.onClick.AddListener(() => ShowScreen(mainScreen));

        // PLAY MODE
        if (hostGameButton != null)
            hostGameButton.onClick.AddListener(() => ShowScreen(createSessionScreen));

        if (browseGamesButton != null)
            browseGamesButton.onClick.AddListener(() => ShowScreen(browserScreen));

        if (joinCodeButton != null)
            joinCodeButton.onClick.AddListener(() => ShowScreen(joinByCodeScreen));

        if (backFromPlayModeButton != null)
            backFromPlayModeButton.onClick.AddListener(() => ShowScreen(mainScreen));

        // MULTIPLAYER
        if (createLobbyButton != null)
            createLobbyButton.onClick.AddListener(() => ShowScreen(createSessionScreen));

        if (browseLobbyButton != null)
            browseLobbyButton.onClick.AddListener(() => ShowScreen(browserScreen));

        if (joinByCodeLobbyButton != null)
            joinByCodeLobbyButton.onClick.AddListener(() => ShowScreen(joinByCodeScreen));

        if (backFromMultiplayerButton != null)
            backFromMultiplayerButton.onClick.AddListener(() => ShowScreen(mainScreen));

        if (rejoinButton != null)
            rejoinButton.onClick.AddListener(OnRejoinClicked);

        // CREATE SESSION
        if (createSessionConfirmButton != null)
            createSessionConfirmButton.onClick.AddListener(OnCreateSessionConfirm);

        if (createSessionCancelButton != null)
            createSessionCancelButton.onClick.AddListener(() => ShowScreen(playModeScreen));

        // JOIN CODE
        if (joinCodeConfirmButton != null)
            joinCodeConfirmButton.onClick.AddListener(OnJoinByCodeConfirm);

        if (joinCodeCancelButton != null)
            joinCodeCancelButton.onClick.AddListener(() => ShowScreen(playModeScreen));

        // OPTIONS
        if (backFromOptionsButton != null)
            backFromOptionsButton.onClick.AddListener(() => ShowScreen(mainScreen));

        // CHANGE NAME
        if (changeNameButton != null)
            changeNameButton.onClick.AddListener(OpenChangeName);

        if (changeNameConfirmButton != null)
            changeNameConfirmButton.onClick.AddListener(OnChangeNameConfirm);

        if (changeNameCancelButton != null)
            changeNameCancelButton.onClick.AddListener(CloseChangeName);
    }

    // ════════════════════════════════════════════════════════════════
    // NAVIGATION
    // ════════════════════════════════════════════════════════════════

    private void ShowScreen(GameObject target)
    {
        if (mainScreen != null) mainScreen.SetActive(target == mainScreen);
        if (newGameScreen != null) newGameScreen.SetActive(target == newGameScreen);
        if (loadGameScreen != null) loadGameScreen.SetActive(target == loadGameScreen);
        if (playModeScreen != null) playModeScreen.SetActive(target == playModeScreen);

        if (multiplayerScreen != null)
            multiplayerScreen.SetActive(target == multiplayerScreen);

        if (createSessionScreen != null)
            createSessionScreen.SetActive(target == createSessionScreen);

        if (browserScreen != null)
            browserScreen.SetActive(target == browserScreen);

        if (joinByCodeScreen != null)
            joinByCodeScreen.SetActive(target == joinByCodeScreen);

        if (optionsScreen != null)
            optionsScreen.SetActive(target == optionsScreen);

        ShowError("");
    }

    // ════════════════════════════════════════════════════════════════
    // PROFILE
    // ════════════════════════════════════════════════════════════════

    private void RefreshProfileHeader()
    {
        if (welcomeText != null)
            welcomeText.text = $"¡Bienvenido, {PlayerProfile.Name}!";

        if (playerIdText != null)
            playerIdText.text = $"ID: {PlayerProfile.PlayerId}";
    }

    private void OpenChangeName()
    {
        if (changeNamePanel == null) return;

        if (changeNameInput != null)
            changeNameInput.text = PlayerProfile.Name;

        changeNamePanel.SetActive(true);
    }

    private void CloseChangeName()
    {
        if (changeNamePanel != null)
            changeNamePanel.SetActive(false);
    }

    private void OnChangeNameConfirm()
    {
        string trimmed = changeNameInput.text.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
            return;

        PlayerProfile.Name = trimmed;

        RefreshProfileHeader();

        CloseChangeName();
    }

    // ════════════════════════════════════════════════════════════════
    // NEW GAME
    // ════════════════════════════════════════════════════════════════

    private void OnNewGameClicked()
    {
        ShowScreen(newGameScreen);

        if (saveNameInput != null)
        {
            int count = SaveSlotManager.Instance.GetAllSlotsMetadata().Count;
            saveNameInput.text = $"Partida {count + 1}";
        }
    }

    private void OnCreateNewGameConfirmed()
    {
        string saveName = saveNameInput != null
            ? saveNameInput.text.Trim()
            : "Nueva Partida";

        if (string.IsNullOrWhiteSpace(saveName))
            saveName = "Partida Sin Nombre";

        SaveSlotManager.Instance.CreateNewSlot(saveName);

        ShowScreen(playModeScreen);
    }

    // ════════════════════════════════════════════════════════════════
    // CONTINUE
    // ════════════════════════════════════════════════════════════════

    private void OnContinueClicked()
    {
        var slots = SaveSlotManager.Instance.GetAllSlotsMetadata();

        if (slots.Count == 0)
        {
            ShowError("No hay partidas guardadas.");
            return;
        }

        var lastPlayed = slots[0];

        LoadSave(lastPlayed.saveId);
    }

    // ════════════════════════════════════════════════════════════════
    // LOAD GAME
    // ════════════════════════════════════════════════════════════════

    private void OnLoadGameClicked()
    {
        ShowScreen(loadGameScreen);

        RefreshLoadGameList();
    }

    private void RefreshLoadGameList()
    {
        if (loadGameContainer == null || saveSlotEntryPrefab == null)
            return;

        foreach (Transform child in loadGameContainer)
            Destroy(child.gameObject);

        var slots = SaveSlotManager.Instance.GetAllSlotsMetadata();

        foreach (var slot in slots)
        {
            GameObject entry = Instantiate(saveSlotEntryPrefab, loadGameContainer);

            SaveSlotEntryUI entryUI = entry.GetComponent<SaveSlotEntryUI>();

            if (entryUI != null)
            {
                entryUI.Setup(
                    slot,
                    OnSlotLoadClicked,
                    OnSlotDeleteClicked
                );
            }
        }
    }

    private void OnSlotLoadClicked(string saveId)
    {
        LoadSave(saveId);
    }

    private void LoadSave(string saveId)
    {
        bool success = SaveSlotManager.Instance.LoadSlot(saveId);

        if (!success)
        {
            ShowError("No se pudo cargar la partida.");
            return;
        }

        ShowScreen(playModeScreen);
    }

    private void OnSlotDeleteClicked(string saveId)
    {
        SaveSlotManager.Instance.DeleteSlot(saveId);

        RefreshLoadGameList();

        RefreshContinueButton();
    }

    // ════════════════════════════════════════════════════════════════
    // MULTIPLAYER
    // ════════════════════════════════════════════════════════════════

    private async void OnCreateSessionConfirm()
    {
        string roomName = string.IsNullOrWhiteSpace(roomNameInput.text)
            ? $"Sala de {PlayerProfile.Name}"
            : roomNameInput.text;

        bool isPrivate = privateToggle != null && privateToggle.isOn;

        SetLoading(true);

        await SessionManager.Instance.CreateSession(roomName, isPrivate);

        SetLoading(false);
    }

    private async void OnJoinByCodeConfirm()
    {
        string code = codeInput.text.Trim();

        if (string.IsNullOrWhiteSpace(code))
        {
            ShowError("Introduce un código.");
            return;
        }

        SetLoading(true);

        await SessionManager.Instance.JoinSessionByCode(code);

        SetLoading(false);
    }

    private async void OnRejoinClicked()
    {
        if (RejoinManager.Instance == null)
            return;

        SetLoading(true);

        bool ok = await RejoinManager.Instance.TryRejoin();

        SetLoading(false);

        if (!ok)
        {
            ShowError("La sesión ya no existe.");

            RefreshRejoinButton();
        }
    }

    // ════════════════════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════════════════════

    private void RefreshContinueButton()
    {
        if (continueButton == null)
            return;

        continueButton.interactable =
            SaveSlotManager.Instance.GetAllSlotsMetadata().Count > 0;
    }

    private void RefreshRejoinButton()
    {
        if (rejoinButton == null)
            return;

        bool canRejoin =
            RejoinManager.Instance != null &&
            RejoinManager.Instance.HasRecentSession;

        rejoinButton.gameObject.SetActive(canRejoin);

        if (canRejoin)
        {
            TMP_Text label = rejoinButton.GetComponentInChildren<TMP_Text>();

            if (label != null)
                label.text = $"Reconectar a {RejoinManager.Instance.LastSessionName}";
        }
    }

    private void ShowError(string msg)
    {
        if (errorText != null)
            errorText.text = msg;
    }

    private void SetLoading(bool loading)
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(loading);
    }

    private void OnSessionError(string message)
    {
        ShowError(message);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}