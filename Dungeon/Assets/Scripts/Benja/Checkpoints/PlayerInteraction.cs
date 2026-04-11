using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;


public class PlayerInteraction : MonoBehaviour
{
    private readonly HashSet<IInteractable> _interactablesInRange = new();
    private IInteractable _closestInteractable;

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        _closestInteractable?.Interact();
    }

    void OnTriggerEnter(Collider other)
    {
        var interactables = other.GetComponentsInChildren<IInteractable>();
        foreach (var interactable in interactables)
        {
            _interactablesInRange.Add(interactable);
        }

        UpdateClosest();
    }

    void OnTriggerExit(Collider other)
    {
        var interactables = other.GetComponentsInChildren<IInteractable>();
        foreach (var interactable in interactables)
        {
            _interactablesInRange.Remove(interactable);
        }

        UpdateClosest();
    }

    void UpdateClosest()
    {
        _closestInteractable = null;
        float minDist = float.MaxValue;

        foreach (var interactable in _interactablesInRange)
        {
            if (interactable is MonoBehaviour mb)
            {
                float dist = Vector3.Distance(transform.position, mb.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    _closestInteractable = interactable;
                }
            }
        }
    }
}