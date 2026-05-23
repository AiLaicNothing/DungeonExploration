using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Spawnea los players manualmente cuando se carga la escena de Gameplay.
/// SOLO se ejecuta en el servidor.
/// </summary>
public class GameplayPlayerSpawner : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Spawn points")]
    [Tooltip("Spawn principal para jugadores nuevos.")]
    [SerializeField] private Transform initialZoneSpawn;

    [Tooltip("Fallback spawns.")]
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

        // Validar SaveSlot activo
        if (SaveSlotManager.Instance == null ||
            !SaveSlotManager.Instance.HasActiveSlot)
        {
            Debug.LogError(
                "[GameplayPlayerSpawner] No hay SaveSlot activo."
            );

            return;
        }

        // Spawn inicial
        SpawnAllConnectedPlayers();

        // Nuevos clientes
        NetworkManager.Singleton.OnClientConnectedCallback +=
            OnClientConnected;
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -=
                OnClientConnected;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // SPAWN
    // ════════════════════════════════════════════════════════════════

    private void SpawnAllConnectedPlayers()
    {
        foreach (var clientPair in
                 NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = clientPair.Key;

            var existing = clientPair.Value.PlayerObject;

            // Ya tiene Player gameplay
            if (existing != null &&
                existing.GetComponent<PlayerController>() != null)
            {
                continue;
            }

            StartCoroutine(
                SpawnPlayerForClientCoroutine(clientId)
            );
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        StartCoroutine(
            SpawnPlayerForClientCoroutine(clientId)
        );
    }

    private IEnumerator SpawnPlayerForClientCoroutine(
        ulong clientId
    )
    {
        // Esperar 1 frame para que NGO termine conexión
        yield return null;

        if (!NetworkManager.Singleton.ConnectedClients
                .TryGetValue(clientId, out var client))
        {
            yield break;
        }

        // Ya tiene gameplay player
        if (client.PlayerObject != null &&
            client.PlayerObject.GetComponent<PlayerController>() != null)
        {
            yield break;
        }

        // Despawn LobbyPlayer
        if (client.PlayerObject != null)
        {
            Debug.Log(
                $"[GameplayPlayerSpawner] " +
                $"Despawneando LobbyPlayer del cliente {clientId}"
            );

            client.PlayerObject.Despawn(true);
        }

        // Spawn temporal inicial
        // Si el jugador tiene save,
        // SaveGameIntegration lo moverá luego.
        Vector3 spawnPos =
            GetInitialSpawnPosition(clientId);

        Quaternion spawnRot =
            GetInitialSpawnRotation(clientId);

        // ─────────────────────────────────────────────
        // SI EL JUGADOR TIENE SAVE,
        // SPAWNEAR DIRECTAMENTE EN SU POSICIÓN
        // ─────────────────────────────────────────────

        var sessionManager =
            SessionManager.Instance;

        if (sessionManager != null &&
            PlayerSaveManager.Instance != null)
        {
            string playerId =
                sessionManager.GetPlayerId();

            var saveData =
                PlayerSaveManager.Instance
                    .GetPlayerDataFromActiveSlot(playerId);

            if (saveData != null)
            {
                spawnPos =
                    saveData.position.ToVector3();

                Debug.Log(
                    $"[GameplayPlayerSpawner] " +
                    $"Usando posición guardada para {playerId}: {spawnPos}"
                );
            }
        }

        GameObject playerInstance =
            Instantiate(
                playerPrefab,
                spawnPos,
                spawnRot
            );

        var netObj =
            playerInstance.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogError(
                "[GameplayPlayerSpawner] " +
                "El prefab no tiene NetworkObject."
            );

            Destroy(playerInstance);

            yield break;
        }
        netObj.SpawnAsPlayerObject(clientId, true);

        Debug.Log(
            $"[GameplayPlayerSpawner] " +
            $"Player spawneado para cliente {clientId}"
        );

        // 🔥 RESTAURAR SAVE DEL JUGADOR
        var sessionData =
            playerInstance.GetComponent<PlayerSessionData>();

        if (sessionData != null &&
            SaveGameIntegration.Instance != null)
        {
            SaveGameIntegration.Instance.OnPlayerSpawned(
                netObj,
                sessionData.PlayerId.Value.ToString()
            );
            StartCoroutine(DebugPlayerPosition(playerInstance));
        }


        // 🔥 IMPORTANTE:
        // NO mover al checkpoint aquí.
        //
        // La restauración de posición ahora ocurre en:
        // SaveGameIntegration ->
        // PlayerSaveManager.RestorePlayerState()
        //
        // Eso hará:
        // - Jugador nuevo -> spawn inicial
        // - Jugador existente -> última posición guardada

        //// Sincronizar puntos mundiales
        //StartCoroutine(
        //    AlignPointsAfterSpawn(playerInstance)
        //);
    }
    private IEnumerator DebugPlayerPosition(GameObject player)
    {
        for (int i = 0; i < 10; i++)
        {
            Debug.Log(
                $"[DEBUG POSITION] Frame {i} -> {player.transform.position}"
            );

            yield return null;
        }
    }

    //// ════════════════════════════════════════════════════════════════
    //// ALIGN WORLD POINTS
    //// ════════════════════════════════════════════════════════════════

    //private IEnumerator AlignPointsAfterSpawn(
    //    GameObject playerInstance
    //)
    //{
    //    if (playerInstance == null)
    //        yield break;

    //    yield return null;

    //    var stats =
    //        playerInstance.GetComponent<PlayerStats>();

    //    if (stats == null)
    //        yield break;

    //    if (WorldCheckpointState.Instance == null)
    //    {
    //        Debug.LogWarning(
    //            "[GameplayPlayerSpawner] " +
    //            "No hay WorldCheckpointState."
    //        );

    //        yield break;
    //    }

    //    int playerEarned =
    //        stats.TotalPointsEarned;

    //    int worldGenerated =
    //        WorldCheckpointState.Instance
    //            .WorldPointsGenerated.Value;

    //    int delta =
    //        worldGenerated - playerEarned;

    //    if (delta > 0)
    //    {
    //        stats.AddUpgradePoints(delta);

    //        Debug.Log(
    //            $"[GameplayPlayerSpawner] " +
    //            $"Sumando {delta} puntos al jugador."
    //        );
    //    }
    //}

    // ════════════════════════════════════════════════════════════════
    // SPAWN HELPERS
    // ════════════════════════════════════════════════════════════════

    private Vector3 GetInitialSpawnPosition(
        ulong clientId
    )
    {
        if (initialZoneSpawn != null)
        {
            return initialZoneSpawn.position;
        }

        if (fallbackSpawnPoints != null &&
            fallbackSpawnPoints.Length > 0)
        {
            int index =
                (int)(clientId %
                (ulong)fallbackSpawnPoints.Length);

            if (fallbackSpawnPoints[index] != null)
            {
                return fallbackSpawnPoints[index].position;
            }
        }

        return Vector3.zero;
    }

    private Quaternion GetInitialSpawnRotation(
        ulong clientId
    )
    {
        if (initialZoneSpawn != null)
        {
            return initialZoneSpawn.rotation;
        }

        if (fallbackSpawnPoints != null &&
            fallbackSpawnPoints.Length > 0)
        {
            int index =
                (int)(clientId %
                (ulong)fallbackSpawnPoints.Length);

            if (fallbackSpawnPoints[index] != null)
            {
                return fallbackSpawnPoints[index].rotation;
            }
        }

        return Quaternion.identity;
    }
}