using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class GoblinRange : EnemyBase
{
    [Header("Core")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float attackRange;
    [SerializeField] private float escapeRange; //--> If player to close, run away
    [SerializeField] private float repositionRange;
    [SerializeField] private int tryReposition;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("OnHitData")]
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

    private bool isPerformingAction;
    private bool instantDetection;
    private bool hasDetectedPlayer;
    private bool isFollowingPlayer;

    private float distPlayer;

    //===TEMPORAL===
    private GameObject player;

    protected override void Awake()
    {
        base.Awake();

        player = GameObject.FindGameObjectWithTag("Player");

        agent.updateRotation = false;
    }
    protected override void Update()
    {
        base.Update();

        if (isStunned || IsStaggered) return;

        HandleDetection();
        HandleActions();

        if (isPerformingAction) return;

        HandleMovement();
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
            Vector3 targetPos = GetBestPos();

            agent.SetDestination(targetPos);

            if (!isPerformingAction)
            {
                RotateToVelocity();
            }
        }
        else
        {
            if (hasPatrol)
            {
                HandlePatrol();

                if (!isPerformingAction)
                {
                    RotateToVelocity();
                }
            }
            else
            {
                agent.ResetPath();
            }
        }
    }

    private Vector3 GetBestPos()
    {
        Vector3 playerPos = player.transform.position;

        if (distPlayer < escapeRange)
        {
            return FindValidPosAway(playerPos);
        }

        if (distPlayer > attackRange)
        {
            return FindValidPosAround(playerPos, attackRange * 0.9f);
        }

        return FindValidPosAround(playerPos, attackRange);
    }

    private Vector3 FindValidPosAway(Vector3 targetPos)
    {
        Vector3 bestPos = transform.position;
        float bestScore = -Mathf.Infinity;

        for (int i = 0; i < tryReposition; i++)
        {
            Vector2 rand = Random.insideUnitCircle.normalized;
            Vector3 dir = new Vector3(rand.x, 0, rand.y);

            Vector3 candidate = transform.position + dir * repositionRange;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                float dist = Vector3.Distance(hit.position, targetPos);

                if (dist > bestScore)
                {
                    bestScore = dist;
                    bestPos = hit.position;
                }
            }
        }

        return bestPos;
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

    private void HandleActions()
    {
        if (isPerformingAction || !hasDetectedPlayer) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);
        Debug.Log(distance);

        if (distance < attackRange && distance > escapeRange)
        {
            Vector3 dir = (player.transform.position - firePoint.position).normalized;

            if (HasLineOFSight(dir))
            {
                StartCoroutine(PerformAttack());
            }
            else
            {
                agent.SetDestination(FindValidPosAround(player.transform.position, attackRange));
            }
        }
    }

    private IEnumerator PerformAttack()
    {
        agent.isStopped = true;
        isPerformingAction = true;

        RotateToPlayer();

        //Trigger animation

        yield return new WaitForSeconds(hitTime);

        Shoot();

        float remainingTime = attackAnimDuration - hitTime;
        if (remainingTime > 0) yield return new WaitForSeconds(remainingTime);

        yield return new WaitForSeconds(recoveryTime);

        agent.isStopped = false;
        isPerformingAction = false;
    }

    private void Shoot()
    {
        Vector3 dir = (player.transform.position - firePoint.position).normalized;

        if (!HasLineOFSight(dir)) return;

        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(dir));


        var projectile = proj.GetComponent<EnemyProyectile>();

        if (projectile != null)
        {
            projectile.InitProj(stats.damage, dir);
        }
    }

    private bool HasLineOFSight(Vector3 dir)
    {
        float dist = Vector3.Distance(firePoint.position, player.transform.position);

        if (Physics.Raycast(firePoint.position, dir, out RaycastHit hit, dist, obstacleLayer))
        {
            if (!hit.collider.CompareTag("Player")) return false;
        }

        return true;
    }

    private void RotateToVelocity()
    {
        Vector3 vel = agent.velocity;
        vel.y = 0;

        if (vel.sqrMagnitude < 0.05f) return;

        Quaternion rot = Quaternion.LookRotation(vel);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 10f);
    }

    private void RotateToPlayer()
    {
        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        transform.rotation = Quaternion.LookRotation(dir);
    }
}
