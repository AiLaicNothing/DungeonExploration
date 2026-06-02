using Unity.Netcode;
using UnityEngine;

public class ChallengePlatform : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float sinkDepth = 10f;
    [SerializeField] private float moveSpeed = 3f;

    private Vector3 _visiblePosition;
    private Vector3 _hiddenPosition;
    private Vector3 _targetPosition;

    private bool _isMoving;

    private NetworkVariable<bool> _isVisible =
        new(false);

    private void Start()
    {
        _visiblePosition = transform.position;

        _hiddenPosition =
            _visiblePosition - Vector3.up * sinkDepth;

        transform.position = _hiddenPosition;

        _targetPosition = _hiddenPosition;
    }

    public override void OnNetworkSpawn()
    {
        _isVisible.OnValueChanged += OnVisibilityChanged;

        ApplyState(_isVisible.Value);
    }

    public override void OnNetworkDespawn()
    {
        _isVisible.OnValueChanged -= OnVisibilityChanged;
    }

    private void Update()
    {
        if (!_isMoving)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            _targetPosition,
            moveSpeed * Time.deltaTime);

        if (Vector3.Distance(
            transform.position,
            _targetPosition) < 0.01f)
        {
            transform.position = _targetPosition;
            _isMoving = false;
        }
    }

    private void OnVisibilityChanged(bool previous, bool current)
    {
        ApplyState(current);
    }

    private void ApplyState(bool visible)
    {
        _targetPosition =
            visible
            ? _visiblePosition
            : _hiddenPosition;

        _isMoving = true;
    }

    public void Show()
    {
        if (!IsServer)
            return;

        _isVisible.Value = true;
    }

    public void Hide()
    {
        if (!IsServer)
            return;

        _isVisible.Value = false;
    }
}