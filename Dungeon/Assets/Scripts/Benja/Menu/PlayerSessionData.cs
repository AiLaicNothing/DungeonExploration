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

    public NetworkVariable<ulong> CurrentCharacterNetId =
    new(0, writePerm: NetworkVariableWritePermission.Server);
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log(
           $"[PlayerSessionData] OnNetworkSpawn " +
           $"Owner={OwnerClientId} " +
           $"IsOwner={IsOwner} " +
           $"IsServer={IsServer}"
       );

        // SOLO el dueño envía sus datos
        if (IsOwner)
        {
            local = this;

            Debug.Log(
                $"[PlayerSessionData] Local session assigned " +
                $"Client={OwnerClientId}"
            );

            SubmitIdentityServerRpc(    
                PlayerProfile.PlayerId,
                PlayerProfile.Name
            );
        }

        SelectedCharacter.OnValueChanged += OnSelectedCharacterChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        SelectedCharacter.OnValueChanged -= OnSelectedCharacterChanged;
    }

    private void OnSelectedCharacterChanged(int previous, int current)
    {
        Debug.Log(
            $"[PlayerSessionData] SelectedCharacter changed " +
            $"Client={OwnerClientId} " +
            $"{previous} -> {current}"
        );
    }

    [ServerRpc]
    private void SubmitIdentityServerRpc(string playerId, string playerName)
    {

        Debug.Log(
           $"[PlayerSessionData] SubmitIdentityServerRpc " +
           $"Player={playerName} ({playerId}) " +
           $"Client={OwnerClientId}"
       );

        PlayerId.Value = new FixedString64Bytes(playerId);
        PlayerName.Value = new FixedString64Bytes(playerName);

        RestoreCharacterSelection(playerId);

        // 🔥 Integración con Save System
        //if (SaveGameIntegration.Instance != null)
        //{
        //    SaveGameIntegration.Instance.OnPlayerSpawned(NetworkObject, playerId);
        //}
    }

    private void RestoreCharacterSelection(string playerId)
    {
        Debug.Log(
            $"[PlayerSessionData] Trying restore character " +
            $"PlayerId={playerId}"
        );

        if (SaveSlotManager.Instance == null)
        {
            Debug.LogWarning(
                "[PlayerSessionData] SaveSlotManager missing"
            );

            return;
        }

        if (!SaveSlotManager.Instance.HasActiveSlot)
        {
            Debug.LogWarning(
                "[PlayerSessionData] No active slot"
            );

            return;
        }

        PlayerSaveEntry entry =
            SaveSlotManager.Instance
                .ActiveSlot
                .players
                .Find(p => p.playerId == playerId);

        if (entry == null)
        {
            Debug.Log(
                $"[PlayerSessionData] No save entry for player {playerId}"
            );

            return;
        }

        Debug.Log(
            $"[PlayerSessionData] Save entry found " +
            $"Character={entry.selectedCharacter}"
        );

        if (entry.selectedCharacter < 0)
        {
            Debug.Log(
                "[PlayerSessionData] Character not selected yet"
            );

            return;
        }

        // IMPORTANT
        // restore network variable
        SelectedCharacter.Value =
            entry.selectedCharacter;

        Debug.Log(
            $"[PlayerSessionData] Character restored " +
            $"Client={OwnerClientId} " +
            $"Character={entry.selectedCharacter}"
        );

        // IMPORTANT
        // tell manager to spawn correct prefab
        CharacterSelectionManager.Instance
            .ServerReceiveSelection(
                OwnerClientId,
                entry.selectedCharacter
            );
    }


    [Rpc(SendTo.Server)]
    public void SubmitCharacterSelectionRpc(int characterIndex)
    {
        Debug.Log(
            $"[PlayerSessionData] SubmitCharacterSelectionRpc " +
            $"Client={OwnerClientId} " +
            $"Character={characterIndex}"
        );

        SelectedCharacter.Value =
            characterIndex;

        CharacterSelectionManager.Instance
            .ServerReceiveSelection(
                OwnerClientId,
                characterIndex
            );

        // FORCE SAVE UPDATE
        if (PlayerSaveManager.Instance != null)
        {
            PlayerSaveManager.Instance
                .CaptureAndUpdatePlayerInActiveSlot(
                    NetworkObject
                );

            Debug.Log(
                "[PlayerSessionData] Character saved into slot"
            );
        }

        // OPTIONAL:
        // immediately save to disk
        if (SaveSlotManager.Instance != null)
        {
            SaveSlotManager.Instance.SaveActiveSlot();

            Debug.Log(
                "[PlayerSessionData] Active slot written to disk"
            );
        }
    }
}