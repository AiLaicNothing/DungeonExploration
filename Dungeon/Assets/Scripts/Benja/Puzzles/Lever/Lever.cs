using Unity.Netcode;
using UnityEngine;

public class Lever : NetworkBehaviour, IInteractable, IActivator
{
    [Header("Configuración")]
    public bool canToggleOff = true;
    public PuzzleReceiver receiver;

    [Header("Visual")]
    public Animator animator;

    private NetworkVariable<bool> _isActive =
        new NetworkVariable<bool>(false);

    public bool IsActive => _isActive.Value;

    private void Start()
    {
        receiver?.RegisterActivator(this);
    }

    public override void OnNetworkSpawn()
    {
        _isActive.OnValueChanged += OnLeverStateChanged;

        animator?.SetBool("IsActive", _isActive.Value);
    }

    public override void OnNetworkDespawn()
    {
        _isActive.OnValueChanged -= OnLeverStateChanged;
    }

    private void OnLeverStateChanged(bool previous, bool current)
    {
        animator?.SetBool("IsActive", current);
    }

    public void Interact()
    {
        ToggleLeverServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleLeverServerRpc()
    {
        if (_isActive.Value && !canToggleOff)
            return;

        _isActive.Value = !_isActive.Value;

        receiver?.Evaluate();
    }

    public void RegisterReceiver(PuzzleReceiver r)
    {
        receiver = r;
    }
}