using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Multiplayer;

/// <summary>
/// Sub-panel del MainMenu que muestra la lista de salas disponibles.
/// </summary>
public class RoomBrowserUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform roomContainer;
    [SerializeField] private GameObject roomPrefab;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject mainPanel; // para volver
    [SerializeField] private TMP_Text statusText;

    private readonly List<GameObject> _spawnedRooms = new();

    void OnEnable()
    {
        if (refreshButton != null) refreshButton.onClick.AddListener(RefreshRooms);
        if (backButton != null) backButton.onClick.AddListener(GoBack);

        RefreshRooms();
    }

    void OnDisable()
    {
        if (refreshButton != null) refreshButton.onClick.RemoveListener(RefreshRooms);
        if (backButton != null) backButton.onClick.RemoveListener(GoBack);
        ClearRooms();
    }

    public async void RefreshRooms()
    {
        ClearRooms();
        SetStatus("Buscando salas...");

        var result = await SessionManager.Instance.QueryAvailableSessions();

        if (result == null)
        {
            SetStatus("Error al buscar salas.");
            return;
        }

        if (result.Sessions.Count == 0)
        {
            SetStatus("No hay salas disponibles. Crea una nueva.");
            return;
        }

        SetStatus($"{result.Sessions.Count} sala(s) encontrada(s).");

        foreach (var session in result.Sessions)
        {
            GameObject room = Instantiate(roomPrefab, roomContainer);

            // Nombre y conteo
            var txt = room.GetComponentInChildren<TMP_Text>();
            if (txt != null)
                txt.text = $"{session.Name}  ({session.AvailableSlots} libres)";

            string id = session.Id;
            var btn = room.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => OnRoomClicked(id));

            _spawnedRooms.Add(room);
        }
    }

    private async void OnRoomClicked(string sessionId)
    {
        SetStatus("Uniéndose...");
        bool ok = await SessionManager.Instance.JoinSessionById(sessionId);
        if (!ok) SetStatus("Error al unirse. Intenta de nuevo.");
    }

    private void ClearRooms()
    {
        foreach (var r in _spawnedRooms)
            if (r != null) Destroy(r);
        _spawnedRooms.Clear();
    }

    private void GoBack()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        gameObject.SetActive(false);
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}