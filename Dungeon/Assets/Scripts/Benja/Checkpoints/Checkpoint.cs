using System.Linq;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Checkpoint en multiplayer.
///
/// NUEVO DISEÑO:
///   - El mundo genera puntos SOLO UNA VEZ por checkpoint.
///   - Cada jugador reclama los puntos mundialmente generados que le faltan.
///   - Si un jugador nuevo entra después:
///       recibe TODOS los puntos pendientes automáticamente.
///   - Activar un checkpoint ya descubierto NO da puntos extra.
/// </summary>
public class Checkpoint : NetworkBehaviour, IInteractable
{
    [Header("Info")]
    public string checkpointName;

    [Tooltip("Punto donde respawnear/teletransportar al jugador.")]
    public Transform spawnPoint;

    //[Header("UI mundo")]
    //public GameObject activateUI;
    //public GameObject openPanelUI;

    [Header("Recompensa")]
    public int upgradePointsReward = 5;

    [Header("Visual")]
    public CheckpointVisual visual;

    private bool _localPlayerInRange;

    void Start()
    {
        if (WorldCheckpointState.Instance != null)
        {
            RefreshVisual();

            WorldCheckpointState.Instance
                .DiscoveredCheckpoints
                .OnListChanged += _ => RefreshVisual();
        }
        else
        {
            StartCoroutine(WaitForWorldStateAndRefresh());
        }
    }

    private System.Collections.IEnumerator WaitForWorldStateAndRefresh()
    {
        while (WorldCheckpointState.Instance == null)
            yield return null;

        RefreshVisual();

        WorldCheckpointState.Instance
            .DiscoveredCheckpoints
            .OnListChanged += _ => RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (visual == null)
            return;

        if (WorldCheckpointState.Instance == null)
            return;

        bool discovered =
            WorldCheckpointState.Instance
                .IsDiscoveredInWorld(checkpointName);

        if (discovered)
            visual.ActivateVisual();
        else
            visual.DeactivateVisual();
    }

    // ─────────────────────────────────────────
    // INTERACT
    // ─────────────────────────────────────────

    public void Interact()
    {
        Debug.Log(
            $"[CHECKPOINT CLIENT] " +
            $"LocalClientId={NetworkManager.Singleton.LocalClientId} " +
            $"Checkpoint={checkpointName}"
        );

        if (LocalPlayer.Controller == null)
        {
            Debug.Log(
                "[CHECKPOINT CLIENT] LocalPlayer.Controller NULL"
            );

            return;
        }

        Debug.Log(
            $"[CHECKPOINT CLIENT] " +
            $"ControllerOwner={LocalPlayer.Controller.OwnerClientId} " +
            $"ControllerName={LocalPlayer.Controller.name}"
        );

        var checkpointData =
            LocalPlayer.Controller
                .GetComponent<PlayerCheckpointData>();

        if (checkpointData == null)
        {
            Debug.Log(
                "[CHECKPOINT CLIENT] PlayerCheckpointData NULL"
            );

            return;
        }

        Debug.Log(
            $"[CHECKPOINT CLIENT] " +
            $"Sending RequestActivateServerRpc()"
        );

        RequestActivateServerRpc();

        if (CheckpointMenuUI.Instance != null)
        {
            Debug.Log(
                $"[CHECKPOINT CLIENT] Opening menu for {checkpointName}"
            );

            InteractionUI.Instance.HideUI();
            CheckpointMenuUI.Instance.Open(checkpointName);
        }
    }

    // ─────────────────────────────────────────
    // SERVER
    // ─────────────────────────────────────────

    [ServerRpc(RequireOwnership = false)]
    private void RequestActivateServerRpc(
    ServerRpcParams rpcParams = default)
    {
        ulong activatorClientId =
            rpcParams.Receive.SenderClientId;

        Debug.Log(
            $"[CHECKPOINT RPC RECEIVED] " +
            $"SenderClientId={activatorClientId}"
        );

        if (!NetworkManager.Singleton
                .ConnectedClients
                .TryGetValue(
                    activatorClientId,
                    out var client))
        {
            Debug.LogError(
                $"[CHECKPOINT RPC] Client not found. " +
                $"SenderClientId={activatorClientId}"
            );

            return;
        }

        Debug.Log(
            $"[CHECKPOINT RPC] " +
            $"Client.PlayerObject=" +
            $"{client.PlayerObject?.name}"
        );

        var session =
            client.PlayerObject
                .GetComponent<PlayerSessionData>();

        if (session == null)
        {
            Debug.LogError(
                $"[CHECKPOINT RPC] Session NULL " +
                $"Client={activatorClientId}"
            );

            return;
        }

        Debug.Log(
            $"[CHECKPOINT RPC] " +
            $"Session Owner={session.OwnerClientId} " +
            $"PlayerId={session.PlayerId.Value} " +
            $"PlayerName={session.PlayerName.Value} " +
            $"CurrentCharacterNetId={session.CurrentCharacterNetId.Value}"
        );

        ulong avatarId =
            session.CurrentCharacterNetId.Value;

        if (!NetworkManager.Singleton
                .SpawnManager
                .SpawnedObjects
                .TryGetValue(
                    avatarId,
                    out var playerObj))
        {
            Debug.LogError(
                $"[CHECKPOINT RPC] Avatar not found. " +
                $"AvatarId={avatarId}"
            );

            return;
        }

        Debug.Log(
            $"[CHECKPOINT RPC] Avatar Found " +
            $"Name={playerObj.name} " +
            $"NetId={playerObj.NetworkObjectId} " +
            $"Owner={playerObj.OwnerClientId}"
        );

        if (playerObj == null)
            return;

        var checkpointData =
            playerObj.GetComponent<PlayerCheckpointData>();

        var stats =
            playerObj.GetComponent<PlayerStats>();

        if (checkpointData == null || stats == null)
        {
            Debug.LogError(
                $"[CHECKPOINT RPC] Missing components " +
                $"CheckpointData={(checkpointData != null)} " +
                $"Stats={(stats != null)}"
            );

            return;
        }

        if (WorldCheckpointState.Instance == null)
            return;

        // =====================================================
        // DEBUGS IMPORTANTES
        // =====================================================

        Debug.Log(
            $"[CHECKPOINT SERVER] " +
            $"Client={activatorClientId} " +
            $"Player={session.PlayerName.Value} " +
            $"Checkpoint={checkpointName}"
        );

        Debug.Log(
            $"[CHECKPOINT SERVER] " +
            $"WorldCheckpointCount=" +
            $"{WorldCheckpointState.Instance.DiscoveredCheckpoints.Count}"
        );

        for (int i = 0;
             i < WorldCheckpointState.Instance.DiscoveredCheckpoints.Count;
             i++)
        {
            Debug.Log(
                $"[CHECKPOINT SERVER] CP[{i}]=" +
                $"{WorldCheckpointState.Instance.DiscoveredCheckpoints[i]}"
            );
        }

        bool wasNewInWorld =
            !WorldCheckpointState.Instance
                .IsDiscoveredInWorld(checkpointName);

        bool wasNewForPlayer =
            !checkpointData
                .HasPersonallyDiscovered(checkpointName);

        Debug.Log(
            $"[CHECKPOINT SERVER] " +
            $"wasNewInWorld={wasNewInWorld} " +
            $"wasNewForPlayer={wasNewForPlayer}"
        );

        if (wasNewInWorld)
        {
            WorldCheckpointState.Instance
                .TryDiscoverInWorld(
                    checkpointName,
                    upgradePointsReward
                );

            Debug.Log(
                $"[Checkpoint] Nuevo checkpoint mundial: {checkpointName}"
            );
        }

        int worldGenerated =
            WorldCheckpointState.Instance
                .WorldPointsGenerated.Value;

        int alreadyClaimed =
            stats.WorldPointsClaimed;

        Debug.Log(
            $"[CHECKPOINT SERVER] " +
            $"WorldGenerated={worldGenerated} " +
            $"AlreadyClaimed={alreadyClaimed}"
        );

        int missing =
            worldGenerated - alreadyClaimed;

        Debug.Log(
            $"[CHECKPOINT SERVER] Missing={missing}"
        );

        if (missing > 0)
        {
            stats.AddUpgradePoints(missing);

            stats.SetWorldPointsClaimed(worldGenerated);

            Debug.Log(
                $"[Checkpoint] Player {activatorClientId} reclamó {missing} puntos pendientes."
            );
        }

        if (wasNewForPlayer)
        {
            checkpointData
                .MarkPersonallyDiscovered(checkpointName);
        }

        checkpointData
            .SetLastUsedIfEmpty(checkpointName);

        NotifyDiscoveryClientRpc(
            checkpointName,
            missing,
            wasNewInWorld,
            CreateClientRpcParams(activatorClientId)
        );

        if (SaveGameIntegration.Instance != null)
        {
            SaveGameIntegration.Instance
                .OnCheckpointActivated();
        }
    }

    // ─────────────────────────────────────────
    // CLIENT UI
    // ─────────────────────────────────────────

    [ClientRpc]
    private void NotifyDiscoveryClientRpc(
        string cpName,
        int pointsAwarded,
        bool wasNewInWorld,
        ClientRpcParams rpcParams = default)
    {
        if (ToastNotificationUI.Instance == null)
            return;

        if (wasNewInWorld)
        {
            ToastNotificationUI.Instance.Show(
                "¡Checkpoint descubierto!",
                $"'{cpName}' generó puntos globales. Recibiste +{pointsAwarded}."
            );
        }
        else
        {
            if (pointsAwarded > 0)
            {
                ToastNotificationUI.Instance.Show(
                    "Puntos sincronizados",
                    $"Recibiste +{pointsAwarded} puntos pendientes del progreso mundial."
                );
            }
            else
            {
                ToastNotificationUI.Instance.Show(
                    "Checkpoint registrado",
                    $"'{cpName}' ya estaba descubierto y ya tenías todos los puntos."
                );
            }
        }
    }

    // ─────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────

    private ClientRpcParams CreateClientRpcParams(
        ulong targetClientId)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds =
                    new ulong[] { targetClientId }
            }
        };
    }

    // ─────────────────────────────────────────
    // TRIGGER UI
    // ─────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        //PlayerController player = other.GetComponent<PlayerController>();

        //if (player == null) return;

        //if (!player.IsOwner) return;


        if (!IsLocalPlayer(other))
            return;

        Debug.Log("CheckPoint: Show interaction");
        InteractionUI.Instance.SetUp("Abrir punto de guardado");
        InteractionUI.Instance.ShowUI();

        _localPlayerInRange = true;

        bool worldDiscovered =
            WorldCheckpointState.Instance != null
            && WorldCheckpointState.Instance
                .IsDiscoveredInWorld(checkpointName);

        //if (worldDiscovered)
        //{
        //    openPanelUI?.SetActive(true);
        //}
        //else
        //{
        //    activateUI?.SetActive(true);
        //}

    }

    void OnTriggerExit(Collider other)
    {
        if (!IsLocalPlayer(other))
            return;

        InteractionUI.Instance.HideUI();

        _localPlayerInRange = false;

        //activateUI?.SetActive(false);
        //openPanelUI?.SetActive(false);

        if (CheckpointMenuUI.Instance != null
            && CheckpointMenuUI.Instance.IsOpen)
        {
            CheckpointMenuUI.Instance.Close();
        }

    }

    private bool IsLocalPlayer(Collider col)
    {
        var pc =
            col.GetComponentInParent<PlayerController>();

        return pc != null && pc.IsOwner;
    }
}