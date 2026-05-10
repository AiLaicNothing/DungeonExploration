using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Spawnea los players manualmente cuando se carga la escena de Gameplay.
/// SOLO se ejecuta en el servidor.
///
/// Para que funcione:
///   1. En NetworkManager, dejar VACÍO el campo "Default Player Prefab".
///   2. Añadir el Player Prefab a "Network Prefabs" igualmente.
///   3. Poner este componente en un GameObject de la escena 04_Gameplay.
///   4. Asignar el Player Prefab al campo playerPrefab en el inspector.
///
/// Resultado: los Players aparecen sólo cuando los clientes están realmente en
/// la escena de Gameplay, no en Lobby.
/// </summary>
public class GameplayPlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;

    [Tooltip("Spawn points donde aparecerán los jugadores. Se rotan según el clientId.")]
    [SerializeField] private Transform[] defaultSpawnPoints;

    void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[GameplayPlayerSpawner] No hay NetworkManager.");
            return;
        }

        if (!NetworkManager.Singleton.IsServer)
        {
            // Solo el servidor spawnea. Los clientes solo reciben los Players.
            return;
        }

        SpawnAllConnectedPlayers();

        // Suscribirse a futuras conexiones (jugadores que se unan después)
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    /// <summary>Spawnea Players para todos los clientes ya conectados.</summary>
    private void SpawnAllConnectedPlayers()
    {
        foreach (var clientPair in NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = clientPair.Key;

            // Si ya tiene PlayerObject (por ejemplo, por rejoin), no spawneamos otro
            if (clientPair.Value.PlayerObject != null) continue;

            SpawnPlayerForClient(clientId);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        // Esperamos un frame para asegurar que el cliente esté listo
        StartCoroutine(SpawnAfterFrame(clientId));
    }

    private System.Collections.IEnumerator SpawnAfterFrame(ulong clientId)
    {
        yield return null;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) yield break;
        if (client.PlayerObject != null) yield break; // ya tiene Player
        SpawnPlayerForClient(clientId);
    }

    /// <summary>Spawnea el Player de un cliente concreto en un spawn point.</summary>
    private void SpawnPlayerForClient(ulong clientId)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[GameplayPlayerSpawner] playerPrefab no está asignado.");
            return;
        }

        // Si el cliente tiene un PlayerObject ligero (LobbyPlayer), lo despawneamos primero.
        // SpawnAsPlayerObject reemplazaría al anterior automáticamente, pero es más limpio destruirlo.
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var existingClient))
        {
            var existing = existingClient.PlayerObject;
            if (existing != null)
            {
                Debug.Log($"[GameplayPlayerSpawner] Despawneando LobbyPlayer previo del cliente {clientId}");
                existing.Despawn(true);
            }
        }

        Vector3 spawnPos = GetSpawnPosition(clientId);
        Quaternion spawnRot = GetSpawnRotation(clientId);

        GameObject playerInstance = Instantiate(playerPrefab, spawnPos, spawnRot);
        var netObj = playerInstance.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("[GameplayPlayerSpawner] El Player Prefab no tiene NetworkObject.");
            Destroy(playerInstance);
            return;
        }

        netObj.SpawnAsPlayerObject(clientId, true);
        Debug.Log($"[GameplayPlayerSpawner] Player spawneado para cliente {clientId} en {spawnPos}");
    }

    private Vector3 GetSpawnPosition(ulong clientId)
    {
        if (defaultSpawnPoints == null || defaultSpawnPoints.Length == 0)
            return Vector3.zero;

        int index = (int)(clientId % (ulong)defaultSpawnPoints.Length);
        return defaultSpawnPoints[index] != null ? defaultSpawnPoints[index].position : Vector3.zero;
    }

    private Quaternion GetSpawnRotation(ulong clientId)
    {
        if (defaultSpawnPoints == null || defaultSpawnPoints.Length == 0)
            return Quaternion.identity;

        int index = (int)(clientId % (ulong)defaultSpawnPoints.Length);
        return defaultSpawnPoints[index] != null ? defaultSpawnPoints[index].rotation : Quaternion.identity;
    }
}