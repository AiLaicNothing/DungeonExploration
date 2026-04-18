using UnityEngine;

public class Enemigo2 : MonoBehaviour
{
    public float altura = 0.5f;
    public float velocidad = 2f;

    private float yInicial;

    void Start()
    {
        yInicial = transform.position.y;
    }

    void Update()
    {
        Vector3 pos = transform.position;

        pos.y = yInicial + Mathf.Sin(Time.time * velocidad) * altura;

        transform.position = pos;
    }
}
