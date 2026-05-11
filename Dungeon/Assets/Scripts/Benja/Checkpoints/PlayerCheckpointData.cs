using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Estado de checkpoints por jugador. Va en el prefab del Player junto a NetworkObject.
///
/// Mantiene:
///   - Lista de checkpoints que ESTE jugador descubrió personalmente (para no dar puntos repetidos)
///   - Último checkpoint usado (para respawn personal)
///
/// Persistencia local: el cliente owner sube su data a su PlayerPrefs al cambiar.
/// Al unirse a una sala, el cliente sube su data anterior al servidor para restaurarla.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class PlayerCheckpointData : NetworkBehaviour
{
    /// <summary>Checkpoints que este jugador ha descubierto personalmente.</summary>
    public NetworkList<FixedString64Bytes> PersonallyDiscovered;

    /// <summary>Nombre del último checkpoint que el jugador usó (para respawn).</summary>
    public NetworkVariable<FixedString64Bytes> LastUsedCheckpoint = new NetworkVariable<FixedString64Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        PersonallyDiscovered = new NetworkList<FixedString64Bytes>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Subir datos guardados localmente al servidor para restaurar progreso
            UploadLocalDataToServer();

            // Suscribirse a cambios para auto-guardar localmente
            PersonallyDiscovered.OnListChanged += _ => SaveLocalData();
            LastUsedCheckpoint.OnValueChanged += (_, _) => SaveLocalData();
        }
    }

    /// <summary>
    /// Sube los datos guardados localmente (por PlayerId) al servidor.
    /// Esto permite que cuando un jugador vuelve a una nueva partida, el servidor
    /// sepa qué checkpoints ya descubrió personalmente antes.
    /// </summary>
    private void UploadLocalDataToServer()
    {
        var local = PlayerCheckpointStorage.Load(PlayerProfile.PlayerId);
        if (local == null) return;

        // NGO no soporta string[] directamente en RPCs — lo envolvemos en NetworkArrayString
        // (definido en PlayerStats.cs).
        var discoveredWrapped = new NetworkArrayString(local.discoveredCheckpoints);
        string lastUsed = local.lastUsedCheckpoint ?? "";

        UploadDataServerRpc(discoveredWrapped, lastUsed);
    }

    [ServerRpc]
    private void UploadDataServerRpc(NetworkArrayString discovered, string lastUsed)
    {
        var discoveredList = discovered.ToList();

        // Si la lista del servidor está vacía pero el cliente trae datos, restauramos
        if (PersonallyDiscovered.Count == 0 && discoveredList.Count > 0)
        {
            foreach (var cp in discoveredList)
            {
                if (!string.IsNullOrEmpty(cp))
                    PersonallyDiscovered.Add(new FixedString64Bytes(cp));
            }
        }

        if (string.IsNullOrEmpty(LastUsedCheckpoint.Value.ToString()) && !string.IsNullOrEmpty(lastUsed))
        {
            LastUsedCheckpoint.Value = new FixedString64Bytes(lastUsed);
        }

        Debug.Log($"[PlayerCheckpointData] Datos restaurados del cliente {OwnerClientId}: " +
                  $"{discoveredList.Count} descubiertos, último='{lastUsed}'");
    }

    /// <summary>SOLO SERVIDOR. Marca un checkpoint como descubierto personalmente.</summary>
    public void MarkPersonallyDiscovered(string checkpointName)
    {
        if (!IsServer) return;
        var key = new FixedString64Bytes(checkpointName);
        for (int i = 0; i < PersonallyDiscovered.Count; i++)
            if (PersonallyDiscovered[i] == key) return; // ya estaba
        PersonallyDiscovered.Add(key);
    }

    /// <summary>True si este jugador ya descubrió este checkpoint personalmente.</summary>
    public bool HasPersonallyDiscovered(string checkpointName)
    {
        var key = new FixedString64Bytes(checkpointName);
        for (int i = 0; i < PersonallyDiscovered.Count; i++)
            if (PersonallyDiscovered[i] == key) return true;
        return false;
    }

    /// <summary>SOLO SERVIDOR. Setea el último checkpoint usado.</summary>
    public void SetLastUsed(string checkpointName)
    {
        if (!IsServer) return;
        LastUsedCheckpoint.Value = new FixedString64Bytes(checkpointName);
    }

    /// <summary>
    /// SOLO SERVIDOR. Setea el respawn SOLO si el jugador no tenía uno previo.
    /// Usado cuando descubres un checkpoint nuevo personalmente:
    ///   - Si era tu primer checkpoint, pasa a ser tu respawn
    ///   - Si ya tenías otro respawn, NO se cambia (decisión consciente del jugador)
    /// </summary>
    public void SetLastUsedIfEmpty(string checkpointName)
    {
        if (!IsServer) return;

        string current = LastUsedCheckpoint.Value.ToString();
        if (string.IsNullOrEmpty(current))
        {
            LastUsedCheckpoint.Value = new FixedString64Bytes(checkpointName);
            Debug.Log($"[PlayerCheckpointData] Cliente {OwnerClientId}: primer checkpoint, respawn = '{checkpointName}'");
        }
    }

    /// <summary>
    /// El cliente pide al servidor cambiar su checkpoint de respawn.
    /// </summary>
    [ServerRpc]
    public void RequestSetRespawnServerRpc(string checkpointName, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;

        LastUsedCheckpoint.Value = new FixedString64Bytes(checkpointName);
        Debug.Log($"[PlayerCheckpointData] Cliente {OwnerClientId} cambió su respawn a '{checkpointName}'");
    }

    /// <summary>Guarda los datos en disco local (PlayerPrefs) usando el PlayerId.</summary>
    private void SaveLocalData()
    {
        if (!IsOwner) return;

        var snapshot = new PlayerCheckpointStorage.Data
        {
            playerId = PlayerProfile.PlayerId,
            lastUsedCheckpoint = LastUsedCheckpoint.Value.ToString(),
            discoveredCheckpoints = new string[PersonallyDiscovered.Count]
        };
        for (int i = 0; i < PersonallyDiscovered.Count; i++)
            snapshot.discoveredCheckpoints[i] = PersonallyDiscovered[i].ToString();

        PlayerCheckpointStorage.Save(PlayerProfile.PlayerId, snapshot);
    }
}