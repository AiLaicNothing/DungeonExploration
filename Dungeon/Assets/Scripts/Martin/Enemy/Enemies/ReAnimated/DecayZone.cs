using Unity.Netcode;
using UnityEngine;

public class DecayZone : NetworkBehaviour
{
    private float radius = 5f;
    private float duration = 5f;
    private float tickRate = 1f;
    private float damage = 10f;

    private float timer;
    private float tickTimer;

    public void Initialize(float dmg, float lifeTime, float tick, float radius)
    {
        this.radius = radius;
        damage = dmg;
        duration = lifeTime;
        tickRate = tick;
    }

    private void Update()
    {
        if (!IsServer) return;

        timer += Time.deltaTime;
        tickTimer += Time.deltaTime;

        if (tickTimer >= tickRate)
        {
            tickTimer = 0f;
            DealDamage();
        }

        if (timer >= duration)
        {
            NetworkObject.Despawn();
        }
    }

    private void DealDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            IDamageable dmg = hit.GetComponentInParent<IDamageable>();

            if (dmg != null)
            {
                Vector3 dir = (hit.transform.position - transform.position).normalized;

                dmg.TakeDamage( damage, ThrowType.None, dir, 0,false, 0, 0);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
