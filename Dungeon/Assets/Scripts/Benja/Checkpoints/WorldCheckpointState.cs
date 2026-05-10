using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Estado global del mundo para checkpoints. Vive en el servidor (NetworkBehaviour persistente).
/// Mantiene la lista de checkpoints descubiertos a nivel mundo y los puntos totales generados.
///
/// Setup: añadir como componente a un GameObject "WorldState" en la escena de Gameplay,
/// con NetworkObject. Se debe spawnear en el servidor (network spawn al cargar gameplay).
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class WorldCheckpointState : NetworkBehaviour
{
    public static WorldCheckpointState Instance { get; private set; }

    /// <summary>
    /// Lista de checkpointName que han sido descubiertos por al menos un jugador.
    /// Sincronizada a todos los clientes.
    /// </summary>
    public NetworkList<FixedString64Bytes> DiscoveredCheckpoints;

    /// <summary>
    /// Total de puntos generados a nivel mundo. Cuando un jugador nuevo se une,
    /// recibe esta cantidad para igualar a los demás.
    /// </summary>
    public NetworkVariable<int> WorldPointsGenerated = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        DiscoveredCheckpoints = new NetworkList<FixedString64Bytes>();
    }

    public override void OnNetworkSpawn()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[WorldCheckpointState] Ya hay una instancia. Destruyendo duplicado.");
            if (IsServer) NetworkObject.Despawn();
            return;
        }
        Instance = this;
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>True si el checkpoint ya fue descubierto a nivel mundo por alguien.</summary>
    public bool IsDiscoveredInWorld(string checkpointName)
    {
        var key = new FixedString64Bytes(checkpointName);
        for (int i = 0; i < DiscoveredCheckpoints.Count; i++)
            if (DiscoveredCheckpoints[i] == key) return true;
        return false;
    }

    /// <summary>
    /// SOLO SERVIDOR. Marca un checkpoint como descubierto a nivel mundo.
    /// Retorna true si era nuevo (primera vez), false si ya estaba.
    /// </summary>
    public bool TryDiscoverInWorld(string checkpointName, int pointsPerPlayer)
    {
        if (!IsServer) return false;
        if (IsDiscoveredInWorld(checkpointName)) return false;

        DiscoveredCheckpoints.Add(new FixedString64Bytes(checkpointName));
        WorldPointsGenerated.Value += pointsPerPlayer;

        Debug.Log($"[WorldCheckpointState] Nuevo descubrimiento mundial: {checkpointName}. " +
                  $"Total mundial: {WorldPointsGenerated.Value} puntos.");
        return true;
    }
}