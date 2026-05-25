using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class CharacterSelectionManager : NetworkBehaviour
{
    public static CharacterSelectionManager Instance;

    [Header("Characters")]
    [SerializeField] private CharacterData[] characters;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    //==================================================
    // SERVER DATA
    //==================================================

    private Dictionary<ulong, int> selectedCharacters = new Dictionary<ulong, int>();

    private Dictionary<ulong, NetworkObject> spawnedCharacters = new Dictionary<ulong, NetworkObject>();

    //==================================================
    // UNITY
    //==================================================

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(HandleExistingSelections());
    }

    //==================================================
    // EXISTING SELECTIONS
    //==================================================

    private IEnumerator HandleExistingSelections()
    {
        yield return new WaitForSeconds(1f);

        if (!NetworkManager.Singleton.IsServer) yield break;

        PlayerSessionData[] sessions = FindObjectsByType<PlayerSessionData>(FindObjectsSortMode.None);

        foreach (var session in sessions)
        {
            int selected = session.SelectedCharacter.Value;

            // never selected
            if (selected < 0) continue;

            ulong clientId = session.OwnerClientId;

            // already spawned
            if (spawnedCharacters.ContainsKey(clientId)) continue;

            selectedCharacters[clientId] = selected;

            SpawnCharacter(clientId);
        }
    }

    //==================================================
    // GETTERS
    //==================================================

    public CharacterData GetCharacter(int index)
    {
        if (index < 0 || index >= characters.Length) return null;

        return characters[index];
    }

    public int CharacterCount => characters.Length;

    //==================================================
    // SERVER RECEIVE
    //==================================================

    public void ServerReceiveSelection(ulong clientId, int characterIndex)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (characterIndex < 0 || characterIndex >= characters.Length) return;

        selectedCharacters[clientId] = characterIndex;

        SpawnCharacter(clientId);
    }

    //==================================================
    // SPAWN
    //==================================================

    private void SpawnCharacter(ulong clientId)
    {
        CharacterData data = characters[selectedCharacters[clientId]];

        if (data == null || data.playerPrefab == null)
        {
            Debug.LogWarning("Character prefab missing");
            return;
        }

        // REMOVE OLD CHARACTER
        if (spawnedCharacters.ContainsKey(clientId))
        {
            if (spawnedCharacters[clientId] != null)
            {
                spawnedCharacters[clientId].Despawn(true);
            }
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject obj = Instantiate(data.playerPrefab, spawnPoint.position, spawnPoint.rotation);

        NetworkObject netObj = obj.GetComponent<NetworkObject>();

        // replace current player object
        netObj.SpawnWithOwnership(clientId);

        spawnedCharacters[clientId] = netObj;

        Debug.Log($"Spawned {data.characterName} for Client {clientId}");
    }

    //==================================================
    // RESPAWN
    //==================================================

    public void RespawnPlayer(ulong clientId)
    {
        PlayerSessionData session = null;

        foreach (var s in FindObjectsByType<PlayerSessionData>(FindObjectsSortMode.None))
        {
            if (s.OwnerClientId == clientId)
            {
                session = s;
                break;
            }
        }

        if (session == null) return;

        int selected = session.SelectedCharacter.Value;

        if (selected < 0) return;

        selectedCharacters[clientId] = selected;

        SpawnCharacter(clientId);
    }
}