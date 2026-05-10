using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Componente de identidad del jugador para el Lobby. Es la versión "ligera"
/// que reemplaza al Player completo cuando estamos en escenas de menú/lobby.
///
/// El owner (cliente dueño) escribe directamente sus NetworkVariables.
/// El servidor solo recibe los cambios y los retransmite al resto.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class LobbyPlayerData : NetworkBehaviour
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
        Debug.Log($"[LobbyPlayerData] OnNetworkSpawn — IsOwner={IsOwner}, IsServer={IsServer}, OwnerClientId={OwnerClientId}, LocalClientId={NetworkManager.Singleton.LocalClientId}");

        // Solo el dueño escribe sus propios datos
        if (IsOwner)
        {
            string name = PlayerProfile.HasName ? PlayerProfile.Name : "Player";
            string id = PlayerProfile.PlayerId ?? "";

            PlayerName.Value = new FixedString64Bytes(name);
            PlayerId.Value = new FixedString64Bytes(id);

            Debug.Log($"[LobbyPlayerData] Owner ({OwnerClientId}) escribió: name='{name}', id='{id}'");
        }

        // Todos (incluido el host respecto a clientes remotos) se suscriben a cambios
        // para que la UI se entere cuando lleguen los datos.
        PlayerName.OnValueChanged += OnNameChanged;
    }

    public override void OnNetworkDespawn()
    {
        PlayerName.OnValueChanged -= OnNameChanged;
    }

    private void OnNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        Debug.Log($"[LobbyPlayerData] PlayerName cambió en cliente {NetworkManager.Singleton.LocalClientId}: '{oldValue}' → '{newValue}' (Owner: {OwnerClientId})");

        // Notificar al UI del lobby para que refresque
        var ui = FindFirstObjectByType<LobbyRoomUI>();
        if (ui != null) ui.RequestRefresh();
    }
}