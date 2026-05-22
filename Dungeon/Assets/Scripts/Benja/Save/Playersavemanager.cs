// ─────────────────────────────────────────────────────────────────────
// PlayerSaveManager.cs
// ─────────────────────────────────────────────────────────────────────

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
        if (Instance == this)
            Instance = null;
    }

    // ══════════════════════════════════════════════════════════════════
    // CAPTURAR ESTADO
    // ══════════════════════════════════════════════════════════════════

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

        var sessionData = playerObject.GetComponent<PlayerSessionData>();
        var stats = playerObject.GetComponent<PlayerStats>();
        var checkpointData = playerObject.GetComponent<PlayerCheckpointData>();

        if (sessionData == null || stats == null)
        {
            Debug.LogError("[PlayerSaveManager] Faltan componentes requeridos.");
            return null;
        }

        var entry = new PlayerSaveEntry
        {
            playerId = sessionData.PlayerId.Value.ToString(),
            playerName = sessionData.PlayerName.Value.ToString(),

            stats = CaptureStats(stats),

            unlockedSkills = new List<string>(),

            // 🔥 ESTA posición es la de reconexión
            position = playerObject.transform.position,

            currentScene =
                UnityEngine.SceneManagement
                    .SceneManager
                    .GetActiveScene()
                    .name,

            // 🔥 SOLO para respawn de muerte
            activeCheckpoint = "",

            personalCheckpoints = new List<string>()
        };

        // ── Capturar checkpoints personales ───────────────────────

        if (checkpointData != null)
        {
            entry.activeCheckpoint =
                checkpointData.LastUsedCheckpoint.Value.ToString();

            foreach (var cpName in checkpointData.PersonallyDiscovered)
            {
                entry.personalCheckpoints.Add(cpName.ToString());
            }
        }

        Debug.Log(
            $"[PlayerSaveManager] Estado capturado para '{entry.playerName}'");

        return entry;
    }

    private PlayerStatsSnapshot CaptureStats(PlayerStats stats)
    {
        return new PlayerStatsSnapshot
        {
            // Valores actuales
            currentHealth = stats.Health.CurrentValue,
            currentMana = stats.Mana.CurrentValue,
            currentStamina = stats.Stamina.CurrentValue,

            // Puntos disponibles
            upgradePoints = stats.UpgradePoints,

            // Puntos invertidos
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
                stats.StaminaRegen.PointsAssigned
        };
    }

    // ══════════════════════════════════════════════════════════════════
    // RESTAURAR ESTADO
    // ══════════════════════════════════════════════════════════════════

    public void RestorePlayerState(
        NetworkObject playerObject,
        PlayerSaveEntry entry)
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

        var stats =
            playerObject.GetComponent<PlayerStats>();

        var checkpointData =
            playerObject.GetComponent<PlayerCheckpointData>();

        // ─────────────────────────────────────────────
        // RESTAURAR STATS
        // ─────────────────────────────────────────────

        if (stats != null)
        {
            stats.SubscribeOrInvokeWhenReady(() =>
            {
                RestoreStats(stats, entry.stats);
            });
        }

        // ─────────────────────────────────────────────
        // RESTAURAR CHECKPOINTS PERSONALES
        // ─────────────────────────────────────────────

        if (checkpointData != null)
        {
            checkpointData.SetLastUsed(
                entry.activeCheckpoint);

            checkpointData.PersonallyDiscovered.Clear();

            foreach (var cpName in entry.personalCheckpoints)
            {
                checkpointData.MarkPersonallyDiscovered(cpName);
            }
        }

        // ─────────────────────────────────────────────
        // RESTAURAR POSICIÓN
        // 🔥 RECONEXIÓN
        // ─────────────────────────────────────────────

        Vector3 pos = entry.position.ToVector3();

        StartCoroutine(
            RestorePositionNextFrame(
                playerObject,
                pos
            )
        );

        Debug.Log(
            $"[PlayerSaveManager] '{entry.playerName}' restaurado en {pos}");
    }

    private IEnumerator RestorePositionNextFrame(
        NetworkObject playerObject,
        Vector3 pos)
    {
        // Esperar 1 frame para que NGO termine
        yield return null;

        if (playerObject == null)
            yield break;

        playerObject.transform.position = pos;

        // Reset velocity
        var rb = playerObject.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log(
            $"[PlayerSaveManager] Posición restaurada en {pos}");
    }

    private void RestoreStats(
        PlayerStats stats,
        PlayerStatsSnapshot snapshot)
    {
        // ── Restaurar puntos invertidos ──────────────────────────

        RestorePoints(stats, "health", snapshot.healthPoints);
        RestorePoints(stats, "mana", snapshot.manaPoints);
        RestorePoints(stats, "stamina", snapshot.staminaPoints);

        RestorePoints(
            stats,
            "physicalDamage",
            snapshot.physicalDamagePoints);

        RestorePoints(
            stats,
            "magicalDamage",
            snapshot.magicalDamagePoints);

        RestorePoints(
            stats,
            "healthRegen",
            snapshot.healthRegenPoints);

        RestorePoints(
            stats,
            "manaRegen",
            snapshot.manaRegenPoints);

        RestorePoints(
            stats,
            "staminaRegen",
            snapshot.staminaRegenPoints);

        // ── Restaurar puntos disponibles ─────────────────────────

        stats.RestoreUpgradePoints(
            snapshot.upgradePoints,
            snapshot.upgradePoints);

        // ── Restaurar valores actuales ───────────────────────────

        stats.SetCurrentValue(
            "health",
            snapshot.currentHealth);

        stats.SetCurrentValue(
            "mana",
            snapshot.currentMana);

        stats.SetCurrentValue(
            "stamina",
            snapshot.currentStamina);
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

    // ══════════════════════════════════════════════════════════════════
    // CONSULTAS
    // ══════════════════════════════════════════════════════════════════

    public bool HasPlayerDataInActiveSlot(string playerId)
    {
        if (!SaveSlotManager.Instance.HasActiveSlot)
            return false;

        return SaveSlotManager.Instance.ActiveSlot.players
            .Any(p => p.playerId == playerId);
    }

    public PlayerSaveEntry GetPlayerDataFromActiveSlot(
        string playerId)
    {
        if (!SaveSlotManager.Instance.HasActiveSlot)
            return null;

        return SaveSlotManager.Instance.ActiveSlot.players
            .FirstOrDefault(p => p.playerId == playerId);
    }

    // ══════════════════════════════════════════════════════════════════
    // ACTUALIZAR SLOT
    // ══════════════════════════════════════════════════════════════════

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

        var slot = SaveSlotManager.Instance.ActiveSlot;

        var existing = slot.players
            .FirstOrDefault(p => p.playerId == entry.playerId);

        if (existing != null)
        {
            slot.players.Remove(existing);
        }

        slot.players.Add(entry);

        Debug.Log(
            $"[PlayerSaveManager] Jugador '{entry.playerName}' actualizado.");
    }

    public void CaptureAllPlayersInActiveSlot()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        if (!SaveSlotManager.Instance.HasActiveSlot)
            return;

        foreach (var clientPair in NetworkManager.Singleton.ConnectedClients)
        {
            var playerObject = clientPair.Value.PlayerObject;

            if (playerObject == null)
                continue;

            CaptureAndUpdatePlayerInActiveSlot(playerObject);
        }

        Debug.Log(
            "[PlayerSaveManager] Todos los jugadores fueron capturados.");
    }
}