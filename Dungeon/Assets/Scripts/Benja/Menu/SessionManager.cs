using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton persistente entre escenas que orquesta el flujo de sesiones
/// (crear sala, buscar, unirse por código, salir).
/// </summary>
public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private string lobbySceneName = "02_Lobby";
    [SerializeField] private string mainMenuSceneName = "01_MainMenu";
    [SerializeField] private string gameplaySceneName = "03_Gameplay";

    public string LobbySceneName => lobbySceneName;
    public string MainMenuSceneName => mainMenuSceneName;
    public string GameplaySceneName => gameplaySceneName;

    public ISession CurrentSession { get; private set; }
    public bool IsHost => CurrentSession != null && CurrentSession.IsHost;

    public string PlayerName
    {
        get => PlayerPrefs.GetString("player_name", "Player");
        set { PlayerPrefs.SetString("player_name", value); PlayerPrefs.Save(); }
    }

    public event Action OnSessionJoined;
    public event Action OnSessionLeft;
    public event Action<string> OnError;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Log para verificar persistencia entre escenas
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log($"[SessionManager] Awake completado. ID instancia: {GetInstanceID()}");
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
        {
            Debug.LogWarning($"[SessionManager] DESTRUYENDO la instancia activa (ID: {GetInstanceID()}). " +
                             $"CurrentSession era: {(CurrentSession == null ? "NULL" : CurrentSession.Name)}");
        }
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Debug.Log($"[SessionManager] Escena cargada: {scene.name}. " +
                  $"Instance ID: {GetInstanceID()}. " +
                  $"CurrentSession: {(CurrentSession == null ? "NULL" : CurrentSession.Name)}");
    }

    public async Task InitializeAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        Debug.Log($"[SessionManager] Inicializado. PlayerId: {AuthenticationService.Instance.PlayerId}");
    }

    // ── CREAR SALA ────────────────────────────────────────────────────
    public async Task<bool> CreateSession(string sessionName, bool isPrivate = false)
    {
        try
        {
            var options = new SessionOptions
            {
                MaxPlayers = maxPlayers,
                IsPrivate = isPrivate,
                Name = sessionName,
            }.WithRelayNetwork();

            CurrentSession = await MultiplayerService.Instance.CreateSessionAsync(options);

            Debug.Log($"[SessionManager] Sesión creada: {CurrentSession.Name} (Code: {CurrentSession.Code})");
            OnSessionJoined?.Invoke();

            // CRÍTICO: esperar a que NGO esté completamente listo como host antes
            // de cambiar de escena. Si cambiamos demasiado pronto, los clientes
            // que intenten unirse durante la transición fallan con "Task canceled".
            await WaitForServerReady();

            // Ahora sí, cargar la escena del lobby de espera (sincronizada).
            NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionManager] Error al crear sesión: {e.Message}");
            OnError?.Invoke($"Error al crear sala: {e.Message}");
            return false;
        }
    }

    // ── UNIRSE POR ID ─────────────────────────────────────────────────
    public async Task<bool> JoinSessionById(string sessionId)
    {
        try
        {
            Debug.Log($"[SessionManager] Intentando unirse a {sessionId}...");
            CurrentSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);

            // Esperar a que NGO termine el handshake como cliente
            await WaitForClientConnected();

            Debug.Log($"[SessionManager] Unido a sesión: {CurrentSession.Name}");
            OnSessionJoined?.Invoke();
            // El cambio de escena llega automáticamente porque el host ya está en el lobby.
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionManager] Error al unirse: {e.Message}");
            OnError?.Invoke($"Error al unirse a la sala: {e.Message}");
            return false;
        }
    }

    // ── UNIRSE POR CÓDIGO ────────────────────────────────────────────
    public async Task<bool> JoinSessionByCode(string code)
    {
        try
        {
            CurrentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
            await WaitForClientConnected();
            Debug.Log($"[SessionManager] Unido por código: {CurrentSession.Name}");
            OnSessionJoined?.Invoke();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionManager] Error al unirse por código: {e.Message}");
            OnError?.Invoke($"Código inválido o sala llena: {e.Message}");
            return false;
        }
    }

    // ── BUSCAR SALAS ──────────────────────────────────────────────────
    public async Task<QuerySessionsResults> QueryAvailableSessions()
    {
        try
        {
            return await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions());
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionManager] Error al buscar sesiones: {e.Message}");
            OnError?.Invoke($"Error al buscar salas: {e.Message}");
            return null;
        }
    }

    // ── SALIR DE LA SALA ──────────────────────────────────────────────
    public async Task LeaveSession()
    {
        if (CurrentSession == null) return;

        try
        {
            await CurrentSession.LeaveAsync();
            Debug.Log("[SessionManager] Sesión abandonada.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SessionManager] Error al salir de sesión: {e.Message}");
        }

        CurrentSession = null;
        OnSessionLeft?.Invoke();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ── HOST: ECHAR JUGADOR ──────────────────────────────────────────
    public async Task KickPlayer(string playerId)
    {
        if (!IsHost) { Debug.LogWarning("[SessionManager] Solo el host puede expulsar jugadores."); return; }
        try
        {
            await CurrentSession.AsHost().RemovePlayerAsync(playerId);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionManager] Error al expulsar: {e.Message}");
        }
    }

    // ── HOST: EMPEZAR PARTIDA ─────────────────────────────────────────
    public void StartGame()
    {
        if (!IsHost) { Debug.LogWarning("[SessionManager] Solo el host puede empezar la partida."); return; }
        NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    // ── Helpers de espera ─────────────────────────────────────────────
    private async Task WaitForServerReady()
    {
        const int maxWaitMs = 5000;
        const int pollMs = 50;
        int waited = 0;

        while (waited < maxWaitMs)
        {
            if (NetworkManager.Singleton != null
                && NetworkManager.Singleton.IsServer
                && NetworkManager.Singleton.IsListening
                && NetworkManager.Singleton.SceneManager != null)
            {
                Debug.Log($"[SessionManager] Server listo después de {waited}ms.");
                return;
            }
            await Task.Delay(pollMs);
            waited += pollMs;
        }
        Debug.LogWarning($"[SessionManager] WaitForServerReady timeout.");
    }

    private async Task WaitForClientConnected()
    {
        const int maxWaitMs = 10000;
        const int pollMs = 50;
        int waited = 0;

        while (waited < maxWaitMs)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.Log($"[SessionManager] Cliente conectado después de {waited}ms.");
                return;
            }
            await Task.Delay(pollMs);
            waited += pollMs;
        }
        throw new TimeoutException("Timeout esperando conexión del cliente.");
    }
}