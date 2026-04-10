using UnityEngine;
using UnityEngine.InputSystem;

public class Checkpoint : MonoBehaviour
{
    public Transform spawnPoint;
    public string checkpointName;
    public GameObject activateUI;
    public GameObject openPanelUI;

    private bool playerInside = false;
    private bool activated = false;

    public CheckpointVisual visual;

    public void Interact()
    {
        if (!playerInside) return;

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
        Debug.Log("Activado");

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
        if (other.CompareTag("Player"))
        {
            playerInside = true;

            if (activated)
            {
                openPanelUI.SetActive(true);
            }
            else
            {
                activateUI.SetActive(true);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;

            activateUI.SetActive(false);
            openPanelUI.SetActive(false);

            CheckpointManager.Instance.CloseTeleportPanel();
        }
    }
}