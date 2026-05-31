using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Integración entre:
/// - Save System
/// - Multiplayer
/// - Gameplay
/// - SessionManager
///
/// Responsabilidades:
/// - Restaurar mundo al cargar Gameplay
/// - Restaurar jugadores al spawnear
/// - Autosave periódico
/// - Guardado manual
/// - Guardado en checkpoints
/// - Protección contra saves simultáneos
/// - Protección durante shutdown/cambio de escena
///
/// IMPORTANTE:
/// - SOLO el servidor guarda
/// - SOLO el servidor restaura
/// - Debe existir UNA sola instancia en Gameplay
///
/// Setup:
/// - Añadir a un GameObject en 04_Gameplay
/// - Añadir NetworkObject al mismo GameObject
/// </summary>
public class SaveGameIntegration : NetworkBehaviour
{
    public static SaveGameIntegration Instance { get; private set; }

    [Header("Autosave")]
    [SerializeField] private bool enableAutosave = true;

    [SerializeField] private float autosaveIntervalSeconds = 300f;

    private float _timeSinceLastAutosave;
    private bool _isSaving;
    private bool _isShuttingDown;

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

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        Debug.Log("[SaveGameIntegration] OnNetworkSpawn");

        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectServer;

        if (SaveSlotManager.Instance != null &&
            SaveSlotManager.Instance.HasActiveSlot)
        {
            SaveSlotManager.Instance.DebugDumpActiveSlot("SaveGameIntegration.OnNetworkSpawn");
            RestoreWorldFromActiveSlot();
        }
        else
        {
            Debug.LogWarning("[SaveGameIntegration] No active slot when network spawned.");
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectServer;
        }

        base.OnNetworkDespawn();
    }

    private void OnClientDisconnectServer(ulong clientId)
    {
        if (!IsServer || _isShuttingDown)
            return;

        Debug.Log($"[SaveGameIntegration] OnClientDisconnectServer ClientId={clientId}");

        if (PlayerSaveManager.Instance == null)
        {
            Debug.LogWarning("[SaveGameIntegration] PlayerSaveManager missing during disconnect save.");
            return;
        }

        if (SaveSlotManager.Instance == null || !SaveSlotManager.Instance.HasActiveSlot)
        {
            Debug.LogWarning("[SaveGameIntegration] No active slot during disconnect save.");
            return;
        }

        // CHANGE:
        // Final chance to capture this player's state before their avatar or session disappears.
        PlayerSaveManager.Instance.CapturePlayerByClientId(clientId);

        SaveSlotManager.Instance.DebugDumpActiveSlot($"OnClientDisconnectServer clientId={clientId}");
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (_isShuttingDown || _isSaving || !enableAutosave)
            return;

        if (SaveSlotManager.Instance == null || !SaveSlotManager.Instance.HasActiveSlot)
            return;

        if (SceneManager.GetActiveScene().name != "04_Gameplay")
            return;

        _timeSinceLastAutosave += Time.deltaTime;

        if (_timeSinceLastAutosave >= autosaveIntervalSeconds)
        {
            PerformAutosave();
            _timeSinceLastAutosave = 0f;
        }
    }

    private void RestoreWorldFromActiveSlot()
    {
        if (WorldSaveManager.Instance == null)
        {
            Debug.LogError("[SaveGameIntegration] WorldSaveManager not found.");
            return;
        }

        if (SaveSlotManager.Instance.ActiveSlot == null)
        {
            Debug.LogWarning("[SaveGameIntegration] ActiveSlot null.");
            return;
        }

        var worldData = SaveSlotManager.Instance.ActiveSlot.worldData;
        if (worldData == null)
        {
            Debug.LogWarning("[SaveGameIntegration] WorldData null.");
            return;
        }

        WorldSaveManager.Instance.RestoreWorldState(worldData);

        Debug.Log("[SaveGameIntegration] World restored.");
    }

    private void SyncMissingWorldPoints(PlayerStats stats)
    {
        if (!IsServer)
            return;

        if (stats == null)
            return;

        if (WorldCheckpointState.Instance == null)
            return;

        int generated =
            WorldCheckpointState.Instance
                .WorldPointsGenerated.Value;

        int claimed =
            stats.WorldPointsClaimed;

        int missing =
            generated - claimed;

        if (missing <= 0)
            return;

        stats.AddUpgradePoints(missing);

        stats.SetWorldPointsClaimed(generated);

        Debug.Log(
            $"[SaveGameIntegration] Synced missing world points. " +
            $"Missing={missing} " +
            $"Generated={generated} " +
            $"ClaimedBefore={claimed}"
        );
    }

    public void OnPlayerSpawned(NetworkObject playerObject, string playerId)
    {
        if (!IsServer || _isShuttingDown)
            return;

        Debug.Log(
            $"[SaveGameIntegration] OnPlayerSpawned PlayerId={playerId} NetId={playerObject.NetworkObjectId}"
        );

        if (PlayerSaveManager.Instance == null)
        {
            Debug.LogError("[SaveGameIntegration] PlayerSaveManager not found.");
            return;
        }

        if (SaveSlotManager.Instance == null || !SaveSlotManager.Instance.HasActiveSlot)
        {
            Debug.Log(
                $"[SaveGameIntegration] No active slot. '{playerId}' starts fresh."
            );
            return;
        }

        if (SaveSlotManager.Instance.TryGetActivePlayerEntry(playerId, out PlayerSaveEntry playerData))
        {
            Debug.Log(
                $"[SaveGameIntegration] Restoring player state " +
                $"Player={playerId} SavedPosition={playerData.position.ToVector3()} LastPos={playerData.lastKnownPosition.ToVector3()}"
            );

            // Position is already applied by spawn, so we only restore gameplay data.
            PlayerSaveManager.Instance.RestorePlayerState(
                playerObject,
                playerData,
                restorePosition: false
            );
            var stats =
    playerObject.GetComponent<PlayerStats>();

            SyncMissingWorldPoints(stats);
            Debug.Log(
                $"[SaveGameIntegration] Player restored successfully Player={playerId}"
            );
        }
        else
        {
            Debug.LogWarning(
                $"[SaveGameIntegration] No save data found for '{playerId}'. This means the player has no persistent entry yet."
            );

            SaveSlotManager.Instance.DebugDumpActiveSlot($"OnPlayerSpawned miss {playerId}");
        }
    }

    public void PerformAutosave()
    {
        if (!IsServer)
            return;

        InternalSave("AUTOSAVE");
    }

    public void PerformManualSave()
    {
        if (!IsServer)
        {
            Debug.LogWarning("[SaveGameIntegration] Only host can save.");
            return;
        }

        InternalSave("MANUAL SAVE");
        _timeSinceLastAutosave = 0f;

        if (ToastNotificationUI.Instance != null)
        {
            ToastNotificationUI.Instance.Show(
                "Partida guardada",
                "Progreso guardado exitosamente."
            );
        }
    }

    public void OnCheckpointActivated()
    {
        if (!IsServer || _isShuttingDown)
            return;

        Debug.Log("[SaveGameIntegration] Autosave by checkpoint.");
        InternalSave("CHECKPOINT SAVE");
    }

    private void InternalSave(string reason)
    {
        if (_isSaving)
        {
            Debug.LogWarning("[SaveGameIntegration] Save already in progress.");
            return;
        }

        if (_isShuttingDown)
        {
            Debug.LogWarning("[SaveGameIntegration] Shutdown active. Save canceled.");
            return;
        }

        if (SaveSlotManager.Instance == null)
        {
            Debug.LogError("[SaveGameIntegration] SaveSlotManager null.");
            return;
        }

        if (!SaveSlotManager.Instance.HasActiveSlot)
        {
            Debug.LogWarning("[SaveGameIntegration] No ActiveSlot.");
            return;
        }

        if (SceneManager.GetActiveScene().name != "04_Gameplay")
        {
            Debug.LogWarning("[SaveGameIntegration] Save ignored outside Gameplay.");
            return;
        }

        _isSaving = true;

        try
        {
            Debug.Log($"[SaveGameIntegration] Executing save ({reason})...");
            SaveSlotManager.Instance.DebugDumpActiveSlot($"Before {reason}");

            if (WorldSaveManager.Instance != null)
            {
                WorldSaveManager.Instance.CaptureAndUpdateActiveSlot();
            }

            if (PlayerSaveManager.Instance != null)
            {
                PlayerSaveManager.Instance.CaptureAllPlayersInActiveSlot();
            }

            SaveSlotManager.Instance.SaveActiveSlot();

            Debug.Log($"[SaveGameIntegration] Save completed ({reason}).");
            SaveSlotManager.Instance.DebugDumpActiveSlot($"After {reason}");
        }
        finally
        {
            _isSaving = false;
        }
    }

    public void PrepareForShutdown()
    {
        _isShuttingDown = true;

        // CHANGE:
        // Final save before returning to menu or closing the host.
        if (IsServer &&
            SaveSlotManager.Instance != null &&
            SaveSlotManager.Instance.HasActiveSlot)
        {
            Debug.Log("[SaveGameIntegration] PrepareForShutdown -> final save");

            if (PlayerSaveManager.Instance != null)
            {
                PlayerSaveManager.Instance.CaptureAllPlayersInActiveSlot();
            }

            if (WorldSaveManager.Instance != null)
            {
                WorldSaveManager.Instance.CaptureAndUpdateActiveSlot();
            }

            SaveSlotManager.Instance.SaveActiveSlot();
            SaveSlotManager.Instance.DebugDumpActiveSlot("PrepareForShutdown");
        }

        Debug.Log("[SaveGameIntegration] Shutdown prepared.");
    }
}