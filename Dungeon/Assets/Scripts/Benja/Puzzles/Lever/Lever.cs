using UnityEngine;

public class Lever : MonoBehaviour, IInteractable, IActivator
{
    [Header("Configuración")]
    public bool canToggleOff = true;        // ¿Se puede desactivar?
    public PuzzleReceiver receiver;

    [Header("Visual")]
    public Animator animator;               // Animación de palanca

    private bool _isActive = false;
    public bool IsActive => _isActive;

    void Start()
    {
        receiver?.RegisterActivator(this);
    }

    public void Interact()
    {
        if (_isActive && !canToggleOff) return; // Solo activa una vez

        _isActive = !_isActive;

        animator?.SetBool("IsActive", _isActive);

        receiver?.Evaluate();
    }

    // Registra manualmente desde el Inspector si hace falta
    public void RegisterReceiver(PuzzleReceiver r) => receiver = r;
}