using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyBase : NetworkBehaviour, IDamageable, IKillable
{
    [Header("Stats")]
    [SerializeField] protected EnemyStats stats;
    NetworkVariable<float> currentHp = new NetworkVariable<float>();

    [Header("State")]
    protected bool isStunned;
    protected bool isStaggered;
    // ── AirBone ──────────────────────────────────────────────────────
    protected bool isAirbone;
    protected bool isAirHanging;
    protected float airHangTimer;

    [Header("Stagger")]
    NetworkVariable<float> currentStaggerBuild = new NetworkVariable<float>();
    protected bool isInStaggerCooldown;

    [Header("Components")]
    protected Rigidbody rb;
    protected Animator anim;
    protected NavMeshAgent agent;
    private EnemyHealthBar healthBar;
    private EnemyStaggerBar staggerBar;
    private EnemyBarHolder enemyBarHolder;

    [SerializeField] protected LayerMask whatIsGround;
    [SerializeField] protected Transform groundCheck;
    protected bool isGrounded;

    protected Coroutine stunCourutine;
    protected Coroutine staggerCourutine;
    protected Coroutine airRoutine;

    // ── Visual related ──────────────────────────────────────────────────────
    protected Vector3 combatVelocity;
    protected float verticalVelocity;
    protected bool isInCombatMotion;

    public bool IsStunned => isStunned;
    public bool IsStaggered => isStaggered;

    public event Action<IKillable> OnKilled;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHp.Value = stats.maxHp;
        }

        if (!IsServer)
        {
            if (agent != null)
            {
                agent.enabled = false;
            }
        }

        currentHp.OnValueChanged += OnHpChanged;
    }

    public override void OnNetworkDespawn()
    {
        currentHp.OnValueChanged -= OnHpChanged;
    }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();

        healthBar = GetComponentInChildren<EnemyHealthBar>();
        staggerBar = GetComponentInChildren<EnemyStaggerBar>();
        enemyBarHolder = GetComponentInChildren<EnemyBarHolder>();
    }

    protected virtual void Update()
    {
        if (!IsServer) return;

        CheckGround();
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        HandleCombatMovement();
    }

    public void TakeDamage(float damage, ThrowType throwType, Vector3 hitDir, float stunDuration, bool keepOnAir, float airLift, float staggerBuild)
    {
        if (!IsServer) return;

        currentHp.Value -= isStaggered ? damage * 1.5f : damage;

        if (currentHp.Value <= 0) OnDie();

        ApplyThrow(throwType, hitDir);

        if (keepOnAir) SustainAir(airLift);

        ApplyStun(stunDuration);
        BuildStagger(staggerBuild);
    }

    private void OnHpChanged(float oldHp, float newHp)
    {
        healthBar.UpdateHealthBar(newHp, stats.maxHp);
        staggerBar.UpdateStaggerhBar(currentStaggerBuild.Value, stats.staggerTreshold);

        enemyBarHolder.Show();
    }

    protected void OnDie()
    {
        // Notifica a cualquier sistema suscrito ANTES de destruir el GameObject
        OnKilled?.Invoke(this);
        NetworkObject.Despawn();
    }

    // ── Stagger ──────────────────────────────────────────────────────

    protected void BuildStagger(float ammount)
    {
        if (!stats.hasStagger || isInStaggerCooldown) return;

        if (isStaggered)
        {
            ExtendStagger();
            return;
        }

        currentStaggerBuild.Value += ammount;

        if (currentStaggerBuild.Value >= stats.staggerTreshold)
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

        currentStaggerBuild.Value = 0;
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

    // ── Type of Throw ──────────────────────────────────────────────────────

    protected void ApplyThrow(ThrowType type, Vector3 dir)
    {
        if (stats.hasStagger && !isStaggered) return;

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
    // ── Push ──────────────────────────────────────────────────────
    protected void Push(Vector3 dir)
    {
        DisableAgents();

        isInCombatMotion = true;

        combatVelocity = dir.normalized * stats.pushForce;
    }


    // ── Launch ──────────────────────────────────────────────────────
    protected void Launch(Vector3 dir)
    {
        DisableAgents();

        isInCombatMotion = true;

        isAirbone = true;

        //combatVelocity = dir.normalized * stats.pushForce;

        verticalVelocity = stats.airForce;

        airHangTimer = stats.airHangTime;
    }


    // ── Air Sustain ──────────────────────────────────────────────────────
    protected void SustainAir(float lift)
    {
        if (verticalVelocity < 0)
        {
            verticalVelocity = 0;
        }

        verticalVelocity += lift;
    }

    // ── Client Visual ──────────────────────────────────────────────────────
    private void HandleCombatMovement()
    {
        if (!isInCombatMotion) return;

        transform.position += combatVelocity * Time.fixedDeltaTime;

        // Work like friction
        combatVelocity = Vector3.Lerp(combatVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);

        if (isAirbone)
        {
            //Hang in the air for window combo
            if (verticalVelocity <= 0 && airHangTimer > 0)
            {
                airHangTimer -= Time.fixedDeltaTime;

                verticalVelocity = 0;
            }
            else
            {
                verticalVelocity += Physics.gravity.y * Time.fixedDeltaTime;
            }

            transform.position += Vector3.up * verticalVelocity * Time.fixedDeltaTime;

            if (isGrounded && verticalVelocity <= 0)
            {
                Land();
            }
        }

        if (combatVelocity.sqrMagnitude < 0.01f && !isAirbone)
        {
            StopCombatMotion();
        }
    }

    private void Land()
    {
        isAirbone = false;

        verticalVelocity = 0;

        StopCombatMotion();
    }

    private void StopCombatMotion()
    {
        isInCombatMotion = false;

        combatVelocity = Vector3.zero;

        EnableAgent();
    }

    // ── GroundCheck ──────────────────────────────────────────────────────
    private void CheckGround()
    {
        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, 0.2f, whatIsGround);
    }

    // ── NavMesh ──────────────────────────────────────────────────────
    private void DisableAgents()
    {
        agent.isStopped = true;
        agent.updatePosition = false;
        agent.updateRotation = false;
    }

    private void EnableAgent()
    {
        agent.Warp(transform.position);
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.isStopped = false;
    }
}
