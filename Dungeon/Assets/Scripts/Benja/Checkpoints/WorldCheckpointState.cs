using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Estado global del mundo para checkpoints.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class WorldCheckpointState : NetworkBehaviour
{
    public static WorldCheckpointState Instance { get; private set; }

    /// <summary>
    /// Checkpoints descubiertos globalmente.
    /// </summary>
    public NetworkList<FixedString64Bytes> DiscoveredCheckpoints;

    /// <summary>
    /// Total de puntos generados globalmente.
    /// </summary>
    public NetworkVariable<int> WorldPointsGenerated =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private void Awake()
    {
        DiscoveredCheckpoints =
            new NetworkList<FixedString64Bytes>();
    }

    public override void OnNetworkSpawn()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[WorldCheckpointState] Ya existe instancia."
            );

            if (IsServer)
                NetworkObject.Despawn();

            return;
        }

        Instance = this;
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this)
            Instance = null;
    }

    // ════════════════════════════════════════════════════════════════
    // CONSULTAS
    // ════════════════════════════════════════════════════════════════

    public bool IsDiscoveredInWorld(string checkpointName)
    {
        var key =
            new FixedString64Bytes(checkpointName);

        for (int i = 0; i < DiscoveredCheckpoints.Count; i++)
        {
            if (DiscoveredCheckpoints[i] == key)
                return true;
        }

        return false;
    }

    // ════════════════════════════════════════════════════════════════
    // DESCUBRIMIENTO
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// SOLO SERVIDOR.
    /// Descubrir checkpoint global.
    /// </summary>
    public bool TryDiscoverInWorld(
        string checkpointName,
        int pointsPerPlayer
    )
    {
        if (!IsServer)
            return false;

        if (IsDiscoveredInWorld(checkpointName))
            return false;

        DiscoveredCheckpoints.Add(
            new FixedString64Bytes(checkpointName)
        );

        WorldPointsGenerated.Value +=
            pointsPerPlayer;

        Debug.Log(
            $"[WorldCheckpointState] " +
            $"Checkpoint mundial descubierto: {checkpointName} | " +
            $"WorldPoints={WorldPointsGenerated.Value}"
        );

        return true;
    }

    // ════════════════════════════════════════════════════════════════
    // RESTORE
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// SOLO SERVIDOR.
    /// Restaura el estado global desde save.
    /// </summary>
    public void RestoreWorldState(
        WorldSaveData data
    )
    {
        if (!IsServer)
            return;

        DiscoveredCheckpoints.Clear();

        foreach (var checkpointName in
                 data.discoveredCheckpoints)
        {
            DiscoveredCheckpoints.Add(
                new FixedString64Bytes(checkpointName)
            );
        }

        WorldPointsGenerated.Value =
            data.globalUpgradePointsGenerated;

        Debug.Log(
            $"[WorldCheckpointState] Mundo restaurado. " +
            $"Checkpoints={DiscoveredCheckpoints.Count} | " +
            $"WorldPoints={WorldPointsGenerated.Value}"
        );
    }
}