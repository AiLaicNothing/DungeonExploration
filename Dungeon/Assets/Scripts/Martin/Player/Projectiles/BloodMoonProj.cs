using System.Collections;
using UnityEngine;

public class BloodMoonProj : MonoBehaviour
{
    [Header("Movement")]
    public float forwardSpeed = 15f;

    [Header("Targeting")]
    public float maxRange = 25f;
    public float spawnHeight = 20f;
    public float fallSpeed = 35f;

    private Rigidbody rb;

    [Header("Ground Check")]
    public float groundCheckDistance = 1f;
    public LayerMask groundLayer;

    [Header("After Impact")]
    public float duration = 5f;
    public float tickRate = 0.5f;
    public float radius = 5f;

    public LayerMask enemyLayer;
    private HitData hitData;

    private bool hasLanded;
    private float timer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Initialize(PlayerController player, HitData data)
    {
        hitData = data;

        Vector3 targetPoint = player.GetAimPoint(maxRange, groundLayer);

        Vector3 dir = targetPoint - player.transform.position;
        float dist = dir.magnitude;

        if (dist > maxRange)
        {
            dir = dir.normalized * maxRange;
            targetPoint = player.transform.position + dir;
        }

        if (!Physics.Raycast(targetPoint + Vector3.up * 5f, Vector3.down, out RaycastHit groundHit, 20f, groundLayer))
        {
            targetPoint = player.transform.position + player.PlayerModel.forward * maxRange;
        }
        else
        {
            targetPoint = groundHit.point;
        }

        transform.position = targetPoint + Vector3.up * spawnHeight;

        rb.linearVelocity = Vector3.down * fallSpeed;
    }

    void FixedUpdate()
    {
        if (hasLanded) return;

        RotateToVelocity();
        CheckGround();
    }

    void RotateToVelocity()
    {
        if (rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
        }
    }

    void CheckGround()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer))
        {
            transform.position = hit.point;
            Land();
        }
    }

    void Land()
    {
        hasLanded = true;

        // stop movement
        rb.linearVelocity = Vector3.zero;
        rb.useGravity = false;

        StartCoroutine(DamageWaves());
    }

    IEnumerator DamageWaves()
    {
        timer = 0f;

        while (timer < duration)
        {
            DealDamage();

            yield return new WaitForSeconds(tickRate);

            timer += tickRate;
        }

        Destroy(gameObject);
    }

    void DealDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemyLayer);

        foreach (var hit in hits)
        {
            IDamageable dmg = hit.GetComponent<IDamageable>();

            if (dmg != null)
            {
                Vector3 dir = (hit.transform.position - transform.position).normalized;

                dmg.TakeDamage(20, hitData.throwType, dir, hitData.stunDuration, hitData.keepInAir, hitData.airLiftForce, hitData.staggerCharge);
            }
        }
    }
}
