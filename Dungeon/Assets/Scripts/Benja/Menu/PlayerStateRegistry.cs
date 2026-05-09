using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Componente que vive solo en el servidor. Mantiene un registro de los jugadores
/// que se han desconectado, indexado por PlayerId (no por clientId).
///
/// Cuando un jugador se desconecta, guarda un snapshot de su estado.
/// Cuando un jugador entra, comprueba si su PlayerId tiene snapshot → si sí, lo restaura.
///
/// Setup: añadir como componente al GameObject del NetworkManager (en 00_Boot).
/// El propio script verifica IsServer antes de hacer cualquier cosa.
/// </summary>
public class PlayerStateRegistry : MonoBehaviour
{
    public static PlayerStateRegistry Instance { get; private set; }

    /// <summary>Snapshot del estado de un jugador que se desconectó.</summary>
    public class PlayerSnapshot
    {
        public string playerId;
        public string playerName;
        public Vector3 position;
        public Quaternion rotation;
        public float currentHealth;
        public float currentStamina;
        public float currentMana;
        public int upgradePoints;
        public int totalPointsEarned;
        public Dictionary<string, int> pointsAssignedByStat = new();
        public float maxHealth;
        public float maxStamina;
        public float maxMana;
        // Añadir más campos según necesites (inventario, posición de checkpoint, etc.)
    }

    private readonly Dictionary<string, PlayerSnapshot> _snapshots = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnect;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnect;
        }
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        // Buscar el PlayerObject del cliente que se va para guardar su estado
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;
        var playerObj = client?.PlayerObject;
        if (playerObj == null) return;

        var sessionData = playerObj.GetComponent<PlayerSessionData>();
        var stats = playerObj.GetComponent<PlayerStats>();

        if (sessionData == null || stats == null) return;

        string playerId = sessionData.PlayerId.Value.ToString();
        if (string.IsNullOrEmpty(playerId)) return;

        var snapshot = new PlayerSnapshot
        {
            playerId = playerId,
            playerName = sessionData.PlayerName.Value.ToString(),
            position = playerObj.transform.position,
            rotation = playerObj.transform.rotation,
            currentHealth = stats.GetCurrentValue("health"),
            currentStamina = stats.GetCurrentValue("stamina"),
            currentMana = stats.GetCurrentValue("mana"),
            upgradePoints = stats.UpgradePoints,
            totalPointsEarned = stats.TotalPointsEarned,
            maxHealth = stats.GetMaxValue("health"),
            maxStamina = stats.GetMaxValue("stamina"),
            maxMana = stats.GetMaxValue("mana"),
        };

        // Guardar puntos asignados por stat
        foreach (var stat in stats.AllStats)
            snapshot.pointsAssignedByStat[stat.Id] = stats.GetPointsAssigned(stat.Id);

        _snapshots[playerId] = snapshot;
        Debug.Log($"[PlayerStateRegistry] Snapshot guardado para {snapshot.playerName} (PlayerId: {playerId})");
    }

    private void OnClientConnect(ulong clientId)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        if (clientId == NetworkManager.ServerClientId) return;

        // Como el PlayerSessionData necesita un frame para sincronizar el PlayerId,
        // posponemos el chequeo a unos frames después.
        StartCoroutine(TryRestoreAfterDelay(clientId));
    }

    private System.Collections.IEnumerator TryRestoreAfterDelay(ulong clientId)
    {
        // Esperamos a que el PlayerSessionData haya sincronizado el PlayerId
        const float timeout = 3f;
        float waited = 0f;

        while (waited < timeout)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                var playerObj = client?.PlayerObject;
                var sessionData = playerObj?.GetComponent<PlayerSessionData>();
                var stats = playerObj?.GetComponent<PlayerStats>();

                if (sessionData != null && stats != null && !string.IsNullOrEmpty(sessionData.PlayerId.Value.ToString()))
                {
                    string playerId = sessionData.PlayerId.Value.ToString();

                    if (_snapshots.TryGetValue(playerId, out var snapshot))
                    {
                        Debug.Log($"[PlayerStateRegistry] Restaurando snapshot para {playerId}");
                        RestoreSnapshot(playerObj, stats, snapshot);
                        _snapshots.Remove(playerId); // ya consumido
                    }
                    else
                    {
                        Debug.Log($"[PlayerStateRegistry] PlayerId {playerId} es nuevo, sin snapshot.");
                    }
                    yield break;
                }
            }
            waited += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning($"[PlayerStateRegistry] Timeout esperando PlayerId del cliente {clientId}");
    }

    private void RestoreSnapshot(NetworkObject playerObj, PlayerStats stats, PlayerSnapshot snap)
    {
        // Restaurar posición
        playerObj.transform.position = snap.position;
        playerObj.transform.rotation = snap.rotation;

        // Restaurar puntos asignados (esto recalcula los Max automáticamente vía AddPoint_Internal)
        // OJO: solo restauramos el conteo de puntos, no el valor crudo de Max.
        // Como AddPoint_Internal está privado en PlayerStats, lo hacemos por la vía pública
        // que es ApplyTradeoff o directamente seteando vía SetCurrentValue después.
        // Aquí asumimos que las stats arrancan con baseValue al spawn, así que tenemos que
        // "subir N puntos" para llegar al estado guardado:
        foreach (var kv in snap.pointsAssignedByStat)
        {
            int targetPoints = kv.Value;
            for (int i = 0; i < targetPoints; i++)
            {
                // Necesitamos llamar a algo que suba un punto sin coste.
                // Lo hacemos con AddUpgradePoints (gratis) + ApplyTradeoff de 1 stat.
                // Para simplificar: añadimos un método público RestorePointToStat en PlayerStats
                // (ver siguiente cambio en PlayerStats.cs).
                stats.RestorePointToStat(kv.Key);
            }
        }

        // Restaurar valores actuales
        stats.SetCurrentValue("health", snap.currentHealth);
        stats.SetCurrentValue("stamina", snap.currentStamina);
        stats.SetCurrentValue("mana", snap.currentMana);

        // Restaurar pool de puntos
        stats.RestoreUpgradePoints(snap.upgradePoints, snap.totalPointsEarned);

        Debug.Log($"[PlayerStateRegistry] Snapshot restaurado para {snap.playerName}");
    }

    /// <summary>Limpia todos los snapshots (al cambiar de partida, etc.).</summary>
    public void ClearAll() => _snapshots.Clear();
}