using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sistema de interacción del jugador. Mantiene una lista de IInteractable
/// dentro de su trigger de detección, y activa el más cercano al pulsar E.
///
/// En multiplayer:
///   - Solo el OWNER del Player puede interactuar (los demás clientes ignoran input).
///   - Cada cliente mantiene su propia lista local de interactables cercanos.
///   - El Interact() de cada interactable es responsable de manejar la red
///     (típicamente vía ServerRpc, como hace Checkpoint).
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class PlayerInteraction : NetworkBehaviour
{
    private readonly HashSet<IInteractable> _interactablesInRange = new();
    private IInteractable _closestInteractable;

    /// <summary>Acceso público al interactable más cercano (útil para UI/highlight).</summary>
    public IInteractable ClosestInteractable => _closestInteractable;

    /// <summary>Disparado cuando cambia el closest (útil para UI dinámica "Pulsa E para...").</summary>
    public event System.Action<IInteractable> OnClosestChanged;

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsOpen)
            return;

        if (!IsOwner) return;

        _closestInteractable?.Interact();
    }

    void OnTriggerEnter(Collider other)
    {
        // Optimización: solo procesamos triggers para el owner
        // (los demás clientes no necesitan tracking de interactables)
        if (!IsOwner) return;

        var interactables = other.GetComponentsInChildren<IInteractable>();
        foreach (var interactable in interactables)
            _interactablesInRange.Add(interactable);

        UpdateClosest();
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsOwner) return;

        var interactables = other.GetComponentsInChildren<IInteractable>();
        foreach (var interactable in interactables)
            _interactablesInRange.Remove(interactable);

        UpdateClosest();
    }

    void UpdateClosest()
    {
        IInteractable newClosest = null;
        float minDist = float.MaxValue;

        // Limpieza: descartar interactables que ya no son válidos (despawneados, destruidos)
        _interactablesInRange.RemoveWhere(i => i is MonoBehaviour mb && mb == null);

        foreach (var interactable in _interactablesInRange)
        {
            if (interactable is MonoBehaviour mb && mb != null)
            {
                float dist = Vector3.Distance(transform.position, mb.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    newClosest = interactable;
                }
            }
        }

        if (newClosest != _closestInteractable)
        {
            _closestInteractable = newClosest;
            OnClosestChanged?.Invoke(_closestInteractable);
        }
    }
}