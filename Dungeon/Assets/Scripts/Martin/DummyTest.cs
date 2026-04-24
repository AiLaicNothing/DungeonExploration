using System.Collections;
using UnityEngine;

public class DummyTest : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public float maxHP = 100f;
    [SerializeField] private float currentHP;

    [Header("Stagger")]
    public bool hasStagger = true;
    public float staggerBar = 100f;
    [SerializeField] private float currentStaggerValue;
    [SerializeField] private float staggerDuration;
    [SerializeField] private float timeResetStagger = 5f;
    private float staggerResetTimer;
    [SerializeField] private bool canStaggerMultipleTimes = true;
    private bool isInStaggerCooldown;

    [Header("Behavior")]
    public bool canBeAffected = true;

    [Header("Components")]
    public Rigidbody rb;

    [Header("Forces")]
    public float pushForce = 5f;
    public float airForce = 7f;

    [Header("Air Control")]
    public float airHangTime = 0.4f;
    public float fallGravityMultiplier = 2f;


    [Header("States")]
    [SerializeField] private bool isStunned;
    [SerializeField] private bool isStaggered;

    //-->Courutinas
    Coroutine airRoutine;
    Coroutine stunCoroutine;
    Coroutine staggerCoroutine;

    void Awake()
    {
        currentHP = maxHP;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    public void Update()
    {
        if(currentHP <= 0)
        {
            Destroy(gameObject);
        }
    }

    public void TakeDamage(float amount, ThrowType throwType, Vector3 hitDirection, float stunDuration, bool keepOnAir, float airLift, float StaggerBuild)
    {
        //--> When staggered increase damage
        if (isStaggered)
        {
            currentHP -= amount * 1.5f;

        }

        //--> CHECK if it CAN BE STUN
        if (!canBeAffected)
        {
            return;
        }

        ApplyThrow(throwType, hitDirection);

        if (keepOnAir)
        {
            SustainAir(airLift);
        }

        ApplyStun(stunDuration);

        BuildStagger(StaggerBuild);
    }

    // === STAGGER SECTION ===
    void BuildStagger(float amount)
    {
        if (!hasStagger || isInStaggerCooldown)
        {
            return;
        }

        if (isStaggered)
        {
            ExtendStagger();
            return;
        }

        currentStaggerValue += amount;

        if (currentStaggerValue >= staggerBar)
        {
            TriggerStagger();
        }
    }

    void TriggerStagger()
    {
        if (isStaggered)
            return;

        if (!canStaggerMultipleTimes && currentStaggerValue >= staggerBar)
        {
            currentStaggerValue = staggerBar;
        }

        if (staggerCoroutine != null)
        {
            StopCoroutine(staggerCoroutine);
        }

        staggerCoroutine = StartCoroutine(StaggerRoutine());
    }
    void ExtendStagger()
    {
        if (staggerCoroutine != null)
        {
            StopCoroutine(staggerCoroutine);
        }

        staggerCoroutine = StartCoroutine(StaggerRoutine());
    }

    IEnumerator StaggerRoutine()
    {
        isStaggered = true;
        isStunned = true;

        Debug.Log("Enemy STAGGERED");

        yield return new WaitForSeconds(staggerDuration);

        isStaggered = false;

        if(stunCoroutine == null)
        {
            isStunned = false;
        }

        isInStaggerCooldown = true;

        Debug.Log("Enemy recovered from stagger");

        yield return new WaitForSeconds(timeResetStagger);

        currentStaggerValue = 0;

        isInStaggerCooldown = false;

        staggerCoroutine = null;
    }

    void ApplyStun(float duration)
    {
        //Evade extending the stun when staggered
        if (isStaggered)
        {
            return;
        }

        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
        }

        stunCoroutine = StartCoroutine(StunRoutine(duration));
    }

    IEnumerator StunRoutine(float duration)
    {
        isStunned = true;

        Debug.Log("Enemy stunned");

        yield return new WaitForSeconds(duration);

        //--> Evade stun cancelling stagger
        if (!isStaggered)
        {
            isStunned = false;
        }

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

        if(hasStagger && !isStaggered)
        {
            return;
        }

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

        rb.linearVelocity += Vector3.down * fallGravityMultiplier;

        Debug.Log("Enemy falling");
    }

    void Push(Vector3 dir)
    {
        Vector3 force = dir * pushForce;
        force.y = 0;

        rb.AddForce(force * 10, ForceMode.Impulse);
    }

    void Launch(Vector3 dir)
    {
        Vector3 force = dir * pushForce + Vector3.up * airForce;

        rb.AddForce(Vector3.up * airForce * 10, ForceMode.Impulse);

        if (airRoutine != null)
            StopCoroutine(airRoutine);

        airRoutine = StartCoroutine(AirHangRoutine());
    }
}
