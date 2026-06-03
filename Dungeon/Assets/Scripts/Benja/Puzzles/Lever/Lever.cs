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
        Debug.Log(
            $"[Lever] OnValueChanged -> {name} | {previous} -> {current}");

        animator?.SetBool("IsActive", current);
    }

    public void Interact()
    {
        Debug.Log($"[Lever] Interact llamado -> {name}");

        ToggleLeverServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleLeverServerRpc()
    {
        Debug.Log($"[Lever] ToggleLeverServerRpc -> {name}");

        if (_isActive.Value && !canToggleOff)
        {
            Debug.Log("[Lever] No se puede apagar");
            return;
        }

        SetStateInternal(!_isActive.Value);
    }

    public void SetState(bool state)
    {
        if (!IsServer)
            return;

        SetStateInternal(state);
    }

    private void SetStateInternal(bool state)
    {
        Debug.Log(
            $"[Lever] SetStateInternal -> {name} | Nuevo estado: {state}");

        _isActive.Value = state;

        receiver?.Evaluate();
    }

    public void RegisterReceiver(PuzzleReceiver r)
    {
        receiver = r;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();

        if (player == null) return;

        if (!player.IsOwner) return;

        InteractionUI.Instance.SetUp("Mover palanca");
        InteractionUI.Instance.ShowUI();
    }

    private void OnTriggerExit(Collider other)
    {
        InteractionUI.Instance.HideUI();
    }
}