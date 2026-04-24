using System.Collections;
using UnityEngine;

public class DummyV2 : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public float maxHP = 100f;
    [SerializeField] private float currentHP;

    [Header("Stagger")]
    public bool hasStaggerBar = true;
    public float staggerBar = 100f;
    [SerializeField] private float currentStaggerValue;

    public bool canStaggerMultipleTimes = true;
    public float timeToResetStaggerBar = 5f;

    private float staggerResetTimer;

    [Header("Behavior")]
    public bool canBeAffected = true;

    [Header("Physics (Kinematic)")]
    public Rigidbody rb;
    private Vector3 velocity;
    public float gravity = -20f;
    public Transform groundCheck;
    public bool isGrounded;

    [Header("Forces")]
    public float pushForce = 5f;
    public float airForce = 7f;

    [Header("Air Control")]
    public float airHangTime = 0.4f;
    public float fallGravityMultiplier = 2f;

    Coroutine airRoutine;

    [Header("State")]
    public bool isStunned;
    public bool isStaggered;
    public float staggerDuration = 2f;

    Coroutine stunCoroutine;
    Coroutine staggerCoroutine;

    void Awake()
    {
        currentHP = maxHP;

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void Update()
    {
        if (currentHP <= 0)
            Destroy(gameObject);

        HandleStaggerReset();

    }

    private void FixedUpdate()
    {
        HandleGravity();

        Vector3 move = velocity * Time.fixedDeltaTime;

        //  ABSOLUTE BLOCK: no downward movement if grounded
        if (isGrounded && move.y < 0)
        {
            move.y = 0;
        }

        rb.MovePosition(rb.position + move);
    }

    // =========================
    // DAMAGE
    // =========================
    public void TakeDamage(float amount, ThrowType throwType, Vector3 hitDirection, float stunDuration, bool keepOnAir, float airLift, float staggerBuild)
    {
        currentHP -= amount;

        if (!canBeAffected)
            return;

        BuildStagger(staggerBuild);

        ApplyThrow(throwType, hitDirection);

        if (keepOnAir)
            SustainAir(airLift);

        ApplyStun(stunDuration);
    }

    // =========================
    // STAGGER SYSTEM
    // =========================
    void BuildStagger(float amount)
    {
        if (!hasStaggerBar)
            return;

        currentStaggerValue += amount;
        staggerResetTimer = timeToResetStaggerBar;

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
            currentStaggerValue = staggerBar;

        if (staggerCoroutine != null)
            StopCoroutine(staggerCoroutine);

        staggerCoroutine = StartCoroutine(StaggerRoutine());
    }

    IEnumerator StaggerRoutine()
    {
        isStaggered = true;
        isStunned = true;

        Debug.Log("Enemy STAGGERED");

        yield return new WaitForSeconds(staggerDuration);

        isStaggered = false;
        isStunned = false;

        if (canStaggerMultipleTimes)
            currentStaggerValue = 0;

        Debug.Log("Enemy recovered from stagger");
    }

    void HandleStaggerReset()
    {
        if (!hasStaggerBar || isStaggered)
            return;

        if (currentStaggerValue > 0)
        {
            staggerResetTimer -= Time.deltaTime;

            if (staggerResetTimer <= 0)
            {
                currentStaggerValue = 0;
            }
        }
    }

    // =========================
    // STUN
    // =========================
    void ApplyStun(float duration)
    {
        if (stunCoroutine != null)
            StopCoroutine(stunCoroutine);

        stunCoroutine = StartCoroutine(StunRoutine(duration));
    }

    IEnumerator StunRoutine(float duration)
    {
        isStunned = true;

        yield return new WaitForSeconds(duration);

        if (!isStaggered) // don't override stagger
            isStunned = false;
    }

    // =========================
    // GRAVITY
    // =========================
    void HandleGravity()
    {
        CheckGround();

        if (isGrounded)
        {
            // HARD STOP vertical movement
            if (velocity.y <= 0)
            {
                velocity.y = 0;
            }
        }
        else
        {
            float currentGravity = gravity;

            if (velocity.y < 0)
                currentGravity *= fallGravityMultiplier;

            velocity.y += currentGravity * Time.fixedDeltaTime;
        }
    }

    void CheckGround()
    {
        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, 0.1f);
    }

    // =========================
    // AIR
    // =========================
    void SustainAir(float lift)
    {
        if (velocity.y < 0)
            velocity.y = 0;

        velocity.y += lift;
    }

    IEnumerator AirHangRoutine()
    {
        while (velocity.y > 0.1f)
            yield return null;

        velocity.y = 0;

        yield return new WaitForSeconds(airHangTime);

        velocity.y += gravity * fallGravityMultiplier;
    }

    // =========================
    // THROW LOGIC
    // =========================
    void ApplyThrow(ThrowType type, Vector3 dir)
    {
        //  IMPORTANT RULE
        if (hasStaggerBar && !isStaggered)
            return;

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

    void Push(Vector3 dir)
    {
        Vector3 force = dir * pushForce;
        force.y = 0;

        velocity += force;
    }

    void Launch(Vector3 dir)
    {
        velocity += dir * pushForce;
        velocity.y = airForce;

        if (airRoutine != null)
            StopCoroutine(airRoutine);

        airRoutine = StartCoroutine(AirHangRoutine());
    }
}
