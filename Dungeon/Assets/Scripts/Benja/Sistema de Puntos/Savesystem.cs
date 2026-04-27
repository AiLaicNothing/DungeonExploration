using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Sistema de guardado en JSON a persistentDataPath.
/// Singleton opcional: puedes llamarlo vía SaveSystem.Save() / SaveSystem.Load().
/// </summary>
public class Savesystem : MonoBehaviour
{
    public static Savesystem Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private string fileName = "savegame.json";
    [SerializeField] private Transform playerTransform;
    [Tooltip("Si está activo, guarda automáticamente al activar un checkpoint.")]
    public bool autoSaveOnCheckpoint = true;

    private string SavePath => Path.Combine(Application.persistentDataPath, fileName);

    // ── Estado en memoria de los checkpoints activados ────────────────
    private HashSet<string> _activatedCheckpoints = new HashSet<string>();
    private string _activeCheckpointName;

    public event Action OnSaved;
    public event Action OnLoaded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (HasSave())
            Load();
    }

    // ── API pública ───────────────────────────────────────────────────
    public bool HasSave() => File.Exists(SavePath);

    public void Save()
    {
        var save = new GameSaveData
        {
            playerStats = PlayerStats.Instance != null ? PlayerStats.Instance.GetSaveData() : null,
            playerPosition = playerTransform != null ? PlayerPositionSaveData.From(playerTransform) : null,
            activatedCheckpoints = new List<string>(_activatedCheckpoints),
            activeCheckpointName = _activeCheckpointName,
            savedAtTicks = DateTime.UtcNow.Ticks
        };

        try
        {
            string json = JsonUtility.ToJson(save, prettyPrint: true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[Savesystem] Partida guardada en {SavePath}");
            OnSaved?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Savesystem] Error guardando: {e.Message}");
        }
    }

    public void Load()
    {
        if (!HasSave())
        {
            Debug.Log("[Savesystem] No hay partida guardada.");
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            var save = JsonUtility.FromJson<GameSaveData>(json);

            // Stats
            if (save.playerStats != null && PlayerStats.Instance != null)
                PlayerStats.Instance.LoadFromSaveData(save.playerStats);

            // Checkpoints
            _activatedCheckpoints = new HashSet<string>(save.activatedCheckpoints ?? new List<string>());
            _activeCheckpointName = save.activeCheckpointName;

            // Posición del jugador
            if (save.playerPosition != null && playerTransform != null)
            {
                playerTransform.position = save.playerPosition.Position;
                playerTransform.rotation = save.playerPosition.Rotation;
            }

            Debug.Log($"[Savesystem] Partida cargada de {SavePath}");
            OnLoaded?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Savesystem] Error cargando: {e.Message}");
        }
    }

    public void DeleteSave()
    {
        if (HasSave())
        {
            File.Delete(SavePath);
            _activatedCheckpoints.Clear();
            _activeCheckpointName = null;
            Debug.Log("[Savesystem] Archivo de partida eliminado.");
        }
    }

    /// <summary>
    /// Reset completo: borra el save y recarga la escena actual.
    /// Todos los sistemas vuelven a sus valores base al arrancar.
    /// Usar desde un botón "Nueva Partida" o "Reiniciar".
    /// </summary>
    public void ResetAndReloadScene()
    {
        DeleteSave();
        _activatedCheckpoints.Clear();
        _activeCheckpointName = null;

        // Importante: si el juego estaba pausado al pulsar el botón,
        // restauramos el timeScale antes de recargar la escena.
        Time.timeScale = 1f;

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene.name);
    }

    /// <summary>
    /// Reset "en caliente": borra el save y resetea los objetos en memoria
    /// sin recargar la escena. Útil para un botón de reset en debug/testing.
    /// </summary>
    public void ResetInPlace(Vector3? spawnPosition = null)
    {
        DeleteSave();

        if (PlayerStats.Instance != null)
            PlayerStats.Instance.ResetToDefaults();

        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.unlockedCheckpoints.Clear();
            CheckpointManager.Instance.activeCheckpoint = null;
        }

        // Notifica a todos los Checkpoint que se refresquen
        // (sus `activated` vuelven a false porque el HashSet ya está vacío)
        OnLoaded?.Invoke();

        if (spawnPosition.HasValue && playerTransform != null)
            playerTransform.position = spawnPosition.Value;

        Debug.Log("[Savesystem] Reset en caliente completado.");
    }

    // ── Checkpoints ───────────────────────────────────────────────────
    public bool IsCheckpointActivated(string checkpointName)
        => _activatedCheckpoints.Contains(checkpointName);

    public void MarkCheckpointActivated(string checkpointName)
    {
        if (string.IsNullOrEmpty(checkpointName)) return;
        _activatedCheckpoints.Add(checkpointName);
    }

    public void SetActiveCheckpoint(string checkpointName)
    {
        _activeCheckpointName = checkpointName;
    }

    public string GetActiveCheckpointName() => _activeCheckpointName;
}