using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Datos persistentes de sesión del jugador.
/// Cada cliente envía su identidad real al servidor.
/// </summary>
public class PlayerSessionData : NetworkBehaviour
{
    public static PlayerSessionData local;

    public NetworkVariable<FixedString64Bytes> PlayerId =
        new(writePerm: NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> PlayerName =
        new(writePerm: NetworkVariableWritePermission.Server);

    public NetworkVariable<int> SelectedCharacter = new(-1, writePerm: NetworkVariableWritePermission.Server);
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // SOLO el dueño envía sus datos
        if (IsOwner)
        {
            local = this;

            SubmitIdentityServerRpc(    
                PlayerProfile.PlayerId,
                PlayerProfile.Name
            );
        }
    }

    [ServerRpc]
    private void SubmitIdentityServerRpc(string playerId, string playerName)
    {
        PlayerId.Value = new FixedString64Bytes(playerId);
        PlayerName.Value = new FixedString64Bytes(playerName);

        Debug.Log($"[PlayerSessionData] Identity recibida: {playerName} ({playerId})");

        // 🔥 Integración con Save System
        if (SaveGameIntegration.Instance != null)
        {
            SaveGameIntegration.Instance.OnPlayerSpawned(NetworkObject, playerId);
        }
    }

    [Rpc(SendTo.Server)]
    public void SubmitCharacterSelectionRpc(int characterIndex)
    {
        SelectedCharacter.Value = characterIndex;

        CharacterSelectionManager.Instance.ServerReceiveSelection( OwnerClientId, characterIndex);
    }
}