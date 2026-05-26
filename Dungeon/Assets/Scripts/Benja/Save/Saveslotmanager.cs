using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Gestor principal de slots de guardado.
/// Singleton persistente (DontDestroyOnLoad).
/// 
/// Responsabilidades:
///   - Crear/cargar/borrar slots de guardado
///   - Mantener referencia al slot activo actual
///   - Gestionar archivos en disco (Application.persistentDataPath)
///   - Proveer listado de partidas guardadas
///   
/// NO maneja la lógica de mundo/jugadores directamente - delega a los managers específicos.
/// </summary>
public class SaveSlotManager : MonoBehaviour
{
    public static SaveSlotManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private int maxSlots = 10;

    private string SaveDirectory => Path.Combine(Application.persistentDataPath, "Saves");

    public SaveSlotData ActiveSlot { get; private set; }

    public bool HasActiveSlot => ActiveSlot != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureSaveDirectoryExists();
        Debug.Log($"[SaveSlotManager] Inicializado. Directorio: {SaveDirectory}");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public SaveSlotData CreateNewSlot(string saveName = null)
    {
        string saveId = Guid.NewGuid().ToString();
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var slot = new SaveSlotData
        {
            saveId = saveId,
            saveName = saveName ?? $"Partida {GetAllSlots().Count + 1}",
            createdTimestamp = now,
            lastPlayedTimestamp = now,
            totalPlayTimeSeconds = 0,
            worldData = new WorldSaveData(),
            players = new List<PlayerSaveEntry>()
        };

        ActiveSlot = slot;
        SaveSlotToDisk(slot);

        Debug.Log($"[SaveSlotManager] Nueva partida creada: '{slot.saveName}' (ID: {saveId})");
        DebugDumpActiveSlot("CreateNewSlot");
        return slot;
    }

    public bool LoadSlot(string saveId)
    {
        var slot = LoadSlotFromDisk(saveId);
        if (slot == null)
        {
            Debug.LogError($"[SaveSlotManager] No se pudo cargar el slot {saveId}");
            return false;
        }

        ActiveSlot = slot;
        ActiveSlot.lastPlayedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        Debug.Log($"[SaveSlotManager] Partida cargada: '{slot.saveName}' (ID: {saveId})");
        DebugDumpActiveSlot("LoadSlot");
        return true;
    }

    public void SaveActiveSlot()
    {
        if (ActiveSlot == null)
        {
            Debug.LogWarning("[SaveSlotManager] No hay slot activo para guardar.");
            return;
        }

        ActiveSlot.lastPlayedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        SaveSlotToDisk(ActiveSlot);

        Debug.Log($"[SaveSlotManager] Partida guardada: '{ActiveSlot.saveName}'");
        DebugDumpActiveSlot("SaveActiveSlot");
    }

    public List<SaveSlotMetadata> GetAllSlotsMetadata()
    {
        var slots = GetAllSlots();
        return slots.Select(s => new SaveSlotMetadata
        {
            saveId = s.saveId,
            saveName = s.saveName,
            createdTimestamp = s.createdTimestamp,
            lastPlayedTimestamp = s.lastPlayedTimestamp,
            totalPlayTimeSeconds = s.totalPlayTimeSeconds,
            playerCount = s.players.Count
        }).OrderByDescending(s => s.lastPlayedTimestamp).ToList();
    }

    private List<SaveSlotData> GetAllSlots()
    {
        EnsureSaveDirectoryExists();
        var files = Directory.GetFiles(SaveDirectory, "*.json");
        var slots = new List<SaveSlotData>();

        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var slot = JsonUtility.FromJson<SaveSlotData>(json);
                if (slot != null) slots.Add(slot);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSlotManager] Error leyendo {file}: {e.Message}");
            }
        }

        return slots;
    }

    public bool DeleteSlot(string saveId)
    {
        string path = GetSlotPath(saveId);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveSlotManager] Slot {saveId} no existe.");
            return false;
        }

        try
        {
            File.Delete(path);
            Debug.Log($"[SaveSlotManager] Slot {saveId} eliminado.");

            if (ActiveSlot != null && ActiveSlot.saveId == saveId)
                ActiveSlot = null;

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSlotManager] Error eliminando slot: {e.Message}");
            return false;
        }
    }

    public void ClearActiveSlot()
    {
        ActiveSlot = null;
        Debug.Log("[SaveSlotManager] Slot activo limpiado.");
    }

    // CHANGE:
    // This helper prints the current slot content.
    // Why:
    // It proves whether player 2 exists in memory when the server tries to restore them.
    public void DebugDumpActiveSlot(string context)
    {
        if (ActiveSlot == null)
        {
            Debug.LogWarning($"[SaveSlotManager] DebugDumpActiveSlot({context}) -> ActiveSlot is NULL");
            return;
        }

        string path = GetSlotPath(ActiveSlot.saveId);
        bool fileExists = File.Exists(path);
        long fileSize = fileExists ? new FileInfo(path).Length : -1;

        Debug.Log($"[SaveSlotManager] DebugDumpActiveSlot({context}) -> " +
            $"SaveId={ActiveSlot.saveId} SaveName={ActiveSlot.saveName} " +
            $"Players={ActiveSlot.players.Count} FileExists={fileExists} FileSize={fileSize}");

        for (int i = 0; i < ActiveSlot.players.Count; i++)
        {
            PlayerSaveEntry p = ActiveSlot.players[i];
            Debug.Log( $"[SaveSlotManager]   Player[{i}] id={p.playerId} name={p.playerName} " + $"char={p.selectedCharacter} pos={p.position.ToVector3()} " +
                $"lastPos={p.lastKnownPosition.ToVector3()} scene={p.currentScene} " +
                $"lastScene={p.lastKnownScene} connected={p.isConnected} spawned={p.hasSpawnedAvatar}");
        }
    }

    // CHANGE:
    // Safe lookup for a player entry.
    // Why:
    // Restoring/spawning should fail loudly if the save does not contain that player.
    public bool TryGetActivePlayerEntry(string playerId, out PlayerSaveEntry entry)
    {
        entry = null;

        if (ActiveSlot == null)
        {
            Debug.LogWarning($"[SaveSlotManager] TryGetActivePlayerEntry failed. ActiveSlot null. PlayerId={playerId}");
            return false;
        }

        entry = ActiveSlot.players.FirstOrDefault(p => p.playerId == playerId);

        if (entry == null)
        {
            Debug.LogWarning($"[SaveSlotManager] No player entry in active slot. PlayerId={playerId}. PlayersInSlot={ActiveSlot.players.Count}");
            return false;
        }

        Debug.Log(
            $"[SaveSlotManager] Player entry found. PlayerId={playerId} " + $"SelectedCharacter={entry.selectedCharacter} Pos={entry.position.ToVector3()} LastPos={entry.lastKnownPosition.ToVector3()} Scene={entry.currentScene}");

        return true;
    }

    // CHANGE:
    // Upsert means the player stays in the save even if they later disconnect.
    public void UpsertPlayerEntry(PlayerSaveEntry entry, bool saveToDisk = true)
    {
        if (entry == null)
        {
            Debug.LogWarning("[SaveSlotManager] UpsertPlayerEntry called with null entry.");
            return;
        }

        if (ActiveSlot == null)
        {
            Debug.LogWarning("[SaveSlotManager] UpsertPlayerEntry failed. ActiveSlot null.");
            return;
        }

        var existing = ActiveSlot.players.FirstOrDefault(p => p.playerId == entry.playerId);

        if (existing != null)
        {
            ActiveSlot.players.Remove(existing);
        }

        ActiveSlot.players.Add(entry);

        Debug.Log($"[SaveSlotManager] Player upserted. PlayerId={entry.playerId} " + $"SelectedCharacter={entry.selectedCharacter} SaveToDisk={saveToDisk}"
        );

        DebugDumpActiveSlot("UpsertPlayerEntry");

        if (saveToDisk)
            SaveActiveSlot();
    }

    private void SaveSlotToDisk(SaveSlotData slot)
    {
        EnsureSaveDirectoryExists();
        string path = GetSlotPath(slot.saveId);

        try
        {
            string json = JsonUtility.ToJson(slot, true);
            File.WriteAllText(path, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSlotManager] Error guardando slot: {e.Message}");
        }
    }

    private SaveSlotData LoadSlotFromDisk(string saveId)
    {
        string path = GetSlotPath(saveId);
        if (!File.Exists(path)) return null;

        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<SaveSlotData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSlotManager] Error cargando slot: {e.Message}");
            return null;
        }
    }

    private string GetSlotPath(string saveId) => Path.Combine(SaveDirectory, $"{saveId}.json");

    private void EnsureSaveDirectoryExists()
    {
        if (!Directory.Exists(SaveDirectory))
            Directory.CreateDirectory(SaveDirectory);
    }
}

[Serializable]
public class SaveSlotMetadata
{
    public string saveId;
    public string saveName;
    public long createdTimestamp;
    public long lastPlayedTimestamp;
    public float totalPlayTimeSeconds;
    public int playerCount;

    public string GetLastPlayedString()
    {
        var date = DateTimeOffset.FromUnixTimeSeconds(lastPlayedTimestamp).LocalDateTime;
        return date.ToString("dd/MM/yyyy HH:mm");
    }

    public string GetPlayTimeString()
    {
        var span = TimeSpan.FromSeconds(totalPlayTimeSeconds);
        return $"{span.Hours}h {span.Minutes}m";
    }
}