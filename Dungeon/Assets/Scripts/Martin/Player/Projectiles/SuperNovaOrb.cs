using Unity.Netcode;
using UnityEngine;

public class SuperNovaOrb : NetworkBehaviour
{
    private Transform player;

    private float radius;
    private float angularSpeed;
    private float duration;

    private float angle;
    private float timer;

    private Vector3 orbitForward;
    private Vector3 orbitRight;

    private HitData hitData;

    private float explosionRadius;
    private float explosionDamage;

    private LayerMask enemyLayer;

    public void Initialize(PlayerController player,float r, float speed, float time, float startAngle, HitData data, float explodeRadius, float explodeDamage, LayerMask enemies)
    {
        this.player = player.transform;

        radius = r;
        angularSpeed = speed;
        duration = time;

        angle = startAngle;

        hitData = data;

        explosionRadius = explodeRadius;
        explosionDamage = explodeDamage;
        enemyLayer = enemies;

        orbitForward = player.PlayerModel.forward;
        orbitRight = player.PlayerModel.right;
    }

    void Update()
    {
        if (!IsServer) return;

        if (player == null)
        {
            NetworkObject.Despawn();
            return;
        }

        timer += Time.deltaTime;

        if (timer >= duration)
        {
            Explode();

            NetworkObject.Despawn();

            return;
        }

        Orbit();
    }

    void Orbit()
    {
        angle += angularSpeed * Time.deltaTime;

        float rad = angle * Mathf.Deg2Rad;

        Vector3 offset = orbitForward * Mathf.Cos(rad) * radius + orbitRight * Mathf.Sin(rad) * radius;

        Vector3 targetPos = player.position + offset;

        transform.position = targetPos;

        Vector3 tangent = -orbitForward * Mathf.Sin(rad) + orbitRight * Mathf.Cos(rad);

        if (tangent != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(tangent);
        }
    }

    void Explode()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, enemyLayer);

        foreach (var hit in hits)
        {
            IDamageable dmg = hit.GetComponent<IDamageable>();

            if (dmg != null)
            {
                Vector3 dir = (hit.transform.position - transform.position).normalized;

                dmg.TakeDamage( explosionDamage, hitData.throwType, dir, hitData.stunDuration, hitData.keepInAir, hitData.airLiftForce, hitData.staggerCharge);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Enemy"))
        {
            IDamageable dmg = other.GetComponent<IDamageable>();

            if (dmg != null)
            {
                Vector3 dir = (other.transform.position - transform.position).normalized;

                dmg.TakeDamage( 10f, hitData.throwType,  dir, hitData.stunDuration, hitData.keepInAir, hitData.airLiftForce, hitData.staggerCharge);
            }
        }
    }
}
