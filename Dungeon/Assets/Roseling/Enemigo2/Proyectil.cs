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
}
