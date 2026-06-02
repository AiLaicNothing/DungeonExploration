
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

    public void CaptureOrUpdateSelectionOnly(
        string playerId,
        string playerName,
        int selectedCharacter)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        if (!SaveSlotManager.Instance.HasActiveSlot)
        {
            Debug.LogWarning(
                $"[PlayerSaveManager] CaptureOrUpdateSelectionOnly skipped. No active slot. PlayerId={playerId}"
            );

            return;
        }

        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogWarning(
                "[PlayerSaveManager] CaptureOrUpdateSelectionOnly skipped. playerId is empty."
            );

            return;
        }

        var slot = SaveSlotManager.Instance.ActiveSlot;

        var entry =
            slot.players.FirstOrDefault(p => p.playerId == playerId);

        if (entry == null)
        {
            CharacterData characterData = null;

            if (CharacterSelectionManager.Instance != null)
            {
                characterData =
                    CharacterSelectionManager.Instance.GetCharacter(
                        selectedCharacter
                    );
            }
            entry = new PlayerSaveEntry
            {
                playerId = playerId,
                playerName = playerName,
                selectedCharacter = selectedCharacter,

                stats = new PlayerStatsSnapshot
                {
                    currentHealth =
        characterData != null
            ? characterData.startingHealth
            : 100,

                    currentMana =
        characterData != null
            ? characterData.startingMana
            : 50,

                    currentStamina =
        characterData != null
            ? characterData.startingStamina
            : 100,

                    upgradePoints = 0,
                    totalPointsEarned = 0,

                    healthPoints = 0,
                    manaPoints = 0,
                    staminaPoints = 0,

                    physicalDamagePoints = 0,
                    magicalDamagePoints = 0,

                    healthRegenPoints = 0,
                    manaRegenPoints = 0,
                    staminaRegenPoints = 0,

                    worldPointsClaimed = 0
                },
                unlockedSkills = new List<string>(),

                position = Vector3.zero,
                lastKnownPosition = Vector3.zero,

                currentScene =
                    UnityEngine.SceneManagement.SceneManager
                        .GetActiveScene().name,

                lastKnownScene =
                    UnityEngine.SceneManagement.SceneManager
                        .GetActiveScene().name,

                activeCheckpoint = "",
                personalCheckpoints = new List<string>(),

                isConnected = true,
                hasSpawnedAvatar = false,

                worldPointsClaimed = 0,

                lastUpdatedTimestamp =
    DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            slot.players.Add(entry);
        }
        else
        {
            entry.playerName = playerName;
            entry.selectedCharacter = selectedCharacter;

            entry.currentScene =
                UnityEngine.SceneManagement.SceneManager
                    .GetActiveScene().name;

            entry.lastKnownScene =
                UnityEngine.SceneManagement.SceneManager
                    .GetActiveScene().name;

            entry.isConnected = true;

            entry.lastUpdatedTimestamp =
                DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        Debug.Log(
            $"[PlayerSaveManager] Selection saved/updated. " +
            $"PlayerId={playerId} " +
            $"PlayerName={playerName} " +
            $"SelectedCharacter={selectedCharacter}"
        );

        SaveSlotManager.Instance.DebugDumpActiveSlot(
            "CaptureOrUpdateSelectionOnly"
        );

        SaveSlotManager.Instance.SaveActiveSlot();
    }

    // ==================================================
    // CAPTURE STATE
    // ==================================================

    public PlayerSaveEntry CapturePlayerState(NetworkObject playerObject)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogError(
                "[PlayerSaveManager] Solo el servidor puede capturar estado."
            );

            return null;
        }

        if (playerObject == null)
        {
            Debug.LogError(
                "[PlayerSaveManager] playerObject es null."
            );

            return null;
        }

        PlayerSessionData sessionData = null;

        foreach (var session in
                 FindObjectsByType<PlayerSessionData>(
                     FindObjectsSortMode.None))
        {
            if (session.CurrentCharacterNetId.Value ==
                playerObject.NetworkObjectId)
            {
                sessionData = session;
                break;
            }
        }

        Debug.Log(
            $"[PlayerSaveManager] Looking for session of character NetId={playerObject.NetworkObjectId}"
        );

        var stats =
            playerObject.GetComponent<PlayerStats>();

        var checkpointData =
            playerObject.GetComponent<PlayerCheckpointData>();

        var skillInventory =
            playerObject.GetComponent<PlayerSkillInventory>();

        if (sessionData == null || stats == null)
        {
            Debug.LogError(
                $"[PlayerSaveManager] Missing required components. " +
                $"sessionData={(sessionData == null ? "NULL" : "OK")} " +
                $"stats={(stats == null ? "NULL" : "OK")} " +
                $"player={playerObject.name}"
            );

            return null;
        }

        Vector3 currentPos =
            playerObject.transform.position;

        string currentScene =
            UnityEngine.SceneManagement.SceneManager
                .GetActiveScene().name;

        List<string> unlockedSkills = new();
        string[] equippedSkillIds = null;
        if (skillInventory != null)
        {
            unlockedSkills =
                skillInventory.CaptureUnlockedSkills();

            equippedSkillIds =
                skillInventory.CaptureEquippedSkillIds();
        }

        var entry = new PlayerSaveEntry
        {
            equippedSkillIds = equippedSkillIds,

            playerId =
                sessionData.PlayerId.Value.ToString(),

            playerName =
                sessionData.PlayerName.Value.ToString(),

            selectedCharacter =
                sessionData.SelectedCharacter.Value,

            stats = CaptureStats(stats),

            unlockedSkills = unlockedSkills,

            position = currentPos,
            lastKnownPosition = currentPos,

            currentScene = currentScene,
            lastKnownScene = currentScene,

            lastUpdatedTimestamp =
                DateTimeOffset.UtcNow.ToUnixTimeSeconds(),

            activeCheckpoint = "",
            personalCheckpoints = new List<string>(),

            isConnected = true,
            hasSpawnedAvatar = true,

            worldPointsClaimed =
                stats != null
                    ? stats.WorldPointsClaimed
                    : 0
        };

        Debug.Log(
            $"[SAVE ENTRY] " +
            $"Player={entry.playerName} " +
            $"HP={entry.stats.currentHealth} " +
            $"MP={entry.stats.currentMana} " +
            $"STA={entry.stats.currentStamina} " +
            $"UpgradePoints={entry.stats.upgradePoints} " +
            $"WorldPointsClaimed={entry.stats.worldPointsClaimed}"
        );

        Debug.Log(
            $"[PlayerSaveManager] Capturing player state " +
            $"Player={entry.playerName} " +
            $"Id={entry.playerId} " +
            $"Char={entry.selectedCharacter} " +
            $"Pos={entry.position.ToVector3()}"
        );

        if (checkpointData != null)
        {
            entry.activeCheckpoint =
                checkpointData.LastUsedCheckpoint.Value.ToString();

            foreach (var cpName in
                     checkpointData.PersonallyDiscovered)
            {
                entry.personalCheckpoints.Add(
                    cpName.ToString()
                );
            }
        }

        Debug.Log(
            $"[PlayerSaveManager] State captured for '{entry.playerName}'"
        );

        return entry;
    }

    private PlayerStatsSnapshot CaptureStats(PlayerStats stats)
    {
        Debug.Log(
            $"[CAPTURE STATS] " +
            $"HP={stats.Health.CurrentValue} " +
            $"MP={stats.Mana.CurrentValue} " +
            $"STA={stats.Stamina.CurrentValue} " +
            $"MaxHP={stats.Health.Max} " +
            $"UpgradePoints={stats.UpgradePoints} " +
            $"WorldPointsClaimed={stats.WorldPointsClaimed}"
        );

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

            physicalDamagePoints =
                stats.PhysicalDamage.PointsAssigned,

            magicalDamagePoints =
                stats.MagicalDamage.PointsAssigned,

            healthRegenPoints =
                stats.HealthRegen.PointsAssigned,

            manaRegenPoints =
                stats.ManaRegen.PointsAssigned,

            staminaRegenPoints =
                stats.StaminaRegen.PointsAssigned,

            worldPointsClaimed =
                stats.WorldPointsClaimed,
        };
    }

    // ==================================================
    // RESTORE STATE
    // ==================================================

    public void RestorePlayerState(
        NetworkObject playerObject,
        PlayerSaveEntry entry,
        bool restorePosition = false)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogError(
                "[PlayerSaveManager] Solo el servidor puede restaurar."
            );

            return;
        }

        if (playerObject == null || entry == null)
        {
            Debug.LogError(
                "[PlayerSaveManager] playerObject o entry es null."
            );

            return;
        }

        var stats =
            playerObject.GetComponent<PlayerStats>();

        var checkpointData =
            playerObject.GetComponent<PlayerCheckpointData>();

        var skillInventory =
            playerObject.GetComponent<PlayerSkillInventory>();

        // ─────────────────────────────────────────
        // RESTORE STATS
        // ─────────────────────────────────────────
        if (stats != null)
        {
            Debug.Log(
                $"[LOAD ENTRY] " +
                $"Player={entry.playerName} " +
                $"HP={entry.stats.currentHealth} " +
                $"MP={entry.stats.currentMana} " +
                $"STA={entry.stats.currentStamina} " +
                $"UpgradePoints={entry.stats.upgradePoints} " +
                $"WorldPointsClaimed={entry.stats.worldPointsClaimed}"
            );

            Action restoreCallback = null;

            restoreCallback = () =>
            {
                stats.OnStatsReady -= restoreCallback;

                Debug.Log(
                    $"[RESTORE CALLBACK] " +
                    $"Player={entry.playerName} " +
                    $"HP={entry.stats.currentHealth} " +
                    $"MP={entry.stats.currentMana} " +
                    $"STA={entry.stats.currentStamina}"
                );

                RestoreStats(stats, entry.stats);

                ApplyMissingWorldPoints(
                    entry,
                    stats
                );
            };

            stats.SubscribeOrInvokeWhenReady(restoreCallback);
        }

        // ─────────────────────────────────────────
        // RESTORE CHECKPOINTS
        // ─────────────────────────────────────────

        if (checkpointData != null)
        {
            checkpointData.SetLastUsed(entry.activeCheckpoint);

            checkpointData.PersonallyDiscovered.Clear();

            foreach (var cpName in entry.personalCheckpoints)
            {
                checkpointData.MarkPersonallyDiscovered(cpName);
            }
        }

        // ─────────────────────────────────────────
        // RESTORE SKILLS
        // ─────────────────────────────────────────

        if (skillInventory != null)
        {
            skillInventory.RestoreUnlockedSkills(
                entry.unlockedSkills
            );

            skillInventory.RestoreEquippedSkills(
                entry.equippedSkillIds
            );
        }

        // ─────────────────────────────────────────
        // RESTORE POSITION
        // ─────────────────────────────────────────

        if (restorePosition)
        {
            Vector3 pos = entry.position.ToVector3();

            StartCoroutine(
                RestorePositionNextFrame(playerObject, pos)
            );
        }

        Debug.Log(
            $"[PlayerSaveManager] '{entry.playerName}' restored. " +
            $"restorePosition={restorePosition}"
        );
    }

    private IEnumerator RestorePositionNextFrame(
        NetworkObject playerObject,
        Vector3 pos)
    {
        yield return null;
        yield return null;
        yield return null;
        yield return new WaitForEndOfFrame();

        if (playerObject == null)
            yield break;

        Transform t = playerObject.transform;

        var controller =
            playerObject.GetComponent<CharacterController>();

        var rb =
            playerObject.GetComponent<Rigidbody>();

        var networkTransform =
            playerObject.GetComponent<
                Unity.Netcode.Components.NetworkTransform>();

        if (controller != null)
            controller.enabled = false;

        if (networkTransform != null)
            networkTransform.enabled = false;

        t.position = pos;

        Physics.SyncTransforms();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = pos;
        }

        yield return new WaitForEndOfFrame();

        if (networkTransform != null)
            networkTransform.enabled = true;

        if (controller != null)
            controller.enabled = true;

        Debug.Log(
            $"[PlayerSaveManager] Position restored FINAL at {t.position}"
        );
    }

    private void RestoreStats(
        PlayerStats stats,
        PlayerStatsSnapshot snapshot)
    {
        if (stats == null || snapshot == null)
            return;
        bool emptySnapshot =
    snapshot.currentHealth <= 0 &&
    snapshot.currentMana <= 0 &&
    snapshot.currentStamina <= 0 &&
    snapshot.totalPointsEarned <= 0;

        if (emptySnapshot)
        {
            Debug.LogWarning(
                "[PlayerSaveManager] Empty snapshot detected. Using defaults."
            );

            snapshot.currentHealth =
                stats.GetMaxValue("health");

            snapshot.currentMana =
                stats.GetMaxValue("mana");

            snapshot.currentStamina =
                stats.GetMaxValue("stamina");

            Debug.Log(
                $"[RESTORE FALLBACK] " +
                $"HP={snapshot.currentHealth} " +
                $"MP={snapshot.currentMana} " +
                $"STA={snapshot.currentStamina}"
            );
        }
        Debug.Log(
            $"[RESTORE STATS] Snapshot " +
            $"HP={snapshot.currentHealth} " +
            $"MP={snapshot.currentMana} " +
            $"STA={snapshot.currentStamina} " +
            $"UpgradePoints={snapshot.upgradePoints} " +
            $"WorldPointsClaimed={snapshot.worldPointsClaimed}"
        );

        Debug.Log(
            $"[RESTORE STATS] Before Reset " +
            $"HP={stats.GetCurrentValue("health")} " +
            $"MaxHP={stats.GetMaxValue("health")} " +
            $"Ready={stats.IsStatsReady}"
        );

        stats.ResetAllStatsForLoad();

        Debug.Log(
            $"[RESTORE STATS] After Reset " +
            $"HP={stats.GetCurrentValue("health")} " +
            $"MaxHP={stats.GetMaxValue("health")}"
        );

        RestorePoints(stats, "health", snapshot.healthPoints);
        RestorePoints(stats, "mana", snapshot.manaPoints);
        RestorePoints(stats, "stamina", snapshot.staminaPoints);

        RestorePoints(
            stats,
            "physicalDamage",
            snapshot.physicalDamagePoints
        );

        RestorePoints(
            stats,
            "magicalDamage",
            snapshot.magicalDamagePoints
        );

        RestorePoints(
            stats,
            "healthRegen",
            snapshot.healthRegenPoints
        );

        RestorePoints(
            stats,
            "manaRegen",
            snapshot.manaRegenPoints
        );

        RestorePoints(
            stats,
            "staminaRegen",
            snapshot.staminaRegenPoints
        );

        Debug.Log(
            $"[RESTORE STATS] After Points Restore " +
            $"HP={stats.GetCurrentValue("health")} " +
            $"MaxHP={stats.GetMaxValue("health")}"
        );

        stats.RestoreUpgradePoints(
            snapshot.upgradePoints,
            snapshot.totalPointsEarned
        );

        stats.SetWorldPointsClaimed(
            snapshot.worldPointsClaimed
        );

        stats.SetCurrentValue(
            "health",
            snapshot.currentHealth
        );

        stats.SetCurrentValue(
            "mana",
            snapshot.currentMana
        );

        stats.SetCurrentValue(
            "stamina",
            snapshot.currentStamina
        );

        Debug.Log(
            $"[RESTORE STATS] Final Values " +
            $"HP={stats.GetCurrentValue("health")} " +
            $"MP={stats.GetCurrentValue("mana")} " +
            $"STA={stats.GetCurrentValue("stamina")} " +
            $"MaxHP={stats.GetMaxValue("health")} " +
            $"UpgradePoints={stats.UpgradePoints}"
        );

        Debug.Log("[PlayerSaveManager] Stats restored.");
    }

    private void RestorePoints(
        PlayerStats stats,
        string statId,
        int amount)
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
        if (!SaveSlotManager.Instance.HasActiveSlot)
            return false;

        return SaveSlotManager.Instance
            .ActiveSlot
            .players
            .Any(p => p.playerId == playerId);
    }

    public PlayerSaveEntry GetPlayerDataFromActiveSlot(
        string playerId)
    {
        if (!SaveSlotManager.Instance.HasActiveSlot)
            return null;

        var entry = SaveSlotManager.Instance
            .ActiveSlot
            .players
            .FirstOrDefault(p => p.playerId == playerId);

        if (entry != null)
        {
            Debug.Log(
                $"[GET ENTRY] " +
                $"Player={entry.playerName} " +
                $"HP={entry.stats.currentHealth} " +
                $"MP={entry.stats.currentMana} " +
                $"STA={entry.stats.currentStamina} " +
                $"UpgradePoints={entry.stats.upgradePoints} " +
                $"WorldPointsClaimed={entry.stats.worldPointsClaimed}"
            );
        }

        return entry;
    }
    // ==================================================
    // UPDATE SLOT
    // ==================================================

    public void CaptureAndUpdatePlayerInActiveSlot(
        NetworkObject playerObject)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        if (!SaveSlotManager.Instance.HasActiveSlot)
            return;

        var entry = CapturePlayerState(playerObject);

        if (entry == null)
            return;

        SaveSlotManager.Instance.UpsertPlayerEntry(
            entry,
            saveToDisk: false
        );

        Debug.Log(
            $"[PlayerSaveManager] Player '{entry.playerName}' updated. " +
            $"PlayerId={entry.playerId} " +
            $"Pos={entry.position.ToVector3()}"
        );
    }

    public void CapturePlayerByClientId(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        var session =
            FindObjectsByType<PlayerSessionData>(
                FindObjectsSortMode.None)
            .FirstOrDefault(s => s.OwnerClientId == clientId);

        if (session == null)
        {
            Debug.LogWarning(
                $"[PlayerSaveManager] CapturePlayerByClientId failed. " +
                $"Session not found. ClientId={clientId}"
            );

            return;
        }

        string playerId =
            session.PlayerId.Value.ToString();

        string playerName =
            session.PlayerName.Value.ToString();

        if (session.CurrentCharacterNetId.Value != 0 &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects
                .TryGetValue(
                    session.CurrentCharacterNetId.Value,
                    out NetworkObject character))
        {
            Debug.Log(
                $"[PlayerSaveManager] Capturing full player state by clientId. " +
                $"ClientId={clientId} PlayerId={playerId}"
            );

            CaptureAndUpdatePlayerInActiveSlot(character);

            SaveSlotManager.Instance.SaveActiveSlot();

            return;
        }

        Debug.LogWarning(
            $"[PlayerSaveManager] Character missing for ClientId={clientId}. " +
            $"Saving selection only. PlayerId={playerId}"
        );

        CaptureOrUpdateSelectionOnly(
            playerId,
            playerName,
            session.SelectedCharacter.Value
        );
    }

    public void CaptureAllPlayersInActiveSlot()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        if (!SaveSlotManager.Instance.HasActiveSlot)
            return;

        PlayerSessionData[] sessions =
            FindObjectsByType<PlayerSessionData>(
                FindObjectsSortMode.None);

        Debug.Log(
            $"[PlayerSaveManager] CaptureAllPlayersInActiveSlot. Sessions={sessions.Length}"
        );

        foreach (var session in sessions)
        {
            ulong netId =
                session.CurrentCharacterNetId.Value;

            Debug.Log(
                $"[PlayerSaveManager] Processing session " +
                $"Client={session.OwnerClientId} " +
                $"PlayerId={session.PlayerId.Value.ToString()} " +
                $"CharacterNetId={netId}"
            );

            if (netId == 0)
            {
                Debug.LogWarning(
                    $"[PlayerSaveManager] No character assigned Client={session.OwnerClientId}"
                );

                continue;
            }

            if (!NetworkManager.Singleton.SpawnManager
                    .SpawnedObjects
                    .TryGetValue(
                        netId,
                        out NetworkObject character))
            {
                Debug.LogWarning(
                    $"[PlayerSaveManager] Character object missing NetId={netId}"
                );

                continue;
            }

            CaptureAndUpdatePlayerInActiveSlot(character);
        }   

        SaveSlotManager.Instance.DebugDumpActiveSlot(
            "CaptureAllPlayersInActiveSlot" 
        );

        Debug.Log(
            "[PlayerSaveManager] All players captured successfully."
        );
    }

    // ==================================================
    // LATEST SNAPSHOT HELPERS
    // ==================================================

    public void UpdateLiveSnapshotFromCharacter(
        NetworkObject playerObject)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        if (!SaveSlotManager.Instance.HasActiveSlot)
            return;

        var entry = CapturePlayerState(playerObject);

        if (entry == null)
            return;

        SaveSlotManager.Instance.UpsertPlayerEntry(
            entry,
            saveToDisk: false
        );

        Debug.Log(
            $"[PlayerSaveManager] Live snapshot updated. " +
            $"PlayerId={entry.playerId} " +
            $"Pos={entry.position.ToVector3()}"
        );
    }
    private void ApplyMissingWorldPoints(
    PlayerSaveEntry entry,
    PlayerStats stats
)
    {
        if (entry == null)
            return;

        if (stats == null)
            return;

        if (WorldCheckpointState.Instance == null)
            return;

        int totalWorldPoints =
            WorldCheckpointState.Instance
                .WorldPointsGenerated.Value;

        int alreadyClaimed =
            entry.worldPointsClaimed;

        int missing =
            totalWorldPoints - alreadyClaimed;

        if (missing <= 0)
            return;

        Debug.Log(
            $"[PlayerSaveManager] " +
            $"Applying missing world points. " +
            $"Missing={missing}"
        );

        stats.AddUpgradePoints(missing);

        stats.SetWorldPointsClaimed(
            totalWorldPoints
        );

        entry.worldPointsClaimed =
            totalWorldPoints;
        
    }
}

