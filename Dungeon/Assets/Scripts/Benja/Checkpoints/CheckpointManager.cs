using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Manager local del cliente. Mantiene un mapa de todos los Checkpoint en escena
/// para poder localizarlos por nombre, y orquesta la UI del panel de teletransporte.
///
/// NO es persistente entre escenas — se crea fresh en cada carga de Gameplay.
/// La verdad sobre qué checkpoints están descubiertos vive en WorldCheckpointState
/// (servidor autoritativo).
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TeleporterPanelUI teleportPanel;

    /// <summary>Compatibilidad con el código viejo: el "checkpoint activo" del jugador local.</summary>
    public Checkpoint activeCheckpoint
    {
        get
        {
            if (LocalPlayer.Controller == null) return null;
            var data = LocalPlayer.Controller.GetComponent<PlayerCheckpointData>();
            if (data == null) return null;
            string name = data.LastUsedCheckpoint.Value.ToString();
            return GetByName(name);
        }
    }

    private readonly Dictionary<string, Checkpoint> _checkpointsByName = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        // Indexar todos los Checkpoint de la escena por nombre
        var allCheckpoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
        foreach (var cp in allCheckpoints)
        {
            if (string.IsNullOrEmpty(cp.checkpointName))
            {
                Debug.LogError($"[CheckpointManager] Checkpoint en {cp.name} no tiene checkpointName!");
                continue;
            }
            if (_checkpointsByName.ContainsKey(cp.checkpointName))
            {
                Debug.LogError($"[CheckpointManager] Checkpoint duplicado: '{cp.checkpointName}'. Cada checkpoint debe tener nombre único.");
                continue;
            }
            _checkpointsByName[cp.checkpointName] = cp;
        }

        Debug.Log($"[CheckpointManager] Indexados {_checkpointsByName.Count} checkpoints en la escena.");
    }

    /// <summary>Busca un Checkpoint por su nombre único.</summary>
    public Checkpoint GetByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        _checkpointsByName.TryGetValue(name, out var cp);
        return cp;
    }

    /// <summary>Lista de checkpoints descubiertos a nivel mundo (todos los disponibles para teletransporte).</summary>
    public IEnumerable<Checkpoint> GetWorldDiscoveredCheckpoints()
    {
        if (WorldCheckpointState.Instance == null) yield break;

        var discoveredList = WorldCheckpointState.Instance.DiscoveredCheckpoints;
        for (int i = 0; i < discoveredList.Count; i++)
        {
            var cp = GetByName(discoveredList[i].ToString());
            if (cp != null) yield return cp;
        }
    }

    public void OpenTeleportPanel()
    {
        if (teleportPanel != null) teleportPanel.Open();
    }

    public void CloseTeleportPanel()
    {
        if (teleportPanel != null) teleportPanel.Close();
    }
}