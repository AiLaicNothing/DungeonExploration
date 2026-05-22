using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Gestor de guardado/carga del estado del mundo.
/// SOLO SERVIDOR puede capturar/restaurar.
/// 
/// Responsabilidades:
///   - Capturar el estado actual del mundo (checkpoints, progreso global, puzzles)
///   - Restaurar el estado del mundo desde un SaveSlotData
///   - Sincronizar vía NetworkVariables (NGO se encarga de replicar a clientes)
///   
/// Setup:
///   - Añadir como componente al GameObject "WorldState" en la escena 04_Gameplay
///   - Asegurarse de que WorldCheckpointState existe en la misma escena
/// </summary>
public class WorldSaveManager : MonoBehaviour
{
    public static WorldSaveManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ══════════════════════════════════════════════════════════════════
    // CAPTURAR ESTADO DEL MUNDO
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// SOLO SERVIDOR. Captura el estado actual del mundo y lo devuelve como WorldSaveData.
    /// Llamar antes de guardar el slot.
    /// </summary>
    public WorldSaveData CaptureWorldState()
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("[WorldSaveManager] Solo el servidor puede capturar el estado del mundo.");
            return null;
        }

        var data = new WorldSaveData();

        // Checkpoints descubiertos
        if (WorldCheckpointState.Instance != null)
        {
            foreach (var cp in WorldCheckpointState.Instance.DiscoveredCheckpoints)
            {
                data.discoveredCheckpoints.Add(cp.ToString());
            }

            data.globalUpgradePointsGenerated = WorldCheckpointState.Instance.WorldPointsGenerated.Value;
        }

        // Bosses derrotados (si tienes un BossManager, aquí lo capturas)
        // Ejemplo:
        // if (BossManager.Instance != null)
        // {
        //     foreach (var bossId in BossManager.Instance.DefeatedBosses)
        //         data.defeatedBosses.Add(bossId);
        // }

        // Puzzles resueltos
        var puzzleReceivers = FindObjectsByType<PuzzleReceiver>(FindObjectsSortMode.None);
        foreach (var receiver in puzzleReceivers)
        {
            if (!string.IsNullOrEmpty(receiver.ReceiverID))
            {
                data.puzzleStates.Add(new PuzzleStateEntry
                {
                    puzzleId = receiver.ReceiverID,
                    isSolved = receiver.IsActive
                });
            }
        }

        Debug.Log($"[WorldSaveManager] Estado del mundo capturado: " +
                  $"{data.discoveredCheckpoints.Count} checkpoints, " +
                  $"{data.defeatedBosses.Count} bosses, " +
                  $"{data.puzzleStates.Count} puzzles.");

        return data;
    }

    // ══════════════════════════════════════════════════════════════════
    // RESTAURAR ESTADO DEL MUNDO
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// SOLO SERVIDOR. Restaura el estado del mundo desde WorldSaveData.
    /// Llamar después de cargar el slot y antes de spawnear jugadores.
    /// </summary>
    public void RestoreWorldState(WorldSaveData data)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("[WorldSaveManager] Solo el servidor puede restaurar el estado del mundo.");
            return;
        }

        if (data == null)
        {
            Debug.LogWarning("[WorldSaveManager] WorldSaveData es null, no se restaura nada.");
            return;
        }

        // Restaurar checkpoints
        if (WorldCheckpointState.Instance != null)
        {
            if (WorldCheckpointState.Instance.DiscoveredCheckpoints != null)
            {
                WorldCheckpointState.Instance.DiscoveredCheckpoints.Clear();
            }
            foreach (var cpName in data.discoveredCheckpoints)
            {
                WorldCheckpointState.Instance.TryDiscoverInWorld(cpName, 0); // 0 porque ya se dieron antes
            }

            WorldCheckpointState.Instance.WorldPointsGenerated.Value = data.globalUpgradePointsGenerated;
        }

        // Restaurar bosses derrotados
        // if (BossManager.Instance != null)
        // {
        //     BossManager.Instance.DefeatedBosses.Clear();
        //     foreach (var bossId in data.defeatedBosses)
        //         BossManager.Instance.MarkBossDefeated(bossId);
        // }

        // Restaurar puzzles
        var puzzleReceivers = FindObjectsByType<PuzzleReceiver>(FindObjectsSortMode.None);
        foreach (var entry in data.puzzleStates)
        {
            foreach (var receiver in puzzleReceivers)
            {
                if (receiver.ReceiverID == entry.puzzleId)
                {
                    receiver.SetStateDirectly(entry.isSolved);
                }
            }
        }

        Debug.Log($"[WorldSaveManager] Estado del mundo restaurado: " +
                  $"{data.discoveredCheckpoints.Count} checkpoints, " +
                  $"{data.defeatedBosses.Count} bosses, " +
                  $"{data.puzzleStates.Count} puzzles.");
    }

    // ══════════════════════════════════════════════════════════════════
    // HELPERS PÚBLICOS (para integración)
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Captura el mundo Y actualiza el slot activo en SaveSlotManager.
    /// Conveniente para autosave.
    /// </summary>
    public void CaptureAndUpdateActiveSlot()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (!SaveSlotManager.Instance.HasActiveSlot) return;

        var worldData = CaptureWorldState();
        if (worldData != null)
        {
            SaveSlotManager.Instance.ActiveSlot.worldData = worldData;
            Debug.Log("[WorldSaveManager] WorldData actualizado en ActiveSlot.");
        }
    }
}