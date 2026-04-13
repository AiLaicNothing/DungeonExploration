using UnityEngine;

public class PuzzleDoor : MonoBehaviour, IActivatable
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

    void Start()
    {
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
    }

    void Update()
    {
        if (!_isMoving) return;

        transform.position = Vector3.MoveTowards(
            transform.position, _targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, _targetPosition) < 0.001f)
        {
            transform.position = _targetPosition; 
            _isMoving = false;
        }
    }

    public void Activate()
    {
        _targetPosition = _openPosition;
        _isMoving = true;
        audioSource?.PlayOneShot(openClip);
    }

    public void Deactivate()
    {
        _targetPosition = _closedPosition;
        _isMoving = true;
        audioSource?.PlayOneShot(closeClip);
    }
}