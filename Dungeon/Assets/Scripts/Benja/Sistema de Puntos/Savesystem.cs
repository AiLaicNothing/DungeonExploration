using System;
using UnityEngine;

/// <summary>
/// LEGACY: Wrapper de compatibilidad para código viejo que aún referencia Savesystem.
///
/// En multiplayer ya NO se usa este sistema. La persistencia ahora es:
///   - PlayerProfile          → nombre del jugador
///   - PlayerCheckpointStorage → checkpoints descubiertos por PlayerId
///   - RejoinManager           → última sesión visitada
///   - WorldCheckpointState    → estado del mundo (en runtime, no persiste)
///   - PlayerStateRegistry     → snapshots por PlayerId (en RAM del servidor)
///
/// Este script existe SOLO para que scripts viejos no rompan. No guarda en disco.
/// Si encuentras código que aún usa Savesystem, migra al sistema nuevo.
/// </summary>
[Obsolete("Savesystem está deprecado en multiplayer. Usa PlayerCheckpointStorage, PlayerProfile, RejoinManager.")]
public class Savesystem : MonoBehaviour
{
    public static Savesystem Instance { get; private set; }

    public bool autoSaveOnCheckpoint = false; // ya no se usa, los snapshots los hace el servidor

    public event Action OnSaved;
    public event Action OnLoaded;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── No-ops compatibles ────────────────────────────────────────────
    public bool HasSave() => false;

    public void Save()
    {
        Debug.LogWarning("[Savesystem] Save() llamado pero el sistema está deprecado. Los datos persistentes ahora se gestionan automáticamente.");
        OnSaved?.Invoke();
    }

    public void Load()
    {
        Debug.LogWarning("[Savesystem] Load() llamado pero el sistema está deprecado.");
        OnLoaded?.Invoke();
    }

    public void DeleteSave()
    {
        // En el sistema nuevo, los datos del jugador local se borran con ResetProgressButton.
        Debug.LogWarning("[Savesystem] DeleteSave() llamado. Para borrar datos locales, usa ResetProgressButton.");
    }

    // ── Compatibilidad con checkpoints (redirige al nuevo sistema) ───
    /// <summary>
    /// LEGACY: ahora pregunta a WorldCheckpointState (el del servidor).
    /// </summary>
    public bool IsCheckpointActivated(string checkpointName)
    {
        if (WorldCheckpointState.Instance == null) return false;
        return WorldCheckpointState.Instance.IsDiscoveredInWorld(checkpointName);
    }

    public void MarkCheckpointActivated(string checkpointName)
    {
        Debug.LogWarning("[Savesystem] MarkCheckpointActivated() está deprecado. Ahora se hace vía Checkpoint.RequestActivateServerRpc.");
    }

    public void SetActiveCheckpoint(string checkpointName)
    {
        Debug.LogWarning("[Savesystem] SetActiveCheckpoint() está deprecado. Ahora cada PlayerCheckpointData guarda el LastUsedCheckpoint del jugador.");
    }

    public string GetActiveCheckpointName()
    {
        // El "último usado" ahora es por jugador, no global. Devolvemos el del jugador local.
        if (LocalPlayer.Controller == null) return "";
        var data = LocalPlayer.Controller.GetComponent<PlayerCheckpointData>();
        return data != null ? data.LastUsedCheckpoint.Value.ToString() : "";
    }
}