// PuzzleDoor.cs

using Unity.Netcode;
using UnityEngine;

public class PuzzleDoor : NetworkBehaviour, IActivatable
{
    public enum MoveAxis { X, Y, Z }

    [Header("Movimiento")]
    public MoveAxis axis = MoveAxis.Y;
    public float moveDistance = 3f;
    public float moveSpeed = 2f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip openClip;
    public AudioClip closeClip;

    private Vector3 _closedPosition;
    private Vector3 _openPosition;
    private Vector3 _targetPosition;

    private bool _isMoving = false;

    private NetworkVariable<bool> _isOpen =
        new NetworkVariable<bool>(false);

    private void Start()
    {
        Debug.Log($"[PuzzleDoor] Start -> {name}");

        _closedPosition = transform.position;

        Vector3 direction = axis switch
        {
            MoveAxis.X => Vector3.right,
            MoveAxis.Y => Vector3.up,
            MoveAxis.Z => Vector3.forward,
            _ => Vector3.up
        };

        _openPosition = _closedPosition + direction * moveDistance;

        _targetPosition = _closedPosition;

        Debug.Log($"[PuzzleDoor] ClosedPos: {_closedPosition}");
        Debug.Log($"[PuzzleDoor] OpenPos: {_openPosition}");
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[PuzzleDoor] OnNetworkSpawn -> {name}");

        _isOpen.OnValueChanged += OnDoorStateChanged;

        ApplyState(_isOpen.Value, false);
    }

    public override void OnNetworkDespawn()
    {
        _isOpen.OnValueChanged -= OnDoorStateChanged;
    }

    private void Update()
    {
        if (!_isMoving) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            _targetPosition,
            moveSpeed * Time.deltaTime);

        Debug.Log($"[PuzzleDoor] Moviendo puerta -> {transform.position}");

        if (Vector3.Distance(transform.position, _targetPosition) < 0.001f)
        {
            transform.position = _targetPosition;

            _isMoving = false;

            Debug.Log("[PuzzleDoor] Movimiento completado");
        }
    }

    private void OnDoorStateChanged(bool previous, bool current)
    {
        Debug.Log($"[PuzzleDoor] Estado cambió: {previous} -> {current}");

        ApplyState(current, true);
    }

    private void ApplyState(bool open, bool playAudio)
    {
        Debug.Log($"[PuzzleDoor] ApplyState -> Open = {open}");

        _targetPosition = open ? _openPosition : _closedPosition;

        _isMoving = true;

        if (!playAudio) return;

        if (open)
        {
            Debug.Log("[PuzzleDoor] Reproduciendo sonido OPEN");
            audioSource?.PlayOneShot(openClip);
        }
        else
        {
            Debug.Log("[PuzzleDoor] Reproduciendo sonido CLOSE");
            audioSource?.PlayOneShot(closeClip);
        }
    }

    public void Activate()
    {
        Debug.Log("[PuzzleDoor] Activate() llamado");

        if (!IsServer)
        {
            Debug.Log("[PuzzleDoor] Activate ignorado porque no soy server");
            return;
        }

        _isOpen.Value = true;

        Debug.Log("[PuzzleDoor] _isOpen = true");
    }

    public void Deactivate()
    {
        Debug.Log("[PuzzleDoor] Deactivate() llamado");

        if (!IsServer)
        {
            Debug.Log("[PuzzleDoor] Deactivate ignorado porque no soy server");
            return;
        }

        _isOpen.Value = false;

        Debug.Log("[PuzzleDoor] _isOpen = false");
    }
}