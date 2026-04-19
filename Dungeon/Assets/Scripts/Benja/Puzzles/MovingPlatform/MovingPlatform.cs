using UnityEngine;
[RequireComponent(typeof(Rigidbody))]

public class MovingPlatform : MonoBehaviour, IActivatable
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

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Start()
    {
        _target = pointA;
        _prevPosition = _rb.position;
    }

    void FixedUpdate()
    {
        if (!_moving)
        {
            CurrentVelocity = Vector3.zero;
            return;
        }

        Vector3 newPosition = Vector3.MoveTowards(
            _rb.position, _target.position, speed * Time.fixedDeltaTime);

        _rb.MovePosition(newPosition);

        CurrentVelocity = (newPosition - _prevPosition) / Time.fixedDeltaTime;
        _prevPosition = newPosition;

        if (Vector3.Distance(newPosition, _target.position) < 0.01f)
            _moving = false;
    }

    public void Activate()
    {
        _target = pointB;
        _moving = true;
        audioSource?.Play();
    }

    public void Deactivate()
    {
        _target = pointA;
        _moving = true;
    }

    void OnCollisionEnter(Collision collision)
    {

        var rider = collision.collider.GetComponentInParent<PlatformRider>();
        if (rider != null) rider.SetPlatform(this);
    }

    void OnCollisionExit(Collision collision)
    {
        var rider = collision.collider.GetComponentInParent<PlatformRider>();
        if (rider != null) rider.ClearPlatform(this);
    }
}