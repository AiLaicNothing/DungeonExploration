using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] protected EnemyStats stats;
    protected float currentHp;

    [Header("State")]
    protected bool isStunned;
    protected bool isStaggered;

    [Header("Stagger")]
    protected float currentStaggerBuild;
    protected bool isInStaggerCooldown;

    [Header("Components")]
    protected Rigidbody rb;
    protected Animator anim;
    protected NavMeshAgent agent;

    [SerializeField] protected LayerMask whatIsGround;
    [SerializeField] protected Transform groundCheck;
    protected bool isGrounded;

    protected Coroutine stunCourutine;
    protected Coroutine staggerCourutine;
    protected Coroutine airRoutine;



    public bool IsStunned => isStunned;
    public bool IsStaggered => isStaggered;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();

        currentHp = stats.maxHp;
    }

    protected virtual void Update()
    {
        CheckGround();
    }

    public void TakeDamage(float damage, ThrowType throwType, Vector3 hitDir, float stunDuration, bool keepOnAir, float airLift, float staggerBuild)
    {
        currentHp -= isStaggered ? damage * 1.5f : damage;

        if (currentHp <= 0) OnDie();

        ApplyThrow(throwType, hitDir);

        if (keepOnAir) SustainAir(airLift);

        ApplyStun(stunDuration);
        BuildStagger(staggerBuild);
    }

    protected void OnDie()
    {
        Destroy(gameObject);
    }

    //=====STAGGER RELATED======

    protected void BuildStagger(float ammount)
    {
        if (!stats.hasStagger || isInStaggerCooldown) return;

        if (isStaggered)
        {
            ExtendStagger();
            return;
        }

        currentStaggerBuild += ammount;

        if (currentStaggerBuild >= stats.staggerTreshold)
        {
            TriggerStagger();
        }
    }

    protected void TriggerStagger()
    {
        if (isStaggered) return;

        if (staggerCourutine != null) StopCoroutine(staggerCourutine);

        staggerCourutine = StartCoroutine(StaggerRoutine());
    }

    protected void ExtendStagger()
    {
        if (staggerCourutine != null) StopCoroutine(staggerCourutine);

        staggerCourutine = StartCoroutine(StaggerRoutine());
    }

    protected IEnumerator StaggerRoutine()
    {
        isStaggered = true;
        isStunned = true;

        yield return new WaitForSeconds(stats.staggerDuration);

        isStaggered = false;

        if (stunCourutine == null) isStunned = false;

        isInStaggerCooldown = true;

        yield return new WaitForSeconds(stats.timeResetStagger);

        currentStaggerBuild = 0;
        isInStaggerCooldown = false;

        staggerCourutine = null;
    }

    //=====STUN RELATED======

    protected void ApplyStun(float duration)
    {
        if (isStaggered) return;

        if (stunCourutine != null)
        {
            StopCoroutine(stunCourutine);
            stunCourutine = null;
        }

        stunCourutine = StartCoroutine(StunRoutine(duration));
    }

    protected IEnumerator StunRoutine(float duration)
    {
        isStunned = true;

        yield return new WaitForSeconds(duration);

        stunCourutine = null;

        if (!isStaggered) isStunned = false;
    }

    //====PHYSIC RELATED=====

    protected void ApplyThrow(ThrowType type, Vector3 dir)
    {
        if (stats.hasStagger && !isStaggered) return;

        agent.isStopped = true;
        agent.updatePosition = false;
        agent.updateRotation = false;

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

    protected void Push(Vector3 dir)
    {
        Vector3 force = dir * stats.pushForce;
        force.y = 0;

        rb.AddForce(force * 10, ForceMode.Impulse);
    }

    protected void Launch(Vector3 dir)
    {
        rb.AddForce(Vector3.up * stats.airForce * 10, ForceMode.Impulse);

        if (airRoutine != null)  StopCoroutine(airRoutine);

        airRoutine = StartCoroutine(AirHangRoutine());
    }

    protected void SustainAir(float lift)
    {
        Vector3 vel = rb.linearVelocity;

        if (vel.y < 0) vel.y = 0;

        vel.y += lift;
        rb.linearVelocity = vel;
    }

    protected IEnumerator AirHangRoutine()
    {
        while (rb.linearVelocity.y > 0.1f) yield return null;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.useGravity = false;

        yield return new WaitForSeconds(stats.airHangTime);

        rb.useGravity = true;
        rb.linearVelocity += Vector3.down * stats.fallGravityMultiplier;

        while(!isGrounded) yield return null;

        yield return new WaitForSeconds(0.05f);

        agent.Warp(transform.position);
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.isStopped = false;
    }

    private void CheckGround()
    {
        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, 0.2f, whatIsGround);
    }
}
