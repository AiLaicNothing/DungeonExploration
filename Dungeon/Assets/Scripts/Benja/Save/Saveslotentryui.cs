using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Componente de UI para representar una entrada de partida guardada.
/// Se instancia en el LoadGameScreen para cada SaveSlot.
/// 
/// Setup:
///   - Crear un prefab con este script
///   - Añadir los elementos UI necesarios (nombre, fecha, botones)
///   - Asignar referencias en el inspector
/// </summary>
public class SaveSlotEntryUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text saveNameText;
    [SerializeField] private TMP_Text lastPlayedText;
    [SerializeField] private TMP_Text playTimeText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteButton;

    private string _saveId;
    private Action<string> _onLoad;
    private Action<string> _onDelete;

    /// <summary>
    /// Inicializa la entrada con los datos del slot.
    /// </summary>
    public void Setup(SaveSlotMetadata metadata, Action<string> onLoad, Action<string> onDelete)
    {
        _saveId = metadata.saveId;
        _onLoad = onLoad;
        _onDelete = onDelete;

        // Rellenar textos
        if (saveNameText != null)
            saveNameText.text = metadata.saveName;

        if (lastPlayedText != null)
            lastPlayedText.text = $"Última vez: {metadata.GetLastPlayedString()}";

        if (playTimeText != null)
            playTimeText.text = $"Tiempo jugado: {metadata.GetPlayTimeString()}";

        if (playerCountText != null)
        {
            string plural = metadata.playerCount == 1 ? "jugador" : "jugadores";
            playerCountText.text = $"{metadata.playerCount} {plural}";
        }

        // Conectar botones
        if (loadButton != null)
        {
            loadButton.onClick.RemoveAllListeners();
            loadButton.onClick.AddListener(OnLoadClicked);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteClicked);
        }
    }

    private void OnLoadClicked()
    {
        _onLoad?.Invoke(_saveId);
    }

    private void OnDeleteClicked()
    {
        _onDelete?.Invoke(_saveId);
    }
}