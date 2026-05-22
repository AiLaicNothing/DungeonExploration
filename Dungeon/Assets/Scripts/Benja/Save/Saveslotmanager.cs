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

    /// <summary>El slot de guardado activo actualmente (la partida que se está jugando).</summary>
    public SaveSlotData ActiveSlot { get; private set; }

    /// <summary>True si hay un slot activo cargado.</summary>
    public bool HasActiveSlot => ActiveSlot != null;

    void Awake()
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

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ══════════════════════════════════════════════════════════════════
    // CREAR NUEVO SLOT
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Crea un nuevo slot de guardado vacío y lo marca como activo.
    /// Llamar desde el menú "Nueva Partida".
    /// </summary>
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
        return slot;
    }

    // ══════════════════════════════════════════════════════════════════
    // CARGAR SLOT EXISTENTE
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Carga un slot existente desde disco y lo marca como activo.
    /// Llamar desde el menú "Cargar Partida".
    /// </summary>
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
        return true;
    }

    // ══════════════════════════════════════════════════════════════════
    // GUARDAR SLOT ACTIVO
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Guarda el slot activo en disco. Llamar periódicamente (autosave) o manualmente.
    /// </summary>
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
    }

    // ══════════════════════════════════════════════════════════════════
    // LISTAR SLOTS
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Devuelve lista de todos los slots guardados (solo metadatos ligeros).
    /// Usado para mostrar el menú de "Cargar Partida".
    /// </summary>
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

    // ══════════════════════════════════════════════════════════════════
    // BORRAR SLOT
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Elimina un slot de guardado del disco.</summary>
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

    // ══════════════════════════════════════════════════════════════════
    // LIMPIEZA
    // ══════════════════════════════════════════════════════════════════

    /// <summary>Limpia el slot activo (usado al volver al menú principal).</summary>
    public void ClearActiveSlot()
    {
        ActiveSlot = null;
        Debug.Log("[SaveSlotManager] Slot activo limpiado.");
    }

    // ══════════════════════════════════════════════════════════════════
    // DISCO (I/O)
    // ══════════════════════════════════════════════════════════════════

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

// ══════════════════════════════════════════════════════════════════════
// METADATA (para UI de listado de partidas)
// ══════════════════════════════════════════════════════════════════════

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