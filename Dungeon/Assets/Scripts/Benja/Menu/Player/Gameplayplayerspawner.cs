using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Spawnea los players manualmente cuando se carga la escena de Gameplay.
/// SOLO se ejecuta en el servidor. Maneja:
///
///   - Despawnear el LobbyPlayer ligero del cliente antes de spawnear el Player completo.
///   - Decidir spawn point: último checkpoint usado o zona inicial.
///   - Alinear los puntos del jugador con los puntos generados a nivel mundo.
///
/// Setup:
///   1. NetworkManager → Default Player Prefab debe ser LobbyPlayerPrefab.
///   2. Player Prefab del juego en Network Prefabs.
///   3. Este componente en escena 04_Gameplay.
///   4. Asignar playerPrefab y los Transforms de spawn.
/// </summary>
public class GameplayPlayerSpawner : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Spawn points")]
    [Tooltip("Spawn point principal de la zona inicial — para jugadores que entran por primera vez.")]
    [SerializeField] private Transform initialZoneSpawn;

    [Tooltip("Spawn points de fallback, se rotan según el clientId si initialZoneSpawn está vacío.")]
    [SerializeField] private Transform[] fallbackSpawnPoints;

    void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[GameplayPlayerSpawner] No hay NetworkManager.");
            return;
        }

        if (!NetworkManager.Singleton.IsServer)
            return;

        // Spawnear a todos los clientes ya conectados al cargar Gameplay
        SpawnAllConnectedPlayers();

        // Suscribirse a futuras conexiones
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void SpawnAllConnectedPlayers()
    {
        foreach (var clientPair in NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = clientPair.Key;
            // Saltamos si ya tiene PlayerObject de gameplay (no LobbyPlayer)
            var existing = clientPair.Value.PlayerObject;
            if (existing != null && existing.GetComponent<PlayerController>() != null) continue;

            StartCoroutine(SpawnPlayerForClientCoroutine(clientId));
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        StartCoroutine(SpawnPlayerForClientCoroutine(clientId));
    }

    private IEnumerator SpawnPlayerForClientCoroutine(ulong clientId)
    {
        // Esperamos un frame para asegurarnos de que el cliente está completamente conectado
        yield return null;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            yield break;

        // Si ya tiene Player de gameplay, no hacemos nada
        if (client.PlayerObject != null && client.PlayerObject.GetComponent<PlayerController>() != null)
            yield break;

        // Despawneamos el LobbyPlayer si lo tiene
        if (client.PlayerObject != null)
        {
            Debug.Log($"[GameplayPlayerSpawner] Despawneando LobbyPlayer del cliente {clientId}");
            client.PlayerObject.Despawn(true);
        }

        // Determinamos el spawn point según los datos del cliente.
        // Necesitamos el LastUsedCheckpoint que se carga vía PlayerCheckpointData,
        // pero ese componente está en el nuevo Player, no en el LobbyPlayer.
        // En vez de eso, leemos el storage local del propio cliente vía RPC,
        // o usamos zona inicial por defecto (la lógica de checkpoint personal se aplicará
        // tras spawnear si el PlayerCheckpointData encuentra un LastUsedCheckpoint guardado).

        Vector3 spawnPos = GetInitialSpawnPosition(clientId);
        Quaternion spawnRot = GetInitialSpawnRotation(clientId);

        GameObject playerInstance = Instantiate(playerPrefab, spawnPos, spawnRot);
        var netObj = playerInstance.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("[GameplayPlayerSpawner] El Player Prefab no tiene NetworkObject.");
            Destroy(playerInstance);
            yield break;
        }

        netObj.SpawnAsPlayerObject(clientId, true);
        Debug.Log($"[GameplayPlayerSpawner] Player spawneado para cliente {clientId} en {spawnPos}");

        // Después de spawnear: esperamos a que PlayerCheckpointData haga upload de su data,
        // luego decidimos si lo movemos a su LastUsedCheckpoint.
        StartCoroutine(MaybeMoveToCheckpointAfterSpawn(playerInstance, clientId));

        // Y alineamos los puntos con el WorldCheckpointState
        StartCoroutine(AlignPointsAfterSpawn(playerInstance));
    }

    /// <summary>
    /// Si el cliente tenía un LastUsedCheckpoint guardado (por sesiones previas),
    /// PlayerCheckpointData hace UploadDataServerRpc al spawnar. Esperamos un poco
    /// y, si llegó, lo teleportamos a ese checkpoint.
    /// </summary>
    private IEnumerator MaybeMoveToCheckpointAfterSpawn(GameObject playerInstance, ulong clientId)
    {
        if (playerInstance == null) yield break;

        var checkpointData = playerInstance.GetComponent<PlayerCheckpointData>();
        if (checkpointData == null) yield break;

        // Esperamos hasta 2 segundos a que el cliente suba sus datos
        const float timeout = 2f;
        float waited = 0f;

        while (waited < timeout)
        {
            string lastUsed = checkpointData.LastUsedCheckpoint.Value.ToString();
            if (!string.IsNullOrEmpty(lastUsed))
            {
                // El cliente tenía checkpoint guardado, movámoslo allí
                if (CheckpointManager.Instance != null)
                {
                    var cp = CheckpointManager.Instance.GetByName(lastUsed);
                    if (cp != null && cp.spawnPoint != null)
                    {
                        playerInstance.transform.position = cp.spawnPoint.position;
                        var rb = playerInstance.GetComponent<Rigidbody>();
                        if (rb != null) rb.linearVelocity = Vector3.zero;
                        Debug.Log($"[GameplayPlayerSpawner] Cliente {clientId} restaurado a checkpoint '{lastUsed}'");
                    }
                }
                yield break;
            }

            waited += Time.deltaTime;
            yield return null;
        }

        // Sin checkpoint previo: ya quedó en zona inicial, no hay que hacer nada
        Debug.Log($"[GameplayPlayerSpawner] Cliente {clientId} es nuevo, queda en zona inicial.");
    }

    /// <summary>
    /// Alinea los upgradePoints del jugador con WorldPointsGenerated.
    /// Si el mundo tiene más puntos generados que los que el jugador ha ganado,
    /// le da la diferencia.
    /// </summary>
    private IEnumerator AlignPointsAfterSpawn(GameObject playerInstance)
    {
        if (playerInstance == null) yield break;

        // Esperamos un frame para que PlayerStats esté listo
        yield return null;

        var stats = playerInstance.GetComponent<PlayerStats>();
        if (stats == null) yield break;

        if (WorldCheckpointState.Instance == null)
        {
            Debug.LogWarning("[GameplayPlayerSpawner] No hay WorldCheckpointState para alinear puntos.");
            yield break;
        }

        int playerEarned = stats.TotalPointsEarned;
        int worldGenerated = WorldCheckpointState.Instance.WorldPointsGenerated.Value;
        int delta = worldGenerated - playerEarned;

        if (delta > 0)
        {
            stats.AddUpgradePoints(delta);
            Debug.Log($"[GameplayPlayerSpawner] Alineando puntos: jugador tenía {playerEarned}, mundo {worldGenerated}, sumando {delta}.");
        }
    }

    private Vector3 GetInitialSpawnPosition(ulong clientId)
    {
        if (initialZoneSpawn != null)
            return initialZoneSpawn.position;

        if (fallbackSpawnPoints != null && fallbackSpawnPoints.Length > 0)
        {
            int index = (int)(clientId % (ulong)fallbackSpawnPoints.Length);
            return fallbackSpawnPoints[index] != null ? fallbackSpawnPoints[index].position : Vector3.zero;
        }

        return Vector3.zero;
    }

    private Quaternion GetInitialSpawnRotation(ulong clientId)
    {
        if (initialZoneSpawn != null)
            return initialZoneSpawn.rotation;

        if (fallbackSpawnPoints != null && fallbackSpawnPoints.Length > 0)
        {
            int index = (int)(clientId % (ulong)fallbackSpawnPoints.Length);
            return fallbackSpawnPoints[index] != null ? fallbackSpawnPoints[index].rotation : Quaternion.identity;
        }

        return Quaternion.identity;
    }
}