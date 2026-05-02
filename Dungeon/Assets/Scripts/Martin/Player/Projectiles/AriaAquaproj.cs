using System;
using UnityEngine;

public class AriaAquaproj : MonoBehaviour
{
    private Rigidbody rb;

    private Vector3 center;
    private float travelTime;
    private float timer;

    private Action onArrive;

    [Header("Spiral")]
    public float spiralStrength = 5f;

    public void Initialize(Vector3 centerPos, float time, Action callback)
    {
        rb = GetComponent<Rigidbody>();

        center = centerPos;
        travelTime = time;
        onArrive = callback;

        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;

        float t = timer / travelTime;

        if (t >= 1f)
        {
            ReachCenter();
            return;
        }

        Vector3 toCenter = (center - transform.position).normalized;

        Vector3 perpendicular = Vector3.Cross(toCenter, Vector3.up).normalized;

        Vector3 finalDir = (toCenter + perpendicular * spiralStrength * (1f - t)).normalized;

        float distance = Vector3.Distance(transform.position, center);
        float speed = distance / (travelTime - timer + 0.01f); // ensures same  time

        rb.linearVelocity = finalDir * speed;

        // rotate for visuals
        transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
    }

    void ReachCenter()
    {
        rb.linearVelocity = Vector3.zero;

        onArrive?.Invoke();

        Destroy(gameObject);
    }
}
