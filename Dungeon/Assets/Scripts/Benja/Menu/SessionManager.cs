using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton persistente que orquesta el flujo de sesiones.
/// Incluye soporte para múltiples PlayerId en testing (vía profile de Auth).
/// </summary>
public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private string lobbySceneName = "03_Lobby";
    [SerializeField] private string mainMenuSceneName = "02_MainMenu";
    [SerializeField] private string gameplaySceneName = "04_Gameplay";

    [Header("Testing — múltiples cuentas en el mismo PC")]
    [Tooltip("Si está marcado, usa un Auth profile distinto en cada arranque, " +
             "permitiendo simular múltiples PlayerId desde la misma instalación.")]
    [SerializeField] private bool useRandomProfileForTesting = false;

    [Tooltip("Profile fijo opcional. Si está vacío, se genera uno aleatorio. " +
             "Útil para tener una cuenta 'host' y otra 'cliente' fijas.")]
    [SerializeField] private string testProfileName = "";

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
        Debug.Log($"[SessionManager] Awake completado. ID instancia: {GetInstanceID()}");
    }

    public async Task InitializeAsync()
    {
        if (UnityServices.State == ServicesInitializationState.Initialized)
        {
            // Ya inicializado (caso típico al reentrar al menú sin reiniciar)
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            return;
        }

        // CONFIGURAR PROFILE PARA TESTING (debe ser ANTES de InitializeAsync)
        if (useRandomProfileForTesting)
        {
            string profile = string.IsNullOrEmpty(testProfileName)
                ? $"test_{Guid.NewGuid().ToString("N").Substring(0, 8)}"
                : testProfileName;

            var options = new InitializationOptions();
            options.SetProfile(profile);
            await UnityServices.InitializeAsync(options);
            Debug.Log($"[SessionManager] Inicializado con profile: '{profile}'");
        }
        else
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        Debug.Log($"[SessionManager] Auth OK. PlayerId: {AuthenticationService.Instance.PlayerId}");
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

            await WaitForServerReady();
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

    public async Task<bool> JoinSessionById(string sessionId)
    {
        try
        {
            CurrentSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);
            await WaitForClientConnected();
            OnSessionJoined?.Invoke();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionManager] Error al unirse: {e.Message}");
            OnError?.Invoke($"Error al unirse a la sala: {e.Message}");
            return false;
        }
    }

    public async Task<bool> JoinSessionByCode(string code)
    {
        try
        {
            CurrentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
            await WaitForClientConnected();
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

    public async Task LeaveSession()
    {
        if (CurrentSession == null) return;
        try { await CurrentSession.LeaveAsync(); }
        catch (Exception e) { Debug.LogWarning($"[SessionManager] Error al salir: {e.Message}"); }

        CurrentSession = null;
        OnSessionLeft?.Invoke();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public async Task KickPlayer(string playerId)
    {
        if (!IsHost) return;
        try { await CurrentSession.AsHost().RemovePlayerAsync(playerId); }
        catch (Exception e) { Debug.LogError($"[SessionManager] Error al expulsar: {e.Message}"); }
    }

    public void StartGame()
    {
        if (!IsHost) return;
        NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    private async Task WaitForServerReady()
    {
        const int maxWaitMs = 5000, pollMs = 50;
        int waited = 0;
        while (waited < maxWaitMs)
        {
            if (NetworkManager.Singleton != null
                && NetworkManager.Singleton.IsServer
                && NetworkManager.Singleton.IsListening
                && NetworkManager.Singleton.SceneManager != null) return;
            await Task.Delay(pollMs);
            waited += pollMs;
        }
    }

    private async Task WaitForClientConnected()
    {
        const int maxWaitMs = 10000, pollMs = 50;
        int waited = 0;
        while (waited < maxWaitMs)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient) return;
            await Task.Delay(pollMs);
            waited += pollMs;
        }
        throw new TimeoutException("Timeout esperando conexión del cliente.");
    }
}