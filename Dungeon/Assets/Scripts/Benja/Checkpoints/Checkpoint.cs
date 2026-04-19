using UnityEngine;
using UnityEngine.InputSystem;

public class Checkpoint : MonoBehaviour , IInteractable
{
    [Header("Info")]
    public string checkpointName; 
    public Transform spawnPoint;

    [Header("UI")]
    public GameObject activateUI;
    public GameObject openPanelUI;

    [Header("Recompensa")]
    public int upgradePointsReward = 5;

    [Header("Visual")]
    public CheckpointVisual visual;

    private bool activated = false;

    void Start()
    {
        RefreshFromSaveSystem();

        if (Savesystem.Instance != null)
            Savesystem.Instance.OnLoaded += RefreshFromSaveSystem;
    }

    void OnDestroy()
    {
        if (Savesystem.Instance != null)
            Savesystem.Instance.OnLoaded -= RefreshFromSaveSystem;
    }

    void RefreshFromSaveSystem()
    {
        if (Savesystem.Instance == null) return;

        activated = Savesystem.Instance.IsCheckpointActivated(checkpointName);

        if (activated && visual != null)
            visual.ActivateVisual();
    }

    public void Interact()
    {
        if (!activated)
            ActivateCheckpoint();
        else
            OpenCheckpointPanel();
    }

    void ActivateCheckpoint()
    {
        activated = true;

        Savesystem.Instance.MarkCheckpointActivated(checkpointName);
        Savesystem.Instance.SetActiveCheckpoint(checkpointName);

        PlayerStats.Instance.AddUpgradePoints(upgradePointsReward);

        CheckpointManager.Instance.SetActiveCheckpoint(this);
        CheckpointManager.Instance.RegisterCheckpoint(this);

        if (visual != null) visual.ActivateVisual();

        activateUI.SetActive(false);
        openPanelUI.SetActive(true);

        if (Savesystem.Instance.autoSaveOnCheckpoint)
            Savesystem.Instance.Save();
    }

    void OpenCheckpointPanel()
    {
        CheckpointManager.Instance.OpenTeleportPanel();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (activated)
            openPanelUI.SetActive(true);
        else
            activateUI.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        activateUI.SetActive(false);
        openPanelUI.SetActive(false);
        CheckpointManager.Instance.CloseTeleportPanel();
    }
}