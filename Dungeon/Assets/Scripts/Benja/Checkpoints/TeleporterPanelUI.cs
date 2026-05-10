using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel de teletransporte. Genera un botón por cada checkpoint descubierto
/// a nivel mundo. Marca visualmente cuál es el "checkpoint activo" del jugador local
/// (el último que usó).
/// </summary>
public class TeleporterPanelUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private TMP_Text activeCheckpointText;
    [SerializeField] private Color activeColor = new Color(0.4f, 0.9f, 0.4f);
    [SerializeField] private Color normalColor = Color.white;

    private readonly List<GameObject> _spawnedButtons = new();

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void Open()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Refresh()
    {
        // Limpiar
        foreach (var b in _spawnedButtons) if (b != null) Destroy(b);
        _spawnedButtons.Clear();

        if (CheckpointManager.Instance == null) return;
        if (LocalPlayer.Controller == null)
        {
            Debug.LogWarning("[TeleporterPanelUI] No hay LocalPlayer registrado.");
            return;
        }

        var playerData = LocalPlayer.Controller.GetComponent<PlayerCheckpointData>();
        string activeName = playerData != null ? playerData.LastUsedCheckpoint.Value.ToString() : "";

        // Mostrar etiqueta de checkpoint activo
        if (activeCheckpointText != null)
        {
            if (string.IsNullOrEmpty(activeName))
                activeCheckpointText.text = "Checkpoint activo: (ninguno)";
            else
                activeCheckpointText.text = $"Checkpoint activo: {activeName}";
        }

        // Crear botones para cada checkpoint descubierto a nivel mundo
        foreach (var cp in CheckpointManager.Instance.GetWorldDiscoveredCheckpoints())
        {
            GameObject btnGO = Instantiate(buttonPrefab, buttonContainer);
            _spawnedButtons.Add(btnGO);

            var label = btnGO.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                bool isActive = cp.checkpointName == activeName;
                label.text = cp.checkpointName + (isActive ? "  (Actual)" : "");
                label.color = isActive ? activeColor : normalColor;
            }

            // Marcar visualmente el actual con color de fondo si tiene Image
            var img = btnGO.GetComponent<Image>();
            if (img != null)
                img.color = (cp.checkpointName == activeName) ? activeColor : normalColor;

            string targetName = cp.checkpointName;
            var btn = btnGO.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => TeleportTo(targetName));
        }
    }

    /// <summary>Teletransporta al jugador local al checkpoint indicado.</summary>
    private void TeleportTo(string checkpointName)
    {
        if (LocalPlayer.Controller == null) return;
        var cp = CheckpointManager.Instance.GetByName(checkpointName);
        if (cp == null || cp.spawnPoint == null) return;

        // Pedir al servidor que mueva al jugador (autoritativo)
        LocalPlayer.Controller.RequestTeleportToCheckpoint(checkpointName);
        Close();
    }
}