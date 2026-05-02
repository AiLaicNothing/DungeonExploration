using UnityEngine;

public class EnemyProyectile : MonoBehaviour
{
    [SerializeField] private float speed;
    private float damage;
    private Vector3 dir;

    private Rigidbody rb;


    public void InitProj(float damage,Vector3 dir)
    {
        this.damage = damage;
        this.dir = dir;

        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = dir * speed;
        }

        Destroy(gameObject, 3f);
    }
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        IDamageable target = other.GetComponentInParent<IDamageable>();

        if (target == null) return;

        target.TakeDamage(damage, ThrowType.None, dir, 0f, false, 0, 0);

        Destroy(gameObject);
    }
}
