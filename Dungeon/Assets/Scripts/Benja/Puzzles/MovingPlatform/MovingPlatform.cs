using UnityEngine;

public class MovingPlatform : MonoBehaviour, IActivatable
{
    public Transform pointA;
    public Transform pointB;
    public float speed = 2f;
    public AudioSource audioSource;

    private bool _moving = false;
    private Transform _target;

    void Start() => _target = pointA;

    void Update()
    {
        if (!_moving) return;

        transform.position = Vector3.MoveTowards(
            transform.position, _target.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, _target.position) < 0.01f)
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
}