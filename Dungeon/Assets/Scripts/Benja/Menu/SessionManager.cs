using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton persistente que controla:
/// - Unity Services
/// - Authentication
/// - Multiplayer Sessions
/// - Relay + NGO
/// - Cambio de escenas
/// </summary>
public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private int maxPlayers = 4;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "02_MainMenu";
    [SerializeField] private string lobbySceneName = "03_Lobby";
    [SerializeField] private string gameplaySceneName = "04_Gameplay";

    [Header("Testing")]
    [SerializeField] private bool useRandomProfileForTesting = false;

    [SerializeField]
    private string testProfileName = "";

    public bool IsInitialized { get; private set; }

    public ISession CurrentSession { get; private set; }

    public bool IsHost =>
        CurrentSession != null &&
        CurrentSession.IsHost;

    public string PlayerName
    {
        get => PlayerPrefs.GetString("player_name", "Player");
        set
        {
            PlayerPrefs.SetString("player_name", value);
            PlayerPrefs.Save();
        }
    }

    public event Action OnSessionJoined;
    public event Action OnSessionLeft;
    public event Action<string> OnError;

    // ════════════════════════════════════════════════════════════════
    // UNITY
    // ════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        Debug.Log("[SessionManager] Inicializado.");
    }

    // ════════════════════════════════════════════════════════════════
    // INITIALIZE
    // ════════════════════════════════════════════════════════════════

    public async Task InitializeAsync()
    {
        if (IsInitialized)
            return;

        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                if (useRandomProfileForTesting)
                {
                    string profile =
                        string.IsNullOrWhiteSpace(testProfileName)
                        ? $"test_{Guid.NewGuid().ToString("N")[..8]}"
                        : testProfileName;

                    InitializationOptions options =
                        new InitializationOptions();

                    options.SetProfile(profile);

                    await UnityServices.InitializeAsync(options);

                    Debug.Log($"[SessionManager] Profile: {profile}");
                }
                else
                {
                    await UnityServices.InitializeAsync();
                }
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Debug.Log(
                $"[SessionManager] Auth OK. PlayerID: {AuthenticationService.Instance.PlayerId}"
            );

            IsInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionManager] Initialize error: {e}");
            OnError?.Invoke("Error inicializando servicios.");
        }
    }

    // ════════════════════════════════════════════════════════════════
    // CREATE SESSION
    // ════════════════════════════════════════════════════════════════

    public async Task<bool> CreateSession(
        string sessionName,
        bool isPrivate = false
    )
    {
        try
        {
            SessionOptions options = new SessionOptions
            {
                MaxPlayers = maxPlayers,
                IsPrivate = isPrivate,
                Name = sessionName,
            }
            .WithRelayNetwork();

            CurrentSession =
                await MultiplayerService.Instance.CreateSessionAsync(options);

            Debug.Log(
                $"[SessionManager] Sesión creada: {CurrentSession.Name}"
            );

            await WaitForServerReady();

            OnSessionJoined?.Invoke();

            NetworkManager.Singleton.SceneManager.LoadScene(
                lobbySceneName,
                LoadSceneMode.Single
            );

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionManager] CreateSession error: {e}");

            OnError?.Invoke($"Error creando sala:\n{e.Message}");

            return false;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // JOIN BY ID
    // ════════════════════════════════════════════════════════════════

    public async Task<bool> JoinSessionById(string sessionId)
    {
        try
        {
            CurrentSession =
                await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);

            await WaitForClientConnected();

            OnSessionJoined?.Invoke();

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionManager] JoinSessionById error: {e}");

            OnError?.Invoke($"Error al unirse:\n{e.Message}");

            return false;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // JOIN BY CODE
    // ════════════════════════════════════════════════════════════════

    public async Task<bool> JoinSessionByCode(string code)
    {
        try
        {
            CurrentSession =
                await MultiplayerService.Instance.JoinSessionByCodeAsync(code);

            await WaitForClientConnected();

            OnSessionJoined?.Invoke();

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionManager] JoinSessionByCode error: {e}");

            OnError?.Invoke($"Código inválido o sala llena.");

            return false;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // QUERY
    // ════════════════════════════════════════════════════════════════

    public async Task<QuerySessionsResults> QueryAvailableSessions()
    {
        try
        {
            return await MultiplayerService.Instance.QuerySessionsAsync(
                new QuerySessionsOptions()
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionManager] QuerySessions error: {e}");

            OnError?.Invoke("Error buscando salas.");

            return null;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // LEAVE
    // ════════════════════════════════════════════════════════════════

    public async Task LeaveSession()
    {
        try
        {
            if (CurrentSession != null)
            {
                await CurrentSession.LeaveAsync();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SessionManager] Leave error: {e}");
        }

        CurrentSession = null;

        OnSessionLeft?.Invoke();

        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ════════════════════════════════════════════════════════════════
    // KICK
    // ════════════════════════════════════════════════════════════════

    public async Task KickPlayer(string playerId)
    {
        if (!IsHost || CurrentSession == null)
            return;

        try
        {
            await CurrentSession
                .AsHost()
                .RemovePlayerAsync(playerId);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionManager] KickPlayer error: {e}");
        }
    }

    // ════════════════════════════════════════════════════════════════
    // START GAME
    // ════════════════════════════════════════════════════════════════

    public void StartGame()
    {
        if (!IsHost)
            return;

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager no encontrado.");
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(
            gameplaySceneName,
            LoadSceneMode.Single
        );
    }

    // ════════════════════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════════════════════

    private async Task WaitForServerReady()
    {
        const int maxWaitMs = 5000;
        const int pollMs = 50;

        int waited = 0;

        while (waited < maxWaitMs)
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsServer &&
                NetworkManager.Singleton.IsListening &&
                NetworkManager.Singleton.SceneManager != null)
            {
                return;
            }

            await Task.Delay(pollMs);

            waited += pollMs;
        }

        throw new TimeoutException("Timeout esperando servidor.");
    }

    private async Task WaitForClientConnected()
    {
        const int maxWaitMs = 10000;
        const int pollMs = 50;

        int waited = 0;

        while (waited < maxWaitMs)
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.IsConnectedClient)
            {
                return;
            }

            await Task.Delay(pollMs);

            waited += pollMs;
        }

        throw new TimeoutException("Timeout esperando cliente.");
    }
}