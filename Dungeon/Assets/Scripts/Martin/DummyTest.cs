using System.Collections;
using UnityEngine;

public class DummyTest : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public float maxHP = 100f;
    [SerializeField] private float currentHP;

    [Header("Behavior")]
    public bool canBeAffected = true;

    [Header("Physics")]
    public Rigidbody rb;

    [Header("Forces")]
    public float pushForce = 5f;
    public float airForce = 7f;

    [Header("Air Control")]
    public float airHangTime = 0.4f;
    public float fallGravityMultiplier = 2f;

    Coroutine airRoutine;

    [Header("State")]
    public bool isStunned;

    Coroutine stunCoroutine;

    void Awake()
    {
        currentHP = maxHP;

        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    public void TakeDamage(float amount, ThrowType throwType, Vector3 hitDirection, float stunDuration, bool keepOnAir, float airLift)
    {
        currentHP -= amount;

        if (!canBeAffected)
            return;

        ApplyThrow(throwType, hitDirection);

        if (keepOnAir)
        {
            SustainAir(airLift);
        }

        ApplyStun(stunDuration);
    }

    void ApplyStun(float duration)
    {
        if (stunCoroutine != null)
            StopCoroutine(stunCoroutine);

        stunCoroutine = StartCoroutine(StunRoutine(duration));
    }

    IEnumerator StunRoutine(float duration)
    {
        isStunned = true;

        Debug.Log("Enemy stunned");

        yield return new WaitForSeconds(duration);

        isStunned = false;

        Debug.Log("Enemy recovered from stun");
    }
    void SustainAir(float lift)
    {
        Vector3 vel = rb.linearVelocity;

        if (vel.y < 0)
            vel.y = 0;

        vel.y += lift;

        rb.linearVelocity = vel;

        Debug.Log("Air sustained");
    }

    void ApplyThrow(ThrowType type, Vector3 dir)
    {
        switch (type)
        {
            case ThrowType.Push:
                Push(dir);
                break;

            case ThrowType.Airbone:
                Launch(dir);
                break;
        }
    }

    IEnumerator AirHangRoutine()
    {
        // Wait until reaching top (velocity close to zero)
        while (rb.linearVelocity.y > 0.1f)
        {
            yield return null;
        }

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.useGravity = false;

        Debug.Log("Enemy hanging in air");

        yield return new WaitForSeconds(airHangTime);

        rb.useGravity = true;

        // Optional: make fall faster
        rb.linearVelocity += Vector3.down * fallGravityMultiplier;

        Debug.Log("Enemy falling");
    }

    void Push(Vector3 dir)
    {
        Vector3 force = dir * pushForce;
        force.y = 0;

        rb.AddForce(force, ForceMode.Impulse);
    }

    void Launch(Vector3 dir)
    {
        Vector3 force = dir * pushForce + Vector3.up * airForce;

        rb.AddForce(Vector3.up * airForce, ForceMode.Impulse);

        if (airRoutine != null)
            StopCoroutine(airRoutine);

        airRoutine = StartCoroutine(AirHangRoutine());
    }
}
