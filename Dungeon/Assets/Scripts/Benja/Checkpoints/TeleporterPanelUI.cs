using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel de checkpoints: combina teletransporte + selección de respawn.
///
/// Botones por entrada:
///   - "Viajar" → teleporta sin cambiar respawn
///   - "Hacer mi respawn aquí" → cambia respawn; luego pregunta si quieres viajar también
///
/// Estados visuales:
///   - ⭐ Checkpoint donde estás físicamente
///   - 🏠 Checkpoint marcado como tu respawn
///   - 📍 Checkpoint disponible
/// </summary>
public class TeleporterPanelUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private GameObject checkpointEntryPrefab;
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private Button closeButton;

    [Header("Colores")]
    [SerializeField] private Color colorCurrentCheckpoint = new Color(0.4f, 0.9f, 0.4f);
    [SerializeField] private Color colorRespawn = new Color(1f, 0.8f, 0.3f);
    [SerializeField] private Color colorNormal = Color.white;

    [Header("Iconos / Etiquetas")]
    [SerializeField] private string iconCurrent = "⭐";
    [SerializeField] private string iconRespawn = "🏠";
    [SerializeField] private string iconNormal = "📍";

    private readonly List<GameObject> _spawnedEntries = new();

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        Refresh();

        if (UIBlockingManager.Instance != null)
            UIBlockingManager.Instance.Register(this);
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        if (UIBlockingManager.Instance != null)
            UIBlockingManager.Instance.Unregister(this);
    }

    private void Refresh()
    {
        // Limpiar entradas viejas
        foreach (var e in _spawnedEntries) if (e != null) Destroy(e);
        _spawnedEntries.Clear();

        if (CheckpointManager.Instance == null) return;
        if (LocalPlayer.Controller == null) return;

        var playerData = LocalPlayer.Controller.GetComponent<PlayerCheckpointData>();
        if (playerData == null) return;

        string respawnName = playerData.LastUsedCheckpoint.Value.ToString();
        string currentName = GetCurrentCheckpointName();

        // Header
        if (headerText != null)
        {
            if (string.IsNullOrEmpty(respawnName))
                headerText.text = "Respawn actual: (zona inicial)";
            else
                headerText.text = $"Respawn actual: {iconRespawn} {respawnName}";
        }

        // Una entrada por cada checkpoint descubierto mundialmente
        foreach (var cp in CheckpointManager.Instance.GetWorldDiscoveredCheckpoints())
        {
            CreateCheckpointEntry(cp.checkpointName, respawnName, currentName);
        }
    }

    private string GetCurrentCheckpointName()
    {
        if (LocalPlayer.Controller == null) return "";

        Vector3 playerPos = LocalPlayer.Controller.transform.position;
        float closestDist = 5f;
        string closestName = "";

        foreach (var cp in CheckpointManager.Instance.GetWorldDiscoveredCheckpoints())
        {
            if (cp.spawnPoint == null) continue;
            float dist = Vector3.Distance(playerPos, cp.spawnPoint.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestName = cp.checkpointName;
            }
        }

        return closestName;
    }

    private void CreateCheckpointEntry(string cpName, string respawnName, string currentName)
    {
        GameObject entry = Instantiate(checkpointEntryPrefab, buttonContainer);
        _spawnedEntries.Add(entry);

        bool isCurrent = cpName == currentName;
        bool isRespawn = cpName == respawnName;

        // Buscar componentes recursivamente (no solo hijos directos)
        var nameLabel = FindDeepChild<TMP_Text>(entry.transform, "NameText");
        var statusLabel = FindDeepChild<TMP_Text>(entry.transform, "StatusText");
        var travelBtn = FindDeepChild<Button>(entry.transform, "TravelButton");
        var respawnBtn = FindDeepChild<Button>(entry.transform, "RespawnButton");

        // Aviso si falta alguno
        if (nameLabel == null) Debug.LogWarning($"[TeleporterPanelUI] No se encontró 'NameText' en el prefab CheckpointEntry.");
        if (travelBtn == null) Debug.LogWarning($"[TeleporterPanelUI] No se encontró 'TravelButton' en el prefab CheckpointEntry.");
        if (respawnBtn == null) Debug.LogWarning($"[TeleporterPanelUI] No se encontró 'RespawnButton' en el prefab CheckpointEntry.");

        // Nombre con icono
        if (nameLabel != null)
        {
            string icon = isRespawn ? iconRespawn : (isCurrent ? iconCurrent : iconNormal);
            nameLabel.text = $"{icon} {cpName}";
            nameLabel.color = isRespawn ? colorRespawn
                            : isCurrent ? colorCurrentCheckpoint
                            : colorNormal;
        }

        // Status
        if (statusLabel != null)
        {
            if (isCurrent && isRespawn) statusLabel.text = "Aquí estás · Respawn activo";
            else if (isCurrent) statusLabel.text = "Aquí estás";
            else if (isRespawn) statusLabel.text = "Respawn activo";
            else statusLabel.text = "Disponible";
        }

        // Botón Viajar
        if (travelBtn != null)
        {
            travelBtn.gameObject.SetActive(!isCurrent);
            travelBtn.onClick.RemoveAllListeners();

            string targetName = cpName;
            travelBtn.onClick.AddListener(() => OnTravelClicked(targetName));
        }

        // Botón Marcar respawn
        if (respawnBtn != null)
        {
            respawnBtn.gameObject.SetActive(!isRespawn);
            respawnBtn.onClick.RemoveAllListeners();

            string targetName = cpName;
            respawnBtn.onClick.AddListener(() => OnSetRespawnClicked(targetName));
        }
    }

    /// <summary>Busca un componente en el GameObject o en cualquier hijo (recursivo).</summary>
    private T FindDeepChild<T>(Transform parent, string childName) where T : Component
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                var component = child.GetComponent<T>();
                if (component != null) return component;
            }

            var found = FindDeepChild<T>(child, childName);
            if (found != null) return found;
        }
        return null;
    }

    private void OnTravelClicked(string checkpointName)
    {
        Debug.Log($"[TeleporterPanelUI] Click VIAJAR a '{checkpointName}'");

        if (LocalPlayer.Controller == null)
        {
            Debug.LogError("[TeleporterPanelUI] LocalPlayer.Controller es null. No se puede viajar.");
            return;
        }

        Debug.Log($"[TeleporterPanelUI] Llamando a RequestTeleportToCheckpoint('{checkpointName}')");
        LocalPlayer.Controller.RequestTeleportToCheckpoint(checkpointName);

        Close();
    }

    private void OnSetRespawnClicked(string checkpointName)
    {
        Debug.Log($"[TeleporterPanelUI] Click MARCAR RESPAWN a '{checkpointName}'");

        if (LocalPlayer.Controller == null)
        {
            Debug.LogError("[TeleporterPanelUI] LocalPlayer.Controller es null.");
            return;
        }

        var playerData = LocalPlayer.Controller.GetComponent<PlayerCheckpointData>();
        if (playerData == null)
        {
            Debug.LogError("[TeleporterPanelUI] PlayerCheckpointData no encontrado en LocalPlayer.");
            return;
        }

        Debug.Log($"[TeleporterPanelUI] Llamando a RequestSetRespawnServerRpc('{checkpointName}')");
        playerData.RequestSetRespawnServerRpc(checkpointName);

        // Preguntar si también quiere viajar
        if (ConfirmDialogUI.Instance != null)
        {
            Debug.Log("[TeleporterPanelUI] Mostrando ConfirmDialog");
            ConfirmDialogUI.Instance.Show(
                title: "Respawn actualizado",
                message: $"'{checkpointName}' es ahora tu punto de respawn.\n\n¿Quieres viajar ahí también?",
                onYes: () =>
                {
                    Debug.Log($"[TeleporterPanelUI] Usuario eligió viajar a '{checkpointName}'");
                    LocalPlayer.Controller.RequestTeleportToCheckpoint(checkpointName);
                    Close();
                },
                onNo: () =>
                {
                    Debug.Log("[TeleporterPanelUI] Usuario eligió quedarse");
                    Refresh();
                },
                yesLabel: "Sí, viajar",
                noLabel: "No, quedarme"
            );
        }
        else
        {
            Debug.LogError("[TeleporterPanelUI] ConfirmDialogUI.Instance es NULL. " +
                          "Asegúrate de tener un GameObject 'ConfirmDialog' con el script ConfirmDialogUI en escena.");
            Refresh();
        }
    }
}