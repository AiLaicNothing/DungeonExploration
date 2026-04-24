using UnityEngine;

public class PlayerProyectile : MonoBehaviour
{
    private float damage;
    private HitData hitData;
    private Vector3 direction;
    private float speed;

    private Rigidbody rb;

    public void Initialize(float dmg, HitData data, Vector3 dir, float spd)
    {
        damage = dmg;
        hitData = data;
        direction = dir.normalized;
        speed = spd;

        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        // Auto destroy after some time
        Destroy(gameObject, 3f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy")) 
        {
            IDamageable target = other.GetComponent<IDamageable>();

            if (target != null)
            {
                target.TakeDamage(damage * hitData.damageMultiplier, hitData.throwType, direction, hitData.stunDuration, hitData.keepInAir, hitData.airLiftForce, hitData.staggerCharge);
            }

            Destroy(gameObject);
        }
    }
}
