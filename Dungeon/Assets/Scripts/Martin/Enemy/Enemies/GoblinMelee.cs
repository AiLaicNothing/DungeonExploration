using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class GoblinMelee : EnemyBase
{
    [Header("Targeting")]
    [SerializeField] private float attackRange = 2.5f;

    [SerializeField] private LayerMask obstacleLayer;

    [Range(0f, 1f)]
    [SerializeField] private float closestTargetChance = 0.6f;

    [SerializeField] private float targetRayHeight = 1.2f;

    // NEW: how close to the target direction the goblin must be before attacking
    [Header("Rotation")]
    [SerializeField] private float facingAngleThreshold = 15f;

    [SerializeField] private float turnSpeed = 15f;

    [Header("OnHitData")]
    [SerializeField] private Vector3 hitBoxSize;
    [SerializeField] private Vector3 hitBoxPos;
    [SerializeField] private float hitTime;
    [SerializeField] private float attackAnimDuration;
    [SerializeField] private float recoveryTime;

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

    private PlayerController currentTarget;

    private bool isPerformingAction;
    private bool instantDetection;
    private bool hasDetectedPlayer;
    private bool isFollowingPlayer;

    private float distPlayer;

    protected override void Awake()
    {
        base.Awake();

        agent.updateRotation = false;
    }

    protected override void Update()
    {
        if (!IsServer) return;

        base.Update();

        if (isStunned || IsStaggered) return;

        UpdateTarget();
        HandleDetection();
        HandleActions();

        if (isPerformingAction) return;

        HandleMovement();
    }

    // ─────────────────────────────────────────────────────────────
    // TARGETING
    // ─────────────────────────────────────────────────────────────

    private void UpdateTarget()
    {
        if (currentTarget != null && !IsTargetValid(currentTarget))
        {
            currentTarget = null;
        }

        if (currentTarget == null)
        {
            currentTarget = SelectTarget();
        }
    }

    /// <summary>
    /// Picks one visible player:
    /// - 60% chance: closest visible player
    /// - 40% chance: one of the other visible players
    /// </summary>
    private PlayerController SelectTarget()
    {
        List<PlayerController> validTargets = GetVisibleTargets();

        if (validTargets.Count == 0) return null;

        validTargets = validTargets.OrderBy(t => Vector3.Distance(transform.position, t.transform.position)).ToList();

        if (validTargets.Count == 1) return validTargets[0];

        if (Random.value <= closestTargetChance)
        {
            return validTargets[0];
        }

        int index = Random.Range(1, validTargets.Count);
        return validTargets[index];
    }

    /// <summary>
    /// Returns all visible players within detection range.
    /// Server only.
    /// </summary>
    private List<PlayerController> GetVisibleTargets()
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        List<PlayerController> validTargets = new List<PlayerController>();

        Vector3 origin = transform.position + Vector3.up * targetRayHeight;

        foreach (PlayerController player in players)
        {
            if (player == null) continue;
            if (!player.gameObject.activeInHierarchy) continue;
            if (player.isDead) continue;

            Vector3 targetPos = player.transform.position + Vector3.up * targetRayHeight;

            float dist = Vector3.Distance(transform.position, player.transform.position);

            if (dist > detectionRange) continue;

            Vector3 dir = (targetPos - origin).normalized;
            float rayDistance = Vector3.Distance(origin, targetPos);

            // If something blocks the view, skip that target
            if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, obstacleLayer))
            {
                if (hit.transform != player.transform && hit.transform.root != player.transform.root)
                {
                    continue;
                }
            }

            validTargets.Add(player);
        }

        return validTargets;
    }

    private bool IsTargetValid(PlayerController target)
    {
        if (target == null) return false;
        if (!target.gameObject.activeInHierarchy) return false;
        if (target.isDead) return false;

        float dist = Vector3.Distance(transform.position, target.transform.position);

        if (dist > detectionRange) return false;

        Vector3 origin = transform.position + Vector3.up * targetRayHeight;
        Vector3 targetPos = target.transform.position + Vector3.up * targetRayHeight;
        Vector3 dir = (targetPos - origin).normalized;
        float rayDistance = Vector3.Distance(origin, targetPos);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, obstacleLayer))
        {
            if (hit.transform != target.transform &&
                hit.transform.root != target.transform.root)
            {
                return false;
            }
        }

        return true;
    }

    // ─────────────────────────────────────────────────────────────
    // DETECTION
    // ─────────────────────────────────────────────────────────────

    private void HandleDetection()
    {
        if (safeZone == null)
            return;

        float distHome = Vector3.Distance(transform.position, safeZone.position);

        if (instantDetection)
        {
            hasDetectedPlayer = true;
            isFollowingPlayer = true;
            return;
        }

        if (currentTarget == null)
        {
            detectionTimer = 0f;
            hasDetectedPlayer = false;
            isFollowingPlayer = false;
            return;
        }

        distPlayer = Vector3.Distance(transform.position, currentTarget.transform.position);

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
            if (distHome > maxChaseDistance)
            {
                hasDetectedPlayer = false;
                isFollowingPlayer = false;
                detectionTimer = 0f;
                currentTarget = null;
                return;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    // MOVEMENT
    // ─────────────────────────────────────────────────────────────

    private void HandleMovement()
    {
        if (isFollowingPlayer && currentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

            if (distance > attackRange)
            {
                // keep chasing while outside attack range
                agent.isStopped = false;
                agent.SetDestination(currentTarget.transform.position);
                RotateToVelocity();
            }
            else
            {
                // inside attack range but not attacking yet:
                // stop moving and face the target
                agent.isStopped = true;
                agent.ResetPath();
                RotateToTarget();
            }
        }
        else
        {
            if (hasPatrol)
            {
                agent.isStopped = false;
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

    // ─────────────────────────────────────────────────────────────
    // ATTACK
    // ─────────────────────────────────────────────────────────────

    private void HandleActions()
    {
        if (isPerformingAction || !hasDetectedPlayer) return;
        if (currentTarget == null) return;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        Debug.Log(distance);

        // NEW: only attack if it is already facing the target enough
        if (distance < attackRange && IsFacingTarget())
        {
            StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        agent.isStopped = true;
        agent.ResetPath();
        isPerformingAction = true;

        // final correction before the attack starts
        FaceTargetInstant();

        // Trigger animation here if needed
        // anim.SetTrigger("Attack");

        yield return new WaitForSeconds(hitTime);

        //  face target again right before hit so the hitbox uses the correct direction
        FaceTargetInstant();

        DoHit();

        float remainingTime = attackAnimDuration - hitTime;

        if (remainingTime > 0) yield return new WaitForSeconds(remainingTime);

        yield return new WaitForSeconds(recoveryTime);

        // pick a new target after attacking
        currentTarget = SelectTarget();

        if (currentTarget == null)
        {
            hasDetectedPlayer = false;
            isFollowingPlayer = false;
        }

        isRevengeMode = false;

        agent.isStopped = false;
        isPerformingAction = false;
    }

    private void DoHit()
    {
        Vector3 attackForward = GetAttackForward();

        Vector3 center = transform.position + attackForward * hitBoxPos.z + Vector3.up * hitBoxPos.y;

        Collider[] hits = Physics.OverlapBox(center, hitBoxSize * 0.5f, Quaternion.LookRotation(attackForward));

        ShowHitbox(center, hitBoxSize, Quaternion.LookRotation(attackForward));

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                IDamageable damageable = hit.GetComponentInParent<IDamageable>();

                if (damageable != null)
                {
                    damageable.TakeDamage(stats.damage, ThrowType.None, attackForward, 0, false, 0, 0);
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    // ROTATION
    // ─────────────────────────────────────────────────────────────

    private void RotateToVelocity()
    {
        Vector3 vel = agent.velocity;
        vel.y = 0;

        if (vel.sqrMagnitude < 0.01f) return;

        Quaternion rot = Quaternion.LookRotation(vel);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * turnSpeed);
    }

    private void RotateToTarget()
    {
        if (currentTarget == null) return;

        Vector3 dir = currentTarget.transform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * turnSpeed);
    }

    // instant face target for the exact attack frame
    private void FaceTargetInstant()
    {
        if (currentTarget == null) return;

        Vector3 dir = currentTarget.transform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    // checks if the enemy is roughly facing the target
    private bool IsFacingTarget()
    {
        if (currentTarget == null) return false;

        Vector3 dir = currentTarget.transform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return false;

        float angle = Vector3.Angle(transform.forward, dir.normalized);
        return angle <= facingAngleThreshold;
    }

    // used by the hitbox to get the current attack direction
    private Vector3 GetAttackForward()
    {
        if (currentTarget != null)
        {
            Vector3 dir = currentTarget.transform.position - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.01f) return dir.normalized;
        }

        Vector3 fallback = transform.forward;
        fallback.y = 0f;

        if (fallback.sqrMagnitude < 0.01f) fallback = Vector3.forward;

        return fallback.normalized;
    }

    // ─────────────────────────────────────────────────────────────
    // TEMP VISUAL
    // ─────────────────────────────────────────────────────────────

    public void ShowHitbox(Vector3 center, Vector3 size, Quaternion rot)
    {
        GameObject box = GameObject.Instantiate(hitBoxPrefab, center, rot);
        box.transform.localScale = size;
        Destroy(box, 0.2f);
    }
}