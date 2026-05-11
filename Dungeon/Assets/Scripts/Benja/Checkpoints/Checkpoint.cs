using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Checkpoint en multiplayer.
///
/// Comportamiento:
///   - Si NO está descubierto a nivel mundo:
///     * Al activarlo, descubrimiento mundial → da puntos a TODOS los jugadores presentes
///     * Si era tu primer checkpoint personal, se hace tu respawn automáticamente
///   - Si YA está descubierto a nivel mundo:
///     * NO da puntos (ya alguien lo había descubierto)
///     * Se muestra un toast explicando esto al jugador
///     * Si era tu primer checkpoint personal, se hace tu respawn automáticamente
///   - El respawn personal solo se cambia automáticamente la PRIMERA vez.
///     Después, el jugador lo cambia manualmente desde el panel.
///   - Al activar, se abre el menú del checkpoint.
/// </summary>
public class Checkpoint : MonoBehaviour, IInteractable
{
    [Header("Info")]
    public string checkpointName;

    [Tooltip("Punto donde respawnear/teletransportar al jugador.")]
    public Transform spawnPoint;

    [Header("UI mundo")]
    public GameObject activateUI;       // "Pulsa E para activar"
    public GameObject openPanelUI;      // "Pulsa E para usar"

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
            WorldCheckpointState.Instance.DiscoveredCheckpoints.OnListChanged += _ => RefreshVisual();
        }
        else
        {
            StartCoroutine(WaitForWorldStateAndRefresh());
        }
    }

    private System.Collections.IEnumerator WaitForWorldStateAndRefresh()
    {
        while (WorldCheckpointState.Instance == null) yield return null;
        RefreshVisual();
        WorldCheckpointState.Instance.DiscoveredCheckpoints.OnListChanged += _ => RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (visual == null) return;
        if (WorldCheckpointState.Instance == null) return;

        bool discovered = WorldCheckpointState.Instance.IsDiscoveredInWorld(checkpointName);
        if (discovered) visual.ActivateVisual();
        else visual.DeactivateVisual();
    }

    // ── Interacción ───────────────────────────────────────────────────
    public void Interact()
    {
        if (LocalPlayer.Controller == null) return;

        var checkpointData = LocalPlayer.Controller.GetComponent<PlayerCheckpointData>();
        if (checkpointData == null) return;

        // Pedimos al servidor activar el checkpoint
        RequestActivateServerRpc();

        // Abrimos el menú del checkpoint inmediatamente
        if (CheckpointMenuUI.Instance != null)
            CheckpointMenuUI.Instance.Open(checkpointName);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestActivateServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong activatorClientId = rpcParams.Receive.SenderClientId;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(activatorClientId, out var client))
            return;

        var playerObj = client.PlayerObject;
        if (playerObj == null) return;

        var checkpointData = playerObj.GetComponent<PlayerCheckpointData>();
        if (checkpointData == null) return;

        if (WorldCheckpointState.Instance == null) return;

        bool wasNewInWorld = !WorldCheckpointState.Instance.IsDiscoveredInWorld(checkpointName);
        bool wasNewForPlayer = !checkpointData.HasPersonallyDiscovered(checkpointName);

        if (wasNewInWorld)
        {
            // Nuevo en el mundo: descubrir + dar puntos a todos
            WorldCheckpointState.Instance.TryDiscoverInWorld(checkpointName, upgradePointsReward);
            GivePointsToAllPlayers(upgradePointsReward);
            Debug.Log($"[Checkpoint] '{checkpointName}' descubierto mundial por {activatorClientId}. " +
                      $"Todos reciben {upgradePointsReward} puntos.");

            // Avisar al cliente que lo descubrió por primera vez en el mundo
            NotifyDiscoveryClientRpc(checkpointName, upgradePointsReward, true,
                CreateClientRpcParams(activatorClientId));
        }
        else if (wasNewForPlayer)
        {
            // El cliente lo descubre por primera vez personalmente,
            // pero ya estaba descubierto a nivel mundo → no puntos
            Debug.Log($"[Checkpoint] '{checkpointName}' ya descubierto mundial. " +
                      $"Cliente {activatorClientId} desbloquea acceso (sin puntos).");

            NotifyDiscoveryClientRpc(checkpointName, 0, false,
                CreateClientRpcParams(activatorClientId));
        }
        // else: el cliente ya lo conocía. No mostramos toast, es una interacción normal.

        // Marcar descubrimiento personal
        checkpointData.MarkPersonallyDiscovered(checkpointName);

        // Establecer como respawn SOLO si es el primer checkpoint del jugador
        checkpointData.SetLastUsedIfEmpty(checkpointName);
    }

    /// <summary>
    /// Notifica al cliente que activó el checkpoint, para mostrarle un toast informativo.
    /// </summary>
    [ClientRpc]
    private void NotifyDiscoveryClientRpc(string cpName, int pointsAwarded, bool wasNewInWorld,
                                           ClientRpcParams rpcParams = default)
    {
        if (ToastNotificationUI.Instance == null) return;

        if (wasNewInWorld)
        {
            ToastNotificationUI.Instance.Show(
                "¡Checkpoint descubierto!",
                $"Has descubierto '{cpName}'. Todos los jugadores reciben +{pointsAwarded} puntos.");
        }
        else
        {
            ToastNotificationUI.Instance.Show(
                "Checkpoint registrado",
                $"'{cpName}' ya fue descubierto por otro jugador. Tienes acceso pero no recibes puntos.");
        }
    }

    /// <summary>Helper para enviar un ClientRpc solo a un cliente concreto.</summary>
    private ClientRpcParams CreateClientRpcParams(ulong targetClientId)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { targetClientId } }
        };
    }

    private void GivePointsToAllPlayers(int amount)
    {
        foreach (var clientPair in NetworkManager.Singleton.ConnectedClients)
        {
            var po = clientPair.Value.PlayerObject;
            if (po == null) continue;

            var stats = po.GetComponent<PlayerStats>();
            if (stats != null) stats.AddUpgradePoints(amount);
        }
    }

    // ── Trigger UI (mundo) ────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (!IsLocalPlayer(other)) return;
        _localPlayerInRange = true;

        bool worldDiscovered = WorldCheckpointState.Instance != null
                            && WorldCheckpointState.Instance.IsDiscoveredInWorld(checkpointName);

        if (worldDiscovered) openPanelUI?.SetActive(true);
        else activateUI?.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsLocalPlayer(other)) return;
        _localPlayerInRange = false;

        activateUI?.SetActive(false);
        openPanelUI?.SetActive(false);

        if (CheckpointMenuUI.Instance != null && CheckpointMenuUI.Instance.IsOpen)
            CheckpointMenuUI.Instance.Close();
    }

    private bool IsLocalPlayer(Collider col)
    {
        var pc = col.GetComponentInParent<PlayerController>();
        return pc != null && pc.IsOwner;
    }
}