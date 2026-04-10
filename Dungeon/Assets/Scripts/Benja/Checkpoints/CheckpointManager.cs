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