using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ReAnimated : EnemyBase
{
    //[SerializeField] private GameObject[] players;
    private GameObject player;
    [SerializeField] private float attackRange;

    [Header("OnHitData")]
    [SerializeField] private Vector3 hitBoxSize;
    [SerializeField] private Vector3 hitBoxPos;
    [SerializeField] private float hitTime;
    [SerializeField] private float attackAnimDuration;
    [SerializeField] private float recoveryTime;
    private Quaternion lockedAttackRot;

    [Header("DecayZone")]
    [SerializeField] private GameObject decaySFX;
    [SerializeField] private float decayHitTime;
    [SerializeField] private float decayCd;
    [SerializeField] private float decayAnimDuration;
    [SerializeField] private float decayRecoveryTime;
    private bool canUseDecay;

    [Header("Decay Zone Stats")]
    [SerializeField] private GameObject decayZonePrefab;
    [SerializeField] private float decayDuration = 5f;
    [SerializeField] private float decayTickRate = 1f;
    [SerializeField] private float decayDamage = 5f;
    [SerializeField] private float decayRadius = 5f;

    [Header("Detection")]
    [SerializeField] private float detectionRange;
    [SerializeField] private float maxChaseDistance;
    [SerializeField] private float detectionDelay;
    private float detectionTimer;

    [Header("Patrol")]
    [SerializeField] private Transform safeZone;
    [SerializeField] private bool hasPatrol;
    [SerializeField] private Transform[] patrolZones;
    [SerializeField] private float stopDistance;
    private int patrolIndex = 0;
    private int patrolDir = 1;

    //TEMPORAL
    public GameObject hitBoxPrefab;

    private bool isPerformingAction;
    private bool instantDetection;
    private bool hasDetectedPlayer;
    private bool isFollowingPlayer;

    private float distPlayer;

    protected override void Awake()
    {
        base.Awake();

        AssignActivePlayer();

        agent.updateRotation = false;

    }
    protected override void Update()
    {
        if (!IsServer) return;

        base.Update();

        if (player == null || !player.activeInHierarchy)
        {
            AssignActivePlayer();
        }

        if (isStunned || IsStaggered) return;

        HandleDetection();
        HandleActions();

        if (isPerformingAction) return;

        HandleMovement();
    }

    private void AssignActivePlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject p in players)
        {
            if (p.activeInHierarchy)
            {
                player = p;
                return;
            }
        }

        player = null; // fallback if none found
    }

    private void HandleDetection()
    {
        if (player == null) return;

        distPlayer = Vector3.Distance(transform.position, player.transform.position);
        float distHome = Vector3.Distance(transform.position, safeZone.transform.position);

        if (instantDetection)
        {
            hasDetectedPlayer = true;
            isFollowingPlayer = true;
            return;
        }

        if (!hasDetectedPlayer)
        {
            if (distPlayer <= detectionRange)
            {
                detectionTimer += Time.deltaTime;

                if (detectionTimer >= detectionDelay)
                {
                    hasDetectedPlayer = true;
                    isFollowingPlayer = true;
                }
            }
            else
            {
                detectionTimer = 0f;
            }
        }
        else
        {
            //--> If to far from "home" return to home
            if (distHome > maxChaseDistance)
            {
                hasDetectedPlayer = false;
                isFollowingPlayer = false;
                detectionTimer = 0f;
                return;
            }
        }
    }

    private void HandleMovement()
    {
        if (isFollowingPlayer)
        {
            agent.SetDestination(player.transform.position);

            //-->Rotate the player depending of distance if its not close, it rotate toward it destination else toward the player
            if (distPlayer > attackRange * 2f)
            {
                RotateToVelocity();
            }
            else
            {
                RotateToPlayer();
            }
        }
        else
        {
            if (hasPatrol)
            {
                HandlePatrol();
                RotateToVelocity();
            }
            else
            {
                agent.ResetPath();
            }
        }
    }

    private void HandlePatrol()
    {
        if (patrolZones == null || patrolZones.Length == 0) return;

        Transform posDesired = patrolZones[patrolIndex];

        agent.SetDestination(posDesired.position);

        float dist = Vector3.Distance(transform.position, posDesired.position);

        if (dist <= stopDistance)
        {
            patrolIndex += patrolDir;

            if (patrolIndex >= patrolZones.Length)
            {
                patrolIndex = patrolZones.Length - 2;
                patrolDir = -1;
            }
            else if (patrolIndex < 0)
            {
                patrolIndex = 1;
                patrolDir = 1;
            }
        }
    }

    private void HandleActions()
    {
        if (isPerformingAction || !hasDetectedPlayer) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);
        Debug.Log(distance);

        float hpPercent = CurrentHp / stats.maxHp;

        if (hpPercent <= 0.75f)
        {
            float rng = Random.value;

            if (rng <= 0.3)
            {
                StartCoroutine(PerformDecayZone());
            }
        }

        if (distance < attackRange)
        {
            StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        agent.isStopped = true;
        isPerformingAction = true;

        //Trigger animation

        yield return new WaitForSeconds(hitTime);

        DoHit();
        float remainingTime = attackAnimDuration - hitTime;
        if (remainingTime > 0) yield return new WaitForSeconds(remainingTime);

        yield return new WaitForSeconds(recoveryTime);

        agent.isStopped = false;
        isPerformingAction = false;
    }

    private IEnumerator PerformDecayZone()
    {
        agent.isStopped = true;
        isPerformingAction = true;

        canUseDecay = false;

        yield return new WaitForSeconds(decayHitTime);

        Vector3 targetPos = player.transform.position;

        if (Physics.Raycast(targetPos + Vector3.up * 10, Vector3.down, out RaycastHit hit, 30f))
        {
            targetPos = hit.point;
        }

        GameObject zone = Instantiate(decayZonePrefab, targetPos, Quaternion.identity);
        zone.GetComponent<NetworkObject>().Spawn();

        DecayZone decay = zone.GetComponent<DecayZone>();

        if (decay != null)
        {
            decay.Initialize(decayDamage, decayDuration, decayTickRate, decayRadius);
        }

        if (decaySFX != null)
        {
            Instantiate(decaySFX, targetPos,Quaternion.identity);
        }

        float remaining = decayAnimDuration - decayHitTime;

        if (remaining > 0) yield return new WaitForSeconds(remaining);

        agent.isStopped = false;
        isPerformingAction = false;

        yield return new WaitForSeconds(decayCd);

        canUseDecay = true;
    }

    private void DoHit()
    {
        Vector3 center = transform.position + transform.forward * hitBoxPos.z + transform.up * hitBoxPos.y;

        Collider[] hits = Physics.OverlapBox(center, hitBoxSize * 0.5f, transform.rotation);

        ShowHitbox(center, hitBoxSize, transform.rotation);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                IDamageable damageable = hit.GetComponentInParent<IDamageable>();

                damageable.TakeDamage(stats.damage, ThrowType.None, transform.forward, 0, false, 0, 0);
            }
        }
    }

    private void RotateToVelocity()
    {
        Vector3 vel = agent.velocity;
        vel.y = 0;

        if (vel.sqrMagnitude < 0.01f) return;

        Quaternion rot = Quaternion.LookRotation(vel);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 10f);
    }

    private void RotateToPlayer()
    {
        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 15f);
    }

    //=========TEMPORAL===========

    public void ShowHitbox(Vector3 center, Vector3 size, Quaternion rot)
    {
        GameObject box = GameObject.Instantiate(hitBoxPrefab, center, rot);

        box.transform.localScale = size;

        Destroy(box, 0.2f);
    }
}
