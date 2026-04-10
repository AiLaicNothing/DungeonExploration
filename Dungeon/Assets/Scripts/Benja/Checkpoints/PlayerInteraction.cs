using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    Checkpoint currentCheckpoint;

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (currentCheckpoint != null)
        {
            currentCheckpoint.Interact();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Checkpoint checkpoint = other.GetComponent<Checkpoint>();

        if (checkpoint != null)
        {
            currentCheckpoint = checkpoint;
        }
    }

    void OnTriggerExit(Collider other)
    {
        Checkpoint checkpoint = other.GetComponent<Checkpoint>();

        if (checkpoint != null && checkpoint == currentCheckpoint)
        {
            currentCheckpoint = null;
        }
    }
}