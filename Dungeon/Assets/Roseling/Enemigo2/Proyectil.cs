using UnityEngine;

public class Proyectil : MonoBehaviour
{
    public float velocidad = 6f;
    public float tiempoVida = 4f;

    void Start()
    {
        Destroy(gameObject, tiempoVida);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * velocidad * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            other.gameObject.GetComponentInParent<IDamageable>().TakeDamage(10, ThrowType.None, Vector3.zero, 0, false, 0);

            Destroy(gameObject);
        }
    }
}
