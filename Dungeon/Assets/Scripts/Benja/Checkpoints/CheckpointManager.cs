using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;

    public List<Checkpoint> unlockedCheckpoints = new List<Checkpoint>();
    public Checkpoint activeCheckpoint;
    public GameObject teleportPanel;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {

        if (Savesystem.Instance != null)
        {
            Savesystem.Instance.OnLoaded += SyncFromSaveSystem;

            SyncFromSaveSystem();
        }
    }

    void OnDestroy()
    {
        if (Savesystem.Instance != null)
            Savesystem.Instance.OnLoaded -= SyncFromSaveSystem;
    }


    public void SyncFromSaveSystem()
    {
        if (Savesystem.Instance == null) return;

        Checkpoint[] allCheckpoints = FindObjectsByType<Checkpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        string activeName = Savesystem.Instance.GetActiveCheckpointName();

        foreach (var cp in allCheckpoints)
        {
            if (cp == null || string.IsNullOrEmpty(cp.checkpointName)) continue;

            if (Savesystem.Instance.IsCheckpointActivated(cp.checkpointName))
            {
                RegisterCheckpoint(cp);

                if (cp.checkpointName == activeName)
                    activeCheckpoint = cp;
            }
        }

        Debug.Log($"[CheckpointManager] Sincronización: {unlockedCheckpoints.Count} checkpoints desbloqueados.");
    }

    public void SetActiveCheckpoint(Checkpoint checkpoint)
    {
        activeCheckpoint = checkpoint;
    }

    public void RegisterCheckpoint(Checkpoint checkpoint)
    {
        if (checkpoint == null) return;

        if (!unlockedCheckpoints.Contains(checkpoint))
        {
            unlockedCheckpoints.Add(checkpoint);
            Debug.Log("Checkpoint registrado: " + checkpoint.checkpointName);
        }
    }

    public void OpenTeleportPanel()
    {
        if (teleportPanel != null)
            teleportPanel.SetActive(true);
    }

    public void CloseTeleportPanel()
    {
        if (teleportPanel != null)
            teleportPanel.SetActive(false);
    }
}