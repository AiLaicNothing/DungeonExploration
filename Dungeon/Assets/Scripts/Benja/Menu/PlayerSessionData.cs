using System.Collections;
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

    public NetworkVariable<int> SelectedCharacter =
        new(-1, writePerm: NetworkVariableWritePermission.Server);

    public NetworkVariable<ulong> CurrentCharacterNetId =
        new(0, writePerm: NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> CharacterSelectionResolved =
        new(false, writePerm: NetworkVariableWritePermission.Server);

    // CHANGE:
    // Live sync loop keeps the save slot updated while the player is online.
    private Coroutine liveSyncRoutine;

    [SerializeField]
    private float liveSyncIntervalSeconds = 1.0f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"[PlayerSessionData] OnNetworkSpawn " +
            $"Owner={OwnerClientId} IsOwner={IsOwner} IsServer={IsServer}");

        if (IsOwner)
        {
            local = this;

            Debug.Log($"[PlayerSessionData] Local session assigned Client={OwnerClientId}");

            SubmitIdentityServerRpc(PlayerProfile.PlayerId,PlayerProfile.Name);
        }

        SelectedCharacter.OnValueChanged += OnSelectedCharacterChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        SelectedCharacter.OnValueChanged -= OnSelectedCharacterChanged;

        if (liveSyncRoutine != null)
        {
            StopCoroutine(liveSyncRoutine);
            liveSyncRoutine = null;
        }
    }

    private void OnSelectedCharacterChanged(int previous, int current)
    {
        Debug.Log($"[PlayerSessionData] SelectedCharacter changed " +
            $"Client={OwnerClientId} {previous} -> {current}");
    }

    [ServerRpc]
    private void SubmitIdentityServerRpc(string playerId, string playerName)
    {
        Debug.Log($"[PlayerSessionData] SubmitIdentityServerRpc " +
            $"Player={playerName} ({playerId}) Client={OwnerClientId}");

        PlayerId.Value = new FixedString64Bytes(playerId);
        PlayerName.Value = new FixedString64Bytes(playerName);

        RestoreCharacterSelection(playerId, playerName);
    }

    private void RestoreCharacterSelection(string playerId, string playerName)
    {
        Debug.Log(
            $"[PlayerSessionData] Trying restore character PlayerId={playerId}"
        );

        if (SaveSlotManager.Instance == null)
        {
            Debug.LogWarning("[PlayerSessionData] SaveSlotManager missing");
            CharacterSelectionResolved.Value = true;
            return;
        }

        if (!SaveSlotManager.Instance.HasActiveSlot)
        {
            Debug.LogWarning("[PlayerSessionData] No active slot");
            CharacterSelectionResolved.Value = true;
            return;
        }

        SaveSlotManager.Instance.DebugDumpActiveSlot($"RestoreCharacterSelection playerId={playerId}");

        if (!SaveSlotManager.Instance.TryGetActivePlayerEntry(playerId, out PlayerSaveEntry entry))
        {
            Debug.LogWarning($"[PlayerSessionData] No save entry found for player {playerId}");

            CharacterSelectionResolved.Value = true;
            return;
        }

        Debug.Log($"[PlayerSessionData] Save entry found Character={entry.selectedCharacter}");

        if (entry.selectedCharacter < 0)
        {
            Debug.Log("[PlayerSessionData] Character not selected yet");
            CharacterSelectionResolved.Value = true;
            return;
        }

        SelectedCharacter.Value = entry.selectedCharacter;

        Debug.Log($"[PlayerSessionData] Character restored " +
            $"Client={OwnerClientId} Character={entry.selectedCharacter}");

        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.ServerReceiveSelection(OwnerClientId, entry.selectedCharacter);
        }

        // CHANGE:
        // Start live sync only after the player has an avatar.
        BeginLiveSyncServer();

        CharacterSelectionResolved.Value = true;
    }

    [Rpc(SendTo.Server)]
    public void SubmitCharacterSelectionRpc(int characterIndex)
    {
        Debug.Log($"[PlayerSessionData] SubmitCharacterSelectionRpc " +
            $"Client={OwnerClientId} Character={characterIndex}"
        );

        SelectedCharacter.Value = characterIndex;
        CharacterSelectionResolved.Value = true;

        // CHANGE:
        // Save selection immediately so the player entry exists even before the next save.
        if (PlayerSaveManager.Instance != null)
        {
            PlayerSaveManager.Instance.CaptureOrUpdateSelectionOnly(
                PlayerId.Value.ToString(),
                PlayerName.Value.ToString(),
                characterIndex
            );
        }

        if (CharacterSelectionManager.Instance != null)
        {
            CharacterSelectionManager.Instance.ServerReceiveSelection( OwnerClientId,characterIndex);
        }

        Debug.Log(
            $"[PlayerSessionData] Selection submitted and saved " +
            $"Client={OwnerClientId} Character={characterIndex}");
    }

    // CHANGE:
    // Called by the CharacterSelectionManager after the avatar is spawned.
    public void NotifyCharacterSpawned(ulong netId)
    {
        if (!IsServer)
            return;

        Debug.Log(
            $"[SESSION] NotifyCharacterSpawned " +
            $"Client={OwnerClientId} " +
            $"OldNetId={CurrentCharacterNetId.Value} " +
            $"NewNetId={netId}"
        );

        CurrentCharacterNetId.Value = netId;

        BeginLiveSyncServer();

        if (PlayerSaveManager.Instance != null &&
            SaveSlotManager.Instance != null &&
            SaveSlotManager.Instance.HasActiveSlot)
        {
            SaveSlotManager.Instance.DebugDumpActiveSlot(
                $"NotifyCharacterSpawned client={OwnerClientId} netId={netId}"
            );
        }
    }
    private void BeginLiveSyncServer()
    {
        if (!IsServer)
            return;

        if (liveSyncRoutine != null)
            StopCoroutine(liveSyncRoutine);

        liveSyncRoutine = StartCoroutine(LiveSyncLoop());
    }

    private IEnumerator LiveSyncLoop()
    {
        while (IsServer)
        {
            yield return new WaitForSeconds(liveSyncIntervalSeconds);

            if (PlayerSaveManager.Instance == null)
                continue;

            if (CurrentCharacterNetId.Value == 0)
                continue;

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue( CurrentCharacterNetId.Value, out NetworkObject character))
            {
                PlayerSaveManager.Instance.UpdateLiveSnapshotFromCharacter(character);
            }
        }
    }
}