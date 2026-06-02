using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class GoblinRange : EnemyBase
{
    [Header("Core")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float moveSpeed = 4.5f;
    [SerializeField] private float attackRange = 8f;

    [Header("Escape / Reposition")]
    [SerializeField] private float escapeRange = 3f;
    [SerializeField] private float repositionRange = 6f;
    [SerializeField] private int tryReposition = 8;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float repathDelay = 0.25f;

    [Header("Targeting")]
    [Range(0f, 1f)]
    [SerializeField] private float closestTargetChance = 0.6f;
    [SerializeField] private float targetRayHeight = 1.2f;
    [SerializeField] private float attackBuffer = 0.75f;

    [Header("Rotation")]
    [SerializeField] private float turnSpeed = 12f;

    [Header("OnHitData")]
    [SerializeField] private float hitTime;
    [SerializeField] private float attackAnimDuration;
    [SerializeField] private float recoveryTime;

    [Header("Detection")]
    [SerializeField] private float detectionRange;
    [SerializeField] private float maxChaseDistance;
    [SerializeField] private float detectionDelay;

    [Header("Patrol")]
    [SerializeField] private Transform safeZone;
    [SerializeField] private bool hasPatrol;
    [SerializeField] private Transform[] patrolZones;
    [SerializeField] private float stopDistance;
    private int patrolIndex = 0;
    private int patrolDir = 1;

    [Header("Temp")]
    public GameObject hitBoxPrefab;

    private PlayerController currentTarget;

    private bool isPerformingAction;
    private bool instantDetection;
    private bool hasDetectedPlayer;
    private bool isFollowingPlayer;

    private bool isEscaping;
    private bool isRepositioning;
    private Vector3 currentMoveDestination;
    private bool hasMoveDestination;

    private float distPlayer;
    private float detectionTimer;
    private float nextRepathTime;

    private Coroutine attackRoutine;

    protected override void Awake()
    {
        base.Awake();

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.speed = moveSpeed;
            agent.stoppingDistance = 0.25f;
            agent.autoBraking = true;
        }
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

    // ==================================================
    // TARGETING
    // ==================================================

    private void UpdateTarget()
    {
        if (currentTarget != null && !IsTargetValid(currentTarget))
        {
            currentTarget = null;
            isEscaping = false;
            isRepositioning = false;
            hasMoveDestination = false;
        }

        if (currentTarget == null)
        {
            currentTarget = SelectTarget();
        }
    }

    private PlayerController SelectTarget()
    {
        List<PlayerController> validTargets = GetVisibleTargets();

        if (validTargets.Count == 0)
            return null;

        validTargets = validTargets
            .OrderBy(t => Vector3.Distance(transform.position, t.transform.position))
            .ToList();

        if (validTargets.Count == 1)
            return validTargets[0];

        if (Random.value <= closestTargetChance)
            return validTargets[0];

        int index = Random.Range(1, validTargets.Count);
        return validTargets[index];
    }

    private List<PlayerController> GetVisibleTargets()
    {
        PlayerController[] players =
            FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        List<PlayerController> validTargets = new List<PlayerController>();
        Vector3 origin = transform.position + Vector3.up * targetRayHeight;

        foreach (PlayerController player in players)
        {
            if (player == null) continue;
            if (!player.gameObject.activeInHierarchy) continue;
            if (player.isDead) continue;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist > detectionRange) continue;

            Vector3 targetPos = player.transform.position + Vector3.up * targetRayHeight;
            Vector3 dir = (targetPos - origin).normalized;
            float rayDistance = Vector3.Distance(origin, targetPos);

            if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, obstacleLayer))
            {
                if (hit.transform != player.transform &&
                    hit.transform.root != player.transform.root)
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

    private bool HasLineOfSightToTarget()
    {
        if (currentTarget == null || firePoint == null) return false;

        Vector3 origin = firePoint.position;
        Vector3 targetPos = currentTarget.transform.position + Vector3.up * targetRayHeight;
        Vector3 dir = (targetPos - origin).normalized;
        float dist = Vector3.Distance(origin, targetPos);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, obstacleLayer))
        {
            if (hit.transform != currentTarget.transform &&
                hit.transform.root != currentTarget.transform.root)
            {
                return false;
            }
        }

        return true;
    }

    // ==================================================
    // DETECTION
    // ==================================================

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
                isEscaping = false;
                isRepositioning = false;
                hasMoveDestination = false;
            }
        }
    }

    // ==================================================
    // MOVEMENT
    // ==================================================

    private void HandleMovement()
    {
        if (isFollowingPlayer && currentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

            if (distance < escapeRange)
            {
                if (!isEscaping || !hasMoveDestination || Time.time >= nextRepathTime)
                {
                    StartEscape();
                }

                agent.isStopped = false;
                agent.SetDestination(currentMoveDestination);
                RotateAwayFromTarget();
                return;
            }

            if (distance > attackRange + attackBuffer)
            {
                isEscaping = false;
                isRepositioning = false;
                hasMoveDestination = false;

                agent.isStopped = false;
                agent.SetDestination(currentTarget.transform.position);
                RotateToTargetSmooth();
                return;
            }

            if (!HasLineOfSightToTarget())
            {
                if (!isRepositioning || !hasMoveDestination || Time.time >= nextRepathTime)
                {
                    StartReposition();
                }

                agent.isStopped = false;
                agent.SetDestination(currentMoveDestination);
                RotateToTargetSmooth();
                return;
            }

            agent.isStopped = true;
            agent.ResetPath();
            RotateToTargetSmooth();
            return;
        }

        if (hasPatrol)
        {
            agent.isStopped = false;
            HandlePatrol();
        }
        else
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private void StartEscape()
    {
        if (currentTarget == null) return;

        Vector3 targetPos = currentTarget.transform.position;
        Vector3 awayDir = (transform.position - targetPos).normalized;

        Vector3 desired = transform.position + awayDir * (escapeRange + repositionRange);

        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, repositionRange, NavMesh.AllAreas))
        {
            currentMoveDestination = hit.position;
        }
        else
        {
            currentMoveDestination = desired;
        }

        hasMoveDestination = true;
        isEscaping = true;
        isRepositioning = false;
        nextRepathTime = Time.time + repathDelay;
    }

    private void StartReposition()
    {
        if (currentTarget == null) return;

        Vector3 center = currentTarget.transform.position;
        currentMoveDestination = FindValidPosAround(center, repositionRange);

        hasMoveDestination = true;
        isRepositioning = true;
        isEscaping = false;
        nextRepathTime = Time.time + repathDelay;
    }

    private Vector3 FindValidPosAround(Vector3 center, float radius)
    {
        Vector3 bestPos = transform.position;
        float bestScore = Mathf.Infinity;

        for (int i = 0; i < tryReposition; i++)
        {
            Vector2 rand = Random.insideUnitCircle.normalized;
            Vector3 candidate = center + new Vector3(rand.x, 0, rand.y) * radius;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                float dist = Mathf.Abs(Vector3.Distance(hit.position, center) - radius);

                if (dist < bestScore)
                {
                    bestScore = dist;
                    bestPos = hit.position;
                }
            }
        }

        return bestPos;
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

    // ==================================================
    // ATTACK
    // ==================================================

    private void HandleActions()
    {
        if (isPerformingAction || !hasDetectedPlayer || currentTarget == null)
            return;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (distance <= attackRange &&
            HasLineOfSightToTarget() &&
            !isEscaping &&
            !isRepositioning &&
            attackRoutine == null)
        {
            attackRoutine = StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        if (currentTarget == null)
        {
            attackRoutine = null;
            yield break;
        }

        agent.isStopped = true;
        agent.ResetPath();
        isPerformingAction = true;

        float elapsed = 0f;

        while (elapsed < hitTime)
        {
            if (currentTarget == null || isEscaping || isRepositioning)
            {
                isPerformingAction = false;
                attackRoutine = null;
                yield break;
            }

            RotateToTargetSmooth();
            elapsed += Time.deltaTime;
            yield return null;
        }

        Shoot();

        float remainingTime = attackAnimDuration - hitTime;
        if (remainingTime > 0f)
            yield return new WaitForSeconds(remainingTime);

        yield return new WaitForSeconds(recoveryTime);

        currentTarget = SelectTarget();

        isEscaping = false;
        isRepositioning = false;
        hasMoveDestination = false;

        if (currentTarget == null)
        {
            hasDetectedPlayer = false;
            isFollowingPlayer = false;
        }

        isPerformingAction = false;
        attackRoutine = null;
        agent.isStopped = false;
    }

    private void Shoot()
    {
        if (currentTarget == null || projectilePrefab == null || firePoint == null)
            return;

        if (!HasLineOfSightToTarget())
            return;

        Vector3 targetPos = currentTarget.transform.position + Vector3.up * targetRayHeight;
        Vector3 dir = (targetPos - firePoint.position).normalized;

        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(dir));

        var projectile = proj.GetComponent<EnemyProyectile>();
        if (projectile != null)
        {
            projectile.InitProj(stats.damage, dir);
        }
    }

    // ==================================================
    // ROTATION
    // ==================================================

    private void RotateToTargetSmooth()
    {
        if (currentTarget == null) return;

        Vector3 dir = currentTarget.transform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * 60f * Time.deltaTime);
    }

    private void RotateAwayFromTarget()
    {
        if (currentTarget == null) return;

        Vector3 dir = transform.position - currentTarget.transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * 60f * Time.deltaTime);
    }
}