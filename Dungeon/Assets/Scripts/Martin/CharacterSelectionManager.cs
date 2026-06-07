using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class CharacterSelectionManager : NetworkBehaviour
{
    public static CharacterSelectionManager Instance;

    [Header("Characters")]
    [SerializeField] private CharacterData[] characters;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    private readonly Dictionary<ulong, int> selectedCharacters = new();
    private readonly Dictionary<ulong, NetworkObject> spawnedCharacters = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(HandleExistingSelections());
    }

    private IEnumerator HandleExistingSelections()
    {
        yield return null;

        if (!NetworkManager.Singleton.IsServer) yield break;

        PlayerSessionData[] sessions = FindObjectsByType<PlayerSessionData>(FindObjectsSortMode.None);

        Debug.Log($"[CharacterSelectionManager] HandleExistingSelections. Sessions={sessions.Length}");

        foreach (var session in sessions)
        {
            int selected = session.SelectedCharacter.Value;

            Debug.Log( $"[CharacterSelectionManager] Session check " + $"Client={session.OwnerClientId} PlayerId={session.PlayerId.Value.ToString()} Selected={selected}" );

            if (selected < 0) continue;

            ulong clientId = session.OwnerClientId;

            if (spawnedCharacters.ContainsKey(clientId)) continue;

            selectedCharacters[clientId] = selected;
            SpawnCharacter(clientId);
        }
    }

    public CharacterData GetCharacter(int index)
    {
        if (index < 0 || index >= characters.Length) return null;

        return characters[index];
    }

    public int CharacterCount => characters.Length;

    public void ServerReceiveSelection(ulong clientId, int characterIndex)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (characterIndex < 0 || characterIndex >= characters.Length) return;

        selectedCharacters[clientId] = characterIndex;

        Debug.Log($"[CharacterSelectionManager] ServerReceiveSelection " +  $"Client={clientId} Character={characterIndex}");

        SpawnCharacter(clientId);
    }

    private void SpawnCharacter(ulong clientId)
    {
        if (!selectedCharacters.TryGetValue(clientId, out int selectedIndex))
        {
            Debug.LogWarning($"[CharacterSelectionManager] No selected character for Client={clientId}");
            return;
        }

        CharacterData data = characters[selectedIndex];

        if (data == null || data.playerPrefab == null)
        {
            Debug.LogWarning("[CharacterSelectionManager] Character prefab missing");
            return;
        }

        PlayerSessionData session = FindSession(clientId);

        PlayerSaveEntry savedEntry = null;
        string playerId = null;

        if (session != null)
        {
            playerId = session.PlayerId.Value.ToString();

            Debug.Log(
                $"[CharacterSelectionManager] Spawn decision " +
                $"Client={clientId} PlayerId={playerId} Selected={selectedIndex}"
            );

            if (SaveSlotManager.Instance != null &&
                SaveSlotManager.Instance.HasActiveSlot &&
                !string.IsNullOrEmpty(playerId))
            {
                SaveSlotManager.Instance.TryGetActivePlayerEntry(
                    playerId,
                    out savedEntry
                );
            }
        }

        Transform spawnPoint = GetFallbackSpawnPoint();

        Vector3 spawnPosition = spawnPoint.position;
        Quaternion spawnRotation = spawnPoint.rotation;

        bool useSavedPosition = false;

        if (savedEntry != null)
        {
            Vector3 savedPos = savedEntry.position.ToVector3();

            // Only restore if this looks like a real gameplay save.
            if (savedPos.sqrMagnitude > 0.001f)
            {
                useSavedPosition = true;
                spawnPosition = savedPos;

                Debug.Log(
                    $"[CharacterSelectionManager] Restoring saved position " +
                    $"Player={playerId} Pos={spawnPosition}"
                );
            }
        }

        if (!useSavedPosition)
        {
            Debug.Log(
                $"[CharacterSelectionManager] Using spawn point " +
                $"Player={playerId} Pos={spawnPosition}"
            );
        }

        if (spawnedCharacters.TryGetValue(clientId, out NetworkObject oldCharacter))
        {
            if (oldCharacter != null && oldCharacter.IsSpawned)
            {
                oldCharacter.Despawn(true);
            }
        }

        GameObject obj = Instantiate(
            data.playerPrefab,
            spawnPosition,
            spawnRotation
        );

        NetworkObject netObj = obj.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogError(
                "[CharacterSelectionManager] Spawned prefab has no NetworkObject"
            );

            Destroy(obj);
            return;
        }

        netObj.SpawnWithOwnership(clientId);

        Debug.Log(
            $"[CharacterSelectionManager] Spawned prefab " +
            $"Character={data.characterName} " +
            $"Client={clientId} " +
            $"NetId={netObj.NetworkObjectId} " +
            $"Pos={spawnPosition}"
        );

        spawnedCharacters[clientId] = netObj;

        if (session != null)
        {
            session.NotifyCharacterSpawned(netObj.NetworkObjectId);

            Debug.Log(
                $"[CharacterSelectionManager] Session notified " +
                $"Client={clientId} CharacterNetId={netObj.NetworkObjectId}"
            );
        }

        if (savedEntry != null &&
            SaveGameIntegration.Instance != null)
        {
            SaveGameIntegration.Instance.OnPlayerSpawned(
                netObj,
                playerId
            );
        }
        else
        {
            PlayerStats stats = netObj.GetComponent<PlayerStats>();

            if (stats != null &&
                WorldCheckpointState.Instance != null)
            {
                stats.SetWorldPointsClaimed(
                    WorldCheckpointState.Instance.WorldPointsGenerated.Value
                );

                Debug.Log(
                    $"[CharacterSelectionManager] " +
                    $"New player synced with world points. " +
                    $"Claimed={WorldCheckpointState.Instance.WorldPointsGenerated.Value}"
                );
            }
        }
    }

    private Transform GetFallbackSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return transform;

        List<Transform> valid = new();
        foreach (var sp in spawnPoints)
        {
            if (sp != null) valid.Add(sp);
        }

        if (valid.Count == 0) return transform;

        return valid[Random.Range(0, valid.Count)];
    }

    private PlayerSessionData FindSession(ulong clientId)
    {
        foreach (var session in FindObjectsByType<PlayerSessionData>(FindObjectsSortMode.None))
        {
            if (session.OwnerClientId == clientId)  return session;
        }

        return null;
    }

    public void RespawnPlayer(ulong clientId)
    {
        PlayerSessionData session = FindSession(clientId);
        if (session == null) return;

        int selected = session.SelectedCharacter.Value;

        if (selected < 0)  return;

        selectedCharacters[clientId] = selected;
        SpawnCharacter(clientId);
    }

}   