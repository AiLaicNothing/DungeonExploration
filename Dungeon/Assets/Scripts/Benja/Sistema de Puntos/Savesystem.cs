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
    // Se mantiene aquí para no depender de PlayerPrefs
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
        // Intenta cargar al iniciar (si hay partida guardada)
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
            Debug.Log($"[SaveSystem] Partida guardada en {SavePath}");
            OnSaved?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Error guardando: {e.Message}");
        }
    }

    public void Load()
    {
        if (!HasSave())
        {
            Debug.Log("[SaveSystem] No hay partida guardada.");
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
                // Si hay un checkpoint activo, preferimos spawnear allí
                if (!string.IsNullOrEmpty(_activeCheckpointName))
                {
                    // Lo dejamos así y que el CheckpointManager decida al hacer respawn.
                    // Para la carga inicial, usamos la posición guardada.
                }
                playerTransform.position = save.playerPosition.Position;
                playerTransform.rotation = save.playerPosition.Rotation;
            }

            Debug.Log($"[SaveSystem] Partida cargada de {SavePath}");
            OnLoaded?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Error cargando: {e.Message}");
        }
    }

    public void DeleteSave()
    {
        if (HasSave())
        {
            File.Delete(SavePath);
            _activatedCheckpoints.Clear();
            _activeCheckpointName = null;
            Debug.Log("[SaveSystem] Partida eliminada.");
        }
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