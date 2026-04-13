using UnityEngine;
using UnityEngine.InputSystem;

public class Checkpoint : MonoBehaviour , IInteractable
{
    public Transform spawnPoint;
    public string checkpointName;
    public GameObject activateUI;
    public GameObject openPanelUI;

    private bool activated = false; 

    public CheckpointVisual visual;

    public void Interact()
    {
        if (!activated)
        {
            ActivateCheckpoint();
        }
        else
        {
            OpenCheckpointPanel();
        }
    }

    void ActivateCheckpoint()
    {
        Debug.Log($"Checkpoint '{checkpointName}' activado");

        activated = true;

        CheckpointManager.Instance.SetActiveCheckpoint(this);
        CheckpointManager.Instance.RegisterCheckpoint(this);

        if (visual != null)
            visual.ActivateVisual();

        activateUI.SetActive(false);
        openPanelUI.SetActive(true);
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