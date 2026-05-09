using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Componente por-jugador que sincroniza el identificador y nombre del jugador entre clientes.
/// Va en el prefab del Player junto a NetworkObject y PlayerController.
///
/// El cliente owner reporta sus datos al servidor en OnNetworkSpawn.
/// El servidor los guarda en NetworkVariables que todos pueden leer.
///
/// El PlayerId es importante para el sistema de rejoin: cuando un cliente
/// se reconecta, el host lo identifica por PlayerId (no por clientId, que cambia).
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class PlayerSessionData : NetworkBehaviour
{
    public NetworkVariable<FixedString64Bytes> PlayerName = new NetworkVariable<FixedString64Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> PlayerId = new NetworkVariable<FixedString64Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            string name = PlayerProfile.HasName ? PlayerProfile.Name : "Player";
            string id = PlayerProfile.PlayerId ?? "";
            SetProfileServerRpc(name, id);
        }
    }

    [ServerRpc]
    private void SetProfileServerRpc(string name, string id)
    {
        PlayerName.Value = name;
        PlayerId.Value = id;

        Debug.Log($"[PlayerSessionData] Server registró: name='{name}', id='{id}', clientId={OwnerClientId}");
    }
}