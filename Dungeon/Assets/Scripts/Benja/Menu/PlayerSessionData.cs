using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Componente por-jugador que sincroniza el identificador y nombre del jugador entre clientes.
/// Va en el prefab del Player completo (Gameplay).
///
/// El PlayerId es importante para el sistema de rejoin: cuando un cliente
/// se reconecta, el host lo identifica por PlayerId (no por clientId, que cambia).
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class PlayerSessionData : NetworkBehaviour
{
    public NetworkVariable<FixedString64Bytes> PlayerName = new NetworkVariable<FixedString64Bytes>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    public NetworkVariable<FixedString64Bytes> PlayerId = new NetworkVariable<FixedString64Bytes>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            string name = PlayerProfile.HasName ? PlayerProfile.Name : "Player";
            string id = PlayerProfile.PlayerId ?? "";

            PlayerName.Value = new FixedString64Bytes(name);
            PlayerId.Value = new FixedString64Bytes(id);

            Debug.Log($"[PlayerSessionData] Owner ({OwnerClientId}) escribió: name='{name}', id='{id}'");
        }
    }
}