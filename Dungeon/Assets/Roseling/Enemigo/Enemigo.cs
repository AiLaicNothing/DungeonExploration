using UnityEngine;

public class Enemigo : MonoBehaviour
{
    public Transform puntoA;
    public Transform puntoB;
    public Transform player;

    public float velocidad = 3f;
    public float rangoDeteccion = 5f;
    public float rangoPersecucion = 8f;

    private Transform objetivoActual;
    private bool persiguiendo = false;

    void Start()
    {
        objetivoActual = puntoA;
    }

    void Update()
    {
        float distanciaPlayer = Vector3.Distance(transform.position, player.position);

        if (distanciaPlayer < rangoDeteccion)
        {
            persiguiendo = true;
        }

        if (distanciaPlayer > rangoPersecucion)
        {
            persiguiendo = false;
        }

        if (persiguiendo)
        {
            //MoverHacia(player.position);
            Vector3 objetivoPlano = new Vector3(player.position.x, transform.position.y, player.position.z);
            MoverHacia(objetivoPlano);
        }
        else
        {
            Patrullar();
        }

        void Patrullar()
        {
            MoverHacia(objetivoActual.position);

            if (Vector3.Distance(transform.position, objetivoActual.position) < 0.2f)
            {
                if (objetivoActual == puntoA)
                {
                    objetivoActual = puntoB;
                }
                else
                {
                    objetivoActual = puntoA;
                }
            }
        }
        void MoverHacia(Vector3 destino)
        {
            transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);

            Vector3 direccion = (destino - transform.position).normalized;
            /*if (direccion != Vector3.zero)
            {
                transform.forward = direccion;
            }*/

            direccion.y = 0;

            if (direccion.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(direccion);
            }
        }
    }
}
