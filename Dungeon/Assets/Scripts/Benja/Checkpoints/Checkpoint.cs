using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Checkpoint en multiplayer.
///
/// Reglas:
///   - Cuando un jugador interactúa, pide al servidor activarlo.
///   - Si el checkpoint NO estaba descubierto a nivel mundo:
///       → todos los jugadores presentes reciben puntos
///       → se marca como descubierto a nivel mundo
///       → se marca como descubierto personalmente para el activador
///   - Si YA estaba descubierto a nivel mundo:
///       → no se dan puntos
///       → si el activador no lo tenía personalmente, se le marca (pero sin puntos)
///   - Siempre setea LastUsedCheckpoint del activador para respawn.
/// </summary>
public class Checkpoint : MonoBehaviour, IInteractable
{
    [Header("Info")]
    public string checkpointName;       // DEBE SER ÚNICO en todo el mundo

    [Tooltip("Punto donde respawnear/teletransportar al jugador.")]
    public Transform spawnPoint;

    [Header("UI mundo")]
    public GameObject activateUI;
    public GameObject openPanelUI;

    [Header("Recompensa")]
    public int upgradePointsReward = 5;

    [Header("Visual")]
    public CheckpointVisual visual;

    private bool _localPlayerInRange;

    void Start()
    {
        // Esperar a que WorldCheckpointState exista para sincronizar visual
        if (WorldCheckpointState.Instance != null)
        {
            RefreshVisual();
            WorldCheckpointState.Instance.DiscoveredCheckpoints.OnListChanged += _ => RefreshVisual();
        }
        else
        {
            // Si aún no existe, esperamos
            StartCoroutine(WaitForWorldStateAndRefresh());
        }
    }

    private System.Collections.IEnumerator WaitForWorldStateAndRefresh()
    {
        while (WorldCheckpointState.Instance == null) yield return null;
        RefreshVisual();
        WorldCheckpointState.Instance.DiscoveredCheckpoints.OnListChanged += _ => RefreshVisual();
    }

    void OnDestroy()
    {
        // No es estrictamente necesario desuscribir, ya que NetworkList se destruye con el GameObject
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
        if (LocalPlayer.Controller == null)
        {
            Debug.LogWarning("[Checkpoint] No hay LocalPlayer registrado.");
            return;
        }

        // El cliente local pide al servidor activar el checkpoint
        var checkpointData = LocalPlayer.Controller.GetComponent<PlayerCheckpointData>();
        if (checkpointData == null)
        {
            Debug.LogError("[Checkpoint] El player no tiene PlayerCheckpointData.");
            return;
        }

        // Si ya lo descubrió personalmente Y ya está descubierto en el mundo, abre el panel
        bool worldDiscovered = WorldCheckpointState.Instance != null
                            && WorldCheckpointState.Instance.IsDiscoveredInWorld(checkpointName);

        if (worldDiscovered && checkpointData.HasPersonallyDiscovered(checkpointName))
        {
            // Ya conocido por este jugador → abrir panel teletransporte
            // Igualmente actualizamos "último usado"
            RequestActivateServerRpc();
            CheckpointManager.Instance.OpenTeleportPanel();
        }
        else
        {
            // Activar (puede dar puntos o solo desbloquear acceso personal)
            RequestActivateServerRpc();
        }
    }

    /// <summary>
    /// Cliente pide al servidor activar este checkpoint para él.
    /// El servidor valida y aplica la lógica de puntos según las reglas.
    /// </summary>
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

        if (wasNewInWorld)
        {
            // PRIMER descubrimiento mundial: dar puntos a TODOS los jugadores presentes
            WorldCheckpointState.Instance.TryDiscoverInWorld(checkpointName, upgradePointsReward);
            GivePointsToAllPlayers(upgradePointsReward);
            Debug.Log($"[Checkpoint] '{checkpointName}' descubierto mundial por {activatorClientId}. Todos reciben {upgradePointsReward} puntos.");
        }
        else if (!checkpointData.HasPersonallyDiscovered(checkpointName))
        {
            // Ya estaba descubierto a nivel mundo, pero este jugador no lo tenía personalmente.
            // No se dan puntos, solo se desbloquea acceso para este jugador.
            Debug.Log($"[Checkpoint] '{checkpointName}' ya descubierto mundialmente. Player {activatorClientId} desbloquea acceso (sin puntos).");
        }
        // else: ya lo tenía todo. Solo actualizar last used.

        // Marcar descubrimiento personal y "último usado"
        checkpointData.MarkPersonallyDiscovered(checkpointName);
        checkpointData.SetLastUsed(checkpointName);

        // Notificar al CheckpointManager que registre este checkpoint para todos los clientes
        // (ya lo hace el evento de cambio de DiscoveredCheckpoints, pero por seguridad)
    }

    /// <summary>SOLO SERVIDOR. Da puntos a todos los jugadores conectados.</summary>
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
        CheckpointManager.Instance?.CloseTeleportPanel();
    }

    /// <summary>Verifica si el collider que entra es el player local del cliente.</summary>
    private bool IsLocalPlayer(Collider col)
    {
        var pc = col.GetComponentInParent<PlayerController>();
        return pc != null && pc.IsOwner;
    }
}