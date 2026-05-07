using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class FirstJudgementSword : NetworkBehaviour
{
    [Header("Fall")]
    public float initialSpeed = 20f;
    public float gravity = -80f;

    [Header("Ground Check")]
    [SerializeField] private float groundOffset = 2f; 
    public float groundCheckDistance = 1.5f;
    public LayerMask groundLayer;

    [Header("Damage")]
    public float radius = 5f;
    public LayerMask enemyLayer;
    public HitData hitData;
    public float damage = 40f;

    [Header("VFX")]
    public GameObject impactSFX;
    public GameObject damageSFX;

    private Rigidbody rb;
    private bool hasLanded;

    private Vector3 velocity;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Initialize(Vector3 target)
    {
        velocity = Vector3.down * initialSpeed;
    }

    void FixedUpdate()
    {
        if (!IsServer) return;

        if (hasLanded) return;

        ApplyMovement();
        CheckGround();
    }

    void ApplyMovement()
    {
        velocity.y += gravity * Time.fixedDeltaTime;

        rb.linearVelocity = velocity;
    }

    void CheckGround()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer))
        {
            // snap to ground
            rb.linearVelocity = Vector3.zero;
            transform.position = hit.point + Vector3.up * groundOffset;

            Land();
        }
    }

    void Land()
    {
        hasLanded = true;

        if (impactSFX != null)
        {
        }

        StartCoroutine(ImpactRoutine());
    }

    private IEnumerator ImpactRoutine()
    {
        yield return new WaitForSeconds(0.05f);

        DealDamage();

        Destroy(gameObject, 0.1f);
    }

    private void DealDamage()
    {
        if (!IsServer) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemyLayer);

        if (damageSFX != null)
        {
        }

        foreach (var hit in hits)
        {
            IDamageable dmg = hit.GetComponent<IDamageable>();

            if (dmg != null)
            {
                Vector3 dir = (hit.transform.position - transform.position).normalized;

                dmg.TakeDamage(damage, hitData.throwType, dir,  hitData.stunDuration, hitData.keepInAir, hitData.airLiftForce, hitData.staggerCharge);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
