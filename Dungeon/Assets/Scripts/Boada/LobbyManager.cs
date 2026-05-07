using TMPro;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using System.Threading.Tasks;
using UnityEngine.UI;
using System.Collections.Generic;


public class LobbyManager : MonoBehaviour
{
    [SerializeField] Transform roomContainer;
    [SerializeField] GameObject roomPrefab;
    [SerializeField] TMP_InputField roomNameInput;
    private List<GameObject> spawnedRooms = new();


    async void Start()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    public async void CreateRoom()
    {
        string roomName = roomNameInput.text;

        SessionOptions options = new SessionOptions 
        {
            MaxPlayers = 4,
            IsPrivate = false,
        }.WithRelayNetwork();

        options.Name = roomName;

        IHostSession session = await MultiplayerService.Instance.CreateSessionAsync(options);

        Debug.Log("Sala creada!");

        Unity.Netcode.NetworkManager.Singleton.StartHost();

        RefreshRooms();
    }

    public async void JoinRoom(string sessionId)
    {
        await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId); 
        Debug.Log("Te uniste a la session:" + sessionId);

        Unity.Netcode.NetworkManager.Singleton.StartClient();

    }

    public async void RefreshRooms()
    {
        ClearRooms();
        
        QuerySessionsResults result = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions());

        foreach (ISessionInfo session in result.Sessions)
        {
            GameObject room = Instantiate(roomPrefab, roomContainer);

            TMP_Text txt = room.GetComponentInChildren<TMP_Text>();

            txt.text = session.Name;

            string id = session.Id;

            room.GetComponent<Button>().onClick.AddListener(() =>
            {
                JoinRoom(id); 
            });

            spawnedRooms.Add(room);

        }
    }

    void ClearRooms() 
    {
        foreach (GameObject room in spawnedRooms)
        {
            Destroy(room);
        }

        spawnedRooms.Clear(); 
    }
    

}