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

    [SerializeField]
    private float autosaveIntervalSeconds = 300f;

    private float _timeSinceLastAutosave;

    // Protección contra saves simultáneos
    private bool _isSaving;

    // Protección durante shutdown
    private bool _isShuttingDown;

    // ════════════════════════════════════════════════════════════════
    // UNITY
    // ════════════════════════════════════════════════════════════════

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

    // ════════════════════════════════════════════════════════════════
    // NGO
    // ════════════════════════════════════════════════════════════════

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        Debug.Log("[SaveGameIntegration] OnNetworkSpawn");

        // Restaurar mundo al entrar a Gameplay
        if (SaveSlotManager.Instance != null &&
            SaveSlotManager.Instance.HasActiveSlot)
        {
            RestoreWorldFromActiveSlot();
        }
    }

    // ════════════════════════════════════════════════════════════════
    // UPDATE
    // ════════════════════════════════════════════════════════════════

    private void Update()
    {
        if (!IsServer)
            return;

        if (_isShuttingDown)
            return;

        if (_isSaving)
            return;

        if (!enableAutosave)
            return;

        if (SaveSlotManager.Instance == null)
            return;

        if (!SaveSlotManager.Instance.HasActiveSlot)
            return;

        // Seguridad extra
        if (SceneManager.GetActiveScene().name != "04_Gameplay")
            return;

        _timeSinceLastAutosave += Time.deltaTime;

        if (_timeSinceLastAutosave >= autosaveIntervalSeconds)
        {
            PerformAutosave();
            _timeSinceLastAutosave = 0f;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // RESTORE WORLD
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// SOLO SERVIDOR.
    /// Restaura el estado global del mundo.
    /// </summary>
    private void RestoreWorldFromActiveSlot()
    {
        if (WorldSaveManager.Instance == null)
        {
            Debug.LogError(
                "[SaveGameIntegration] WorldSaveManager no encontrado."
            );

            return;
        }

        if (SaveSlotManager.Instance.ActiveSlot == null)
        {
            Debug.LogWarning(
                "[SaveGameIntegration] ActiveSlot null."
            );

            return;
        }

        var worldData =
            SaveSlotManager.Instance.ActiveSlot.worldData;

        if (worldData == null)
        {
            Debug.LogWarning(
                "[SaveGameIntegration] WorldData null."
            );

            return;
        }

        WorldSaveManager.Instance.RestoreWorldState(worldData);

        Debug.Log(
            "[SaveGameIntegration] Mundo restaurado."
        );
    }

    // ════════════════════════════════════════════════════════════════
    // PLAYER RESTORE
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// SOLO SERVIDOR.
    /// Restaurar estado de jugador cuando spawnea.
    /// </summary>
    public void OnPlayerSpawned(
        NetworkObject playerObject,
        string playerId
    )
    {
        if (!IsServer)
            return;

        if (_isShuttingDown)
            return;

        Debug.Log(
            $"[SaveGameIntegration] OnCharacterSpawned " +
            $"PlayerId={playerId} " +
            $"NetId={playerObject.NetworkObjectId}"
        );

        if (PlayerSaveManager.Instance == null)
        {
            Debug.LogError(
                "[SaveGameIntegration] PlayerSaveManager no encontrado."
            );

            return;
        }

        if (SaveSlotManager.Instance == null ||
            !SaveSlotManager.Instance.HasActiveSlot)
        {
            Debug.Log(
                $"[SaveGameIntegration] " +
                $"No active slot. '{playerId}' starts fresh."
            );

            return;
        }

        var playerData =
            PlayerSaveManager.Instance
                .GetPlayerDataFromActiveSlot(playerId);

        if (playerData == null)
        {
            Debug.Log(
                $"[SaveGameIntegration] " +
                $"No save data found for '{playerId}'"
            );

            return;
        }

        Debug.Log(
            $"[SaveGameIntegration] Restoring player state " +
            $"Player={playerId} " +
            $"SavedPosition={playerData.position.ToVector3()}"
        );

        PlayerSaveManager.Instance
            .RestorePlayerState(
                playerObject,
                playerData
            );

        Debug.Log(
            $"[SaveGameIntegration] Player restored successfully " +
            $"Player={playerId}"
        );
    }

    // ════════════════════════════════════════════════════════════════
    // AUTOSAVE
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// SOLO SERVIDOR.
    /// Guardado completo automático.
    /// </summary>
    public void PerformAutosave()
    {
        if (!IsServer)
            return;

        InternalSave("AUTOSAVE");
    }

    /// <summary>
    /// SOLO SERVIDOR.
    /// Guardado manual desde menú.
    /// </summary>
    public void PerformManualSave()
    {
        if (!IsServer)
        {
            Debug.LogWarning(
                "[SaveGameIntegration] Solo el host puede guardar."
            );

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
        Debug.Log(
    $"[SaveGameIntegration] " +
    $"IsServer={IsServer} | " +
    $"IsHost={NetworkManager.Singleton.IsHost} | " +
    $"IsClient={NetworkManager.Singleton.IsClient}"
);
    }

    // ════════════════════════════════════════════════════════════════
    // CHECKPOINT SAVE
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// SOLO SERVIDOR.
    /// Guardado automático al activar checkpoint.
    /// </summary>
    public void OnCheckpointActivated()
    {
        if (!IsServer)
            return;

        if (_isShuttingDown)
            return;

        Debug.Log(
            "[SaveGameIntegration] Autosave por checkpoint."
        );

        InternalSave("CHECKPOINT SAVE");
    }

    // ════════════════════════════════════════════════════════════════
    // INTERNAL SAVE
    // ════════════════════════════════════════════════════════════════

    private void InternalSave(string reason)
    {
        if (_isSaving)
        {
            Debug.LogWarning(
                "[SaveGameIntegration] Ya hay un save en progreso."
            );

            return;
        }

        if (_isShuttingDown)
        {
            Debug.LogWarning(
                "[SaveGameIntegration] Shutdown activo. Save cancelado."
            );

            return;
        }

        if (SaveSlotManager.Instance == null)
        {
            Debug.LogError(
                "[SaveGameIntegration] SaveSlotManager null."
            );

            return;
        }

        if (!SaveSlotManager.Instance.HasActiveSlot)
        {
            Debug.LogWarning(
                "[SaveGameIntegration] No hay ActiveSlot."
            );

            return;
        }

        if (SceneManager.GetActiveScene().name != "04_Gameplay")
        {
            Debug.LogWarning(
                "[SaveGameIntegration] Save ignorado fuera de Gameplay."
            );

            return;
        }

        _isSaving = true;

        try
        {
            Debug.Log(
                $"[SaveGameIntegration] Ejecutando save ({reason})..."
            );

            // ─────────────────────────────────────────────
            // GUARDAR MUNDO
            // ─────────────────────────────────────────────

            if (WorldSaveManager.Instance != null)
            {
                WorldSaveManager.Instance
                    .CaptureAndUpdateActiveSlot();
            }

            // ─────────────────────────────────────────────
            // GUARDAR JUGADORES
            // ─────────────────────────────────────────────

            if (PlayerSaveManager.Instance != null)
            {
                PlayerSaveManager.Instance
                    .CaptureAllPlayersInActiveSlot();
            }

            // ─────────────────────────────────────────────
            // ESCRIBIR A DISCO
            // ─────────────────────────────────────────────

            SaveSlotManager.Instance.SaveActiveSlot();

            Debug.Log(
                $"[SaveGameIntegration] Save completado ({reason})."
            );
        }
        finally
        {
            _isSaving = false;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // SHUTDOWN
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Bloquea nuevos autosaves durante shutdown.
    /// Llamar antes de salir de sesión o cambiar escena.
    /// </summary>
    public void PrepareForShutdown()
    {
        _isShuttingDown = true;

        Debug.Log(
            "[SaveGameIntegration] Shutdown preparado."
        );
    }
}