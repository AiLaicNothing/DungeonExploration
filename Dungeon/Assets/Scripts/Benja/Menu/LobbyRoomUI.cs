using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI de la sala de espera (escena 02_Lobby).
/// Muestra lista de jugadores conectados, código de sala, y botones de control.
/// </summary>
public class LobbyRoomUI : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private Button copyCodeButton;

    [Header("Player list")]
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerEntryPrefab;

    [Header("Controles")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private TMP_Text waitingText;

    private readonly List<GameObject> _spawnedEntries = new();

    void Start()
    {
        StartCoroutine(InitializeWhenReady());
    }

    /// <summary>
    /// Espera a que SessionManager tenga una sesión asignada.
    /// Necesario porque al unirse desde un cliente, NGO carga la escena del lobby
    /// ANTES de que JoinSessionByIdAsync retorne y asigne CurrentSession.
    /// </summary>
    private System.Collections.IEnumerator InitializeWhenReady()
    {
        const float timeout = 10f;
        float waited = 0f;

        // Esperar a que exista el SessionManager y tenga sesión
        while (waited < timeout)
        {
            if (SessionManager.Instance != null && SessionManager.Instance.CurrentSession != null)
                break;

            waited += Time.deltaTime;
            yield return null;
        }

        if (SessionManager.Instance == null)
        {
            Debug.LogError("[LobbyRoomUI] SessionManager.Instance es NULL después de esperar.\n" +
                "Estás arrancando Play sin pasar por 00_Boot.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("01_MainMenu");
            yield break;
        }

        if (SessionManager.Instance.CurrentSession == null)
        {
            Debug.LogError($"[LobbyRoomUI] CurrentSession sigue siendo NULL después de {timeout}s. " +
                "Algo salió mal en el flujo de unión.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("01_MainMenu");
            yield break;
        }

        Debug.Log($"[LobbyRoomUI] Sesión OK: {SessionManager.Instance.CurrentSession.Name}, IsHost: {SessionManager.Instance.IsHost} (esperé {waited:F2}s)");
        SetupUI();
    }

    private void SetupUI()
    {
        // Cabecera
        if (roomNameText != null) roomNameText.text = SessionManager.Instance.CurrentSession.Name;
        if (roomCodeText != null) roomCodeText.text = $"Código: {SessionManager.Instance.CurrentSession.Code}";

        if (copyCodeButton != null)
            copyCodeButton.onClick.AddListener(CopyCodeToClipboard);

        // Botones
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
            startGameButton.gameObject.SetActive(SessionManager.Instance.IsHost);
        }

        if (leaveButton != null)
            leaveButton.onClick.AddListener(OnLeaveClicked);

        if (waitingText != null)
            waitingText.gameObject.SetActive(!SessionManager.Instance.IsHost);

        // Suscribirse a eventos de NGO para refrescar lista cuando alguien entra/sale
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        RefreshPlayerList();
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId) => RefreshPlayerList();
    private void OnClientDisconnected(ulong clientId) => RefreshPlayerList();

    /// <summary>Refresca la UI con la lista actual de clientes conectados.</summary>
    private void RefreshPlayerList()
    {
        // Limpiar entradas viejas
        foreach (var e in _spawnedEntries) if (e != null) Destroy(e);
        _spawnedEntries.Clear();

        if (NetworkManager.Singleton == null) return;

        foreach (var clientPair in NetworkManager.Singleton.ConnectedClients)
        {
            var client = clientPair.Value;
            var playerObj = client.PlayerObject;
            if (playerObj == null) continue;

            var sessionData = playerObj.GetComponent<PlayerSessionData>();
            string playerName = sessionData != null ? sessionData.PlayerName.Value.ToString() : $"Player {clientPair.Key}";

            // Si el nombre todavía no se ha sincronizado, lo escuchamos
            if (sessionData != null && string.IsNullOrEmpty(playerName))
            {
                sessionData.PlayerName.OnValueChanged += (_, _) => RefreshPlayerList();
            }

            GameObject entry = Instantiate(playerEntryPrefab, playerListContainer);

            var txt = entry.GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                bool isHost = clientPair.Key == NetworkManager.ServerClientId;
                txt.text = playerName + (isHost ? "  (Host)" : "");
            }

            // Botón de kick: solo visible si soy host Y este no soy yo
            var kickBtn = entry.transform.Find("KickButton")?.GetComponent<Button>();
            if (kickBtn != null)
            {
                bool canKick = SessionManager.Instance.IsHost && clientPair.Key != NetworkManager.ServerClientId;
                kickBtn.gameObject.SetActive(canKick);

                if (canKick)
                {
                    ulong targetId = clientPair.Key;
                    kickBtn.onClick.AddListener(() => OnKickClicked(targetId));
                }
            }

            _spawnedEntries.Add(entry);
        }
    }

    // ── Acciones ──────────────────────────────────────────────────────
    private void OnStartGameClicked()
    {
        SessionManager.Instance.StartGame();
    }

    private async void OnLeaveClicked()
    {
        await SessionManager.Instance.LeaveSession();
    }

    private async void OnKickClicked(ulong clientId)
    {
        // Necesitamos el playerId de UMS, no el clientId de NGO. Lo tomamos del PlayerObject.
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;

        var sessionData = client.PlayerObject?.GetComponent<PlayerSessionData>();
        // En UMS el playerId real se obtiene por otro lado. Una solución simple es
        // desconectar el cliente vía NGO, lo cual también lo saca de la sesión UMS:
        NetworkManager.Singleton.DisconnectClient(clientId);

        // Si quieres usar la API formal de UMS para kick (más limpio):
        // string playerId = ... // requiere mantener un mapping clientId<->playerId
        // await SessionManager.Instance.KickPlayer(playerId);
    }

    private void CopyCodeToClipboard()
    {
        if (SessionManager.Instance.CurrentSession != null)
        {
            GUIUtility.systemCopyBuffer = SessionManager.Instance.CurrentSession.Code;
            Debug.Log("Código copiado al portapapeles.");
        }
    }
}