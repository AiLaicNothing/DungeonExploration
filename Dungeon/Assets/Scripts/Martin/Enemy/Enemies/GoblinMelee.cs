using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class GoblinMelee : EnemyBase
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

    [SerializeField] private bool hasPatrol;
    [SerializeField] private Transform safeZone;
    [SerializeField] protected Transform[] patrolZones;

    //TEMPORAL
    public GameObject hitBoxPrefab;

    private bool isPerformingAction;
    private bool isFollowingPlayer;

    private NavMeshAgent agent;

    protected override void Awake()
    {
        base.Awake();

        player = GameObject.FindGameObjectWithTag("Player");
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {

        if (isStunned || IsStaggered) return;

        HandleActions();

        if (isPerformingAction) return;

        HandleMovement();
    }

    private void HandleMovement()
    {
        //This value fuction is that when the enemy get too far from the safe zone or home zone it return to it either to say iddle or patrol
        if(safeZone != null)
        {
            float distance = Vector3.Distance(transform.position, safeZone.position);
        }
    }

    private void HandleActions()
    {
        if (isPerformingAction) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);
        Debug.Log(distance);

        if (distance < attackRange)
        {
            StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        isPerformingAction = true;
        //Trigger animation

        yield return new WaitForSeconds(hitTime);

        DoHit();
        float remainingTime = attackAnimDuration - hitTime;
        if (remainingTime > 0) yield return new WaitForSeconds(remainingTime);

        yield return new WaitForSeconds(recoveryTime);

        isPerformingAction = false;
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

    public void ShowHitbox(Vector3 center, Vector3 size, Quaternion rot)
    {
        GameObject box = GameObject.Instantiate(hitBoxPrefab, center, rot);

        box.transform.localScale = size;

        Destroy(box, 0.2f);
    }
}
