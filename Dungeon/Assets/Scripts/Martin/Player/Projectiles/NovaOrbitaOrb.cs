using UnityEngine;
using Unity.Netcode;
public class NovaOrbitaOrb : NetworkBehaviour
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

    public void Initialize(PlayerController owner, float r, float speed, float time, float startAngle, HitData data)
    {
        hitData = data;

        player = owner.transform;

        radius = r;
        angularSpeed = speed;
        duration = time;

        angle = startAngle;

        orbitForward = owner.PlayerModel.forward;
        orbitRight = owner.PlayerModel.right;
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

        // tangent direction (for rotation)
        Vector3 tangent = -orbitForward * Mathf.Sin(rad) + orbitRight * Mathf.Cos(rad);

        if (tangent != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(tangent);
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

                dmg.TakeDamage(10f, hitData.throwType, dir, hitData.stunDuration, hitData.keepInAir, hitData.airLiftForce, hitData.staggerCharge);
            }
        }
    }
}