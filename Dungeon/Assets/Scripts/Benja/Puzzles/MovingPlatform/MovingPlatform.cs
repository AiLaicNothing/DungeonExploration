using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovingPlatform : NetworkBehaviour, IActivatable
{
    [Header("Movimiento")]
    public Transform pointA;
    public Transform pointB;
    public float speed = 2f;

    [Header("Audio")]
    public AudioSource audioSource;

    public Vector3 CurrentVelocity { get; private set; }

    private Rigidbody _rb;

    private bool _moving = false;
    private Transform _target;
    private Vector3 _prevPosition;

    private NetworkVariable<bool> _isActive =
        new NetworkVariable<bool>(false);

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        _rb.isKinematic = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Start()
    {
        _target = pointA;
        _prevPosition = _rb.position;
    }

    public override void OnNetworkSpawn()
    {
        _isActive.OnValueChanged += OnPlatformStateChanged;

        ApplyState(_isActive.Value, false);
    }

    public override void OnNetworkDespawn()
    {
        _isActive.OnValueChanged -= OnPlatformStateChanged;
    }

    private void FixedUpdate()
    {
        if (!_moving)
        {
            CurrentVelocity = Vector3.zero;
            return;
        }

        Vector3 newPosition = Vector3.MoveTowards(
            _rb.position,
            _target.position,
            speed * Time.fixedDeltaTime);

        _rb.MovePosition(newPosition);

        CurrentVelocity =
            (newPosition - _prevPosition) / Time.fixedDeltaTime;

        _prevPosition = newPosition;

        if (Vector3.Distance(newPosition, _target.position) < 0.01f)
        {
            _rb.MovePosition(_target.position);

            CurrentVelocity = Vector3.zero;
            _moving = false;
        }
    }

    private void OnPlatformStateChanged(bool previous, bool current)
    {
        ApplyState(current, true);
    }

    private void ApplyState(bool active, bool playAudio)
    {
        _target = active ? pointB : pointA;

        _moving = true;

        if (playAudio && audioSource != null)
        {
            if (!audioSource.isPlaying)
                audioSource.Play();
        }
    }

    public void Activate()
    {
        if (!IsServer) return;

        _isActive.Value = true;
    }

    public void Deactivate()
    {
        if (!IsServer) return;

        _isActive.Value = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        var rider = collision.collider.GetComponentInParent<PlatformRider>();

        if (rider != null)
            rider.SetPlatform(this);
    }

    private void OnCollisionExit(Collision collision)
    {
        var rider = collision.collider.GetComponentInParent<PlatformRider>();

        if (rider != null)
            rider.ClearPlatform(this);
    }
}