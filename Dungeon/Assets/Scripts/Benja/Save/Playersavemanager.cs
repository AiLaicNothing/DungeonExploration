// ─────────────────────────────────────────────────────────────────────
// PlayerSaveManager.cs
// ─────────────────────────────────────────────────────────────────────

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerSaveManager : MonoBehaviour
{
    public static PlayerSaveManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ==================================================
    // SELECTION SAVE
    // ==================================================

    // CHANGE:
    // Save the selection immediately, even if the avatar has not been spawned yet.
    // Why:
    // This prevents player 2 from losing their chosen character if they disconnect
    // before the next autosave or manual save.
    public void CaptureOrUpdateSelectionOnly( string playerId, string playerName, int selectedCharacter)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        if (!SaveSlotManager.Instance.HasActiveSlot)
        {
            Debug.LogWarning($"[PlayerSaveManager] CaptureOrUpdateSelectionOnly skipped. No active slot. PlayerId={playerId}");
            return;
        }

        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogWarning("[PlayerSaveManager] CaptureOrUpdateSelectionOnly skipped. playerId is empty.");
            return;
        }

        var slot = SaveSlotManager.Instance.ActiveSlot;
        var entry = slot.players.FirstOrDefault(p => p.playerId == playerId);

        if (entry == null)
        {
            entry = new PlayerSaveEntry
            {
                playerId = playerId,
                playerName = playerName,
                selectedCharacter = selectedCharacter,
                stats = new PlayerStatsSnapshot(),
                unlockedSkills = new List<string>(),
                position = Vector3.zero,
                lastKnownPosition = Vector3.zero,
                currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                lastKnownScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                activeCheckpoint = "",
                personalCheckpoints = new List<string>(),
                isConnected = true,
                hasSpawnedAvatar = false,
                lastUpdatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            slot.players.Add(entry);
        }
        else
        {
            entry.playerName = playerName;
            entry.selectedCharacter = selectedCharacter;
            entry.currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            entry.lastKnownScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            entry.isConnected = true;
            entry.lastUpdatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        Debug.Log($"[PlayerSaveManager] Selection saved/updated. PlayerId={playerId} " +
            $"PlayerName={playerName} SelectedCharacter={selectedCharacter}");

        SaveSlotManager.Instance.DebugDumpActiveSlot("CaptureOrUpdateSelectionOnly");
        SaveSlotManager.Instance.SaveActiveSlot();
    }

    // ==================================================
    // CAPTURE STATE
    // ==================================================

    public PlayerSaveEntry CapturePlayerState(NetworkObject playerObject)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("[PlayerSaveManager] Solo el servidor puede capturar estado.");
            return null;
        }

        if (playerObject == null)
        {
            Debug.LogError("[PlayerSaveManager] playerObject es null.");
            return null;
        }

        PlayerSessionData sessionData = null;

        foreach (var session in FindObjectsByType<PlayerSessionData>(FindObjectsSortMode.None))
        {
            if (session.CurrentCharacterNetId.Value == playerObject.NetworkObjectId)
            {
                sessionData = session;
                break;
            }
        }

        Debug.Log($"[PlayerSaveManager] Looking for session of character NetId={playerObject.NetworkObjectId}");

        var stats = playerObject.GetComponent<PlayerStats>();
        var checkpointData = playerObject.GetComponent<PlayerCheckpointData>();

        if (sessionData == null || stats == null)
        {
            Debug.LogError($"[PlayerSaveManager] Missing required components. " +
                $"sessionData={(sessionData == null ? "NULL" : "OK")} stats={(stats == null ? "NULL" : "OK")} " +
                $"player={playerObject.name}");

            return null;
        }

        Vector3 currentPos = playerObject.transform.position;
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        var entry = new PlayerSaveEntry
        {
            playerId = sessionData.PlayerId.Value.ToString(),
            playerName = sessionData.PlayerName.Value.ToString(),
            selectedCharacter = sessionData.SelectedCharacter.Value,
            stats = CaptureStats(stats),
            unlockedSkills = new List<string>(),
            position = currentPos,
            lastKnownPosition = currentPos,
            currentScene = currentScene,
            lastKnownScene = currentScene,
            lastUpdatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            activeCheckpoint = "",
            personalCheckpoints = new List<string>(),
            isConnected = true,
            hasSpawnedAvatar = true
        };

        Debug.Log($"[PlayerSaveManager] Capturing player state " +
            $"Player={entry.playerName} Id={entry.playerId} Char={entry.selectedCharacter} Pos={entry.position.ToVector3()}");

        if (checkpointData != null)
        {
            entry.activeCheckpoint = checkpointData.LastUsedCheckpoint.Value.ToString();

            foreach (var cpName in checkpointData.PersonallyDiscovered)
            {
                entry.personalCheckpoints.Add(cpName.ToString());
            }
        }

        Debug.Log($"[PlayerSaveManager] State captured for '{entry.playerName}'");
        return entry;
    }

    private PlayerStatsSnapshot CaptureStats(PlayerStats stats)
    {
        return new PlayerStatsSnapshot
        {
            currentHealth = stats.Health.CurrentValue,
            currentMana = stats.Mana.CurrentValue,
            currentStamina = stats.Stamina.CurrentValue,
            upgradePoints = stats.UpgradePoints,
            totalPointsEarned = stats.TotalPointsEarned,
            healthPoints = stats.Health.PointsAssigned,
            manaPoints = stats.Mana.PointsAssigned,
            staminaPoints = stats.Stamina.PointsAssigned,
            physicalDamagePoints = stats.PhysicalDamage.PointsAssigned,
            magicalDamagePoints = stats.MagicalDamage.PointsAssigned,
            healthRegenPoints = stats.HealthRegen.PointsAssigned,
            manaRegenPoints = stats.ManaRegen.PointsAssigned,
            staminaRegenPoints = stats.StaminaRegen.PointsAssigned
        };
    }

    // ==================================================
    // RESTORE STATE
    // ==================================================

    public void RestorePlayerState(NetworkObject playerObject, PlayerSaveEntry entry, bool restorePosition = false)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("[PlayerSaveManager] Solo el servidor puede restaurar.");
            return;
        }

        if (playerObject == null || entry == null)
        {
            Debug.LogError("[PlayerSaveManager] playerObject o entry es null.");
            return;
        }

        var stats = playerObject.GetComponent<PlayerStats>();
        var checkpointData = playerObject.GetComponent<PlayerCheckpointData>();

        if (stats != null)
        {
            stats.SubscribeOrInvokeWhenReady(() => {RestoreStats(stats, entry.stats);});
        }

        if (checkpointData != null)
        {
            checkpointData.SetLastUsed(entry.activeCheckpoint);

            checkpointData.PersonallyDiscovered.Clear();

            foreach (var cpName in entry.personalCheckpoints)
            {
                checkpointData.MarkPersonallyDiscovered(cpName);
            }
        }

        if (restorePosition)
        {
            Vector3 pos = entry.position.ToVector3();
            StartCoroutine(RestorePositionNextFrame(playerObject, pos));
        }

        Debug.Log(
            $"[PlayerSaveManager] '{entry.playerName}' restored. restorePosition={restorePosition}"
        );
    }

    private IEnumerator RestorePositionNextFrame(NetworkObject playerObject, Vector3 pos)
    {
        yield return null;
        yield return null;
        yield return null;
        yield return new WaitForEndOfFrame();

        if (playerObject == null) yield break;

        Transform t = playerObject.transform;

        var controller = playerObject.GetComponent<CharacterController>();
        var rb = playerObject.GetComponent<Rigidbody>();
        var networkTransform = playerObject.GetComponent<Unity.Netcode.Components.NetworkTransform>();

        if (controller != null) controller.enabled = false;

        if (networkTransform != null) networkTransform.enabled = false;

        t.position = pos;

        Physics.SyncTransforms();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = pos;
        }

        yield return new WaitForEndOfFrame();

        if (networkTransform != null)  networkTransform.enabled = true;

        if (controller != null) controller.enabled = true;

        Debug.Log($"[PlayerSaveManager] Position restored FINAL at {t.position}");
    }

    private void RestoreStats(PlayerStats stats, PlayerStatsSnapshot snapshot)
    {
        if (stats == null || snapshot == null)
            return;

        stats.ResetAllStatsForLoad();

        RestorePoints(stats, "health", snapshot.healthPoints);
        RestorePoints(stats, "mana", snapshot.manaPoints);
        RestorePoints(stats, "stamina", snapshot.staminaPoints);
        RestorePoints(stats, "physicalDamage", snapshot.physicalDamagePoints);
        RestorePoints(stats, "magicalDamage", snapshot.magicalDamagePoints);
        RestorePoints(stats, "healthRegen", snapshot.healthRegenPoints);
        RestorePoints(stats, "manaRegen", snapshot.manaRegenPoints);
        RestorePoints(stats, "staminaRegen", snapshot.staminaRegenPoints);

        stats.RestoreUpgradePoints(snapshot.upgradePoints, snapshot.totalPointsEarned);

        stats.SetCurrentValue("health", snapshot.currentHealth);
        stats.SetCurrentValue("mana", snapshot.currentMana);
        stats.SetCurrentValue("stamina", snapshot.currentStamina);

        Debug.Log("[PlayerSaveManager] Stats restored.");
    }

    private void RestorePoints(PlayerStats stats, string statId, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            stats.RestorePointToStat(statId);
        }
    }

    // ==================================================
    // QUERIES
    // ==================================================

    public bool HasPlayerDataInActiveSlot(string playerId)
    {
        if (!SaveSlotManager.Instance.HasActiveSlot)  return false;

        return SaveSlotManager.Instance.ActiveSlot.players.Any(p => p.playerId == playerId);
    }

    public PlayerSaveEntry GetPlayerDataFromActiveSlot(string playerId)
    {
        if (!SaveSlotManager.Instance.HasActiveSlot)  return null;

        return SaveSlotManager.Instance.ActiveSlot.players.FirstOrDefault(p => p.playerId == playerId);
    }

    // ==================================================
    // UPDATE SLOT
    // ==================================================

    // CHANGE:
    // This updates the in-memory slot without removing offline players.
    public void CaptureAndUpdatePlayerInActiveSlot(NetworkObject playerObject)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (!SaveSlotManager.Instance.HasActiveSlot) return;

        var entry = CapturePlayerState(playerObject);

        if (entry == null)  return;

        SaveSlotManager.Instance.UpsertPlayerEntry(entry, saveToDisk: false);

        Debug.Log($"[PlayerSaveManager] Player '{entry.playerName}' updated. " +
            $"PlayerId={entry.playerId} Pos={entry.position.ToVector3()}");
    }

    // CHANGE:
    // Fallback for disconnect/shutdown when only the client id is known.
    public void CapturePlayerByClientId(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer)  return;

        var session = FindObjectsByType<PlayerSessionData>(FindObjectsSortMode.None).FirstOrDefault(s => s.OwnerClientId == clientId);

        if (session == null)
        {
            Debug.LogWarning($"[PlayerSaveManager] CapturePlayerByClientId failed. Session not found. ClientId={clientId}");
            return;
        }

        string playerId = session.PlayerId.Value.ToString();
        string playerName = session.PlayerName.Value.ToString();

        if (session.CurrentCharacterNetId.Value != 0 &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(session.CurrentCharacterNetId.Value, out NetworkObject character))
        {
            Debug.Log($"[PlayerSaveManager] Capturing full player state by clientId. ClientId={clientId} PlayerId={playerId}");

            CaptureAndUpdatePlayerInActiveSlot(character);
            SaveSlotManager.Instance.SaveActiveSlot();
            return;
        }

        Debug.LogWarning($"[PlayerSaveManager] Character missing for ClientId={clientId}. Saving selection only. PlayerId={playerId}");

        CaptureOrUpdateSelectionOnly(playerId, playerName, session.SelectedCharacter.Value);
    }

    // CHANGE:
    // This keeps saved offline players inside the slot.
    public void CaptureAllPlayersInActiveSlot()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        if (!SaveSlotManager.Instance.HasActiveSlot)
            return;

        PlayerSessionData[] sessions =
            FindObjectsByType<PlayerSessionData>(FindObjectsSortMode.None);

        Debug.Log($"[PlayerSaveManager] CaptureAllPlayersInActiveSlot. Sessions={sessions.Length}");

        foreach (var session in sessions)
        {
            ulong netId = session.CurrentCharacterNetId.Value;

            Debug.Log( $"[PlayerSaveManager] Processing session " +
                $"Client={session.OwnerClientId} PlayerId={session.PlayerId.Value.ToString()} CharacterNetId={netId}");

            if (netId == 0)
            {
                Debug.LogWarning($"[PlayerSaveManager] No character assigned Client={session.OwnerClientId}");
                continue;
            }

            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out NetworkObject character))
            {
                Debug.LogWarning($"[PlayerSaveManager] Character object missing NetId={netId}");
                continue;
            }

            CaptureAndUpdatePlayerInActiveSlot(character);
        }

        SaveSlotManager.Instance.DebugDumpActiveSlot("CaptureAllPlayersInActiveSlot");
        Debug.Log("[PlayerSaveManager] All players captured successfully.");
    }

    // ==================================================
    // LATEST SNAPSHOT HELPERS
    // ==================================================

    public void UpdateLiveSnapshotFromCharacter(NetworkObject playerObject)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (!SaveSlotManager.Instance.HasActiveSlot) return;

        var entry = CapturePlayerState(playerObject);

        if (entry == null) return;

        SaveSlotManager.Instance.UpsertPlayerEntry(entry, saveToDisk: false);

        Debug.Log($"[PlayerSaveManager] Live snapshot updated. PlayerId={entry.playerId} Pos={entry.position.ToVector3()}");
    }
}