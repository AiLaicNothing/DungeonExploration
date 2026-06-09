using UnityEngine;
using Unity.Netcode;

public class EnemyProjExplosive : NetworkBehaviour
{
    [Header("Projectile")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float radius = 3f;
    [SerializeField] private LayerMask targetLayer;

    [Header("Damage")]
    [SerializeField] private float damage = 10f;

    [Header("Sfx")]
    [SerializeField] private GameObject explosionSfx;

    private Rigidbody rb;
    private bool hasExploded;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        rb = GetComponent<Rigidbody>();
    }

    public void InitProj(float damage, Vector3 dir)
    {
        this.damage = damage;

        if (rb != null)
        {
            rb.linearVelocity = dir.normalized * speed;
        }

        Destroy(gameObject, 3f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
            return;

        if (hasExploded)
            return;

        if (other.CompareTag("Player") || other.CompareTag("Wall") || other.CompareTag("Ground"))
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (hasExploded) return;

        hasExploded = true;

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, targetLayer);

        foreach (Collider hit in hits)
        {
            if (hit == null) continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(damage, ThrowType.None, Vector3.zero, 0f, false, 0f, 0f);
            }
        }

        PlayExplosionSfxClientRpc(transform.position);

        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [ClientRpc]
    private void PlayExplosionSfxClientRpc(Vector3 pos)
    {
        if (explosionSfx == null) return;

        GameObject sfx = Instantiate(explosionSfx, pos, Quaternion.identity);
        Destroy(sfx, 3f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}