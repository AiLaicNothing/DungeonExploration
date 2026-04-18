using UnityEngine;
using System.Collections;

public class Enemigo : MonoBehaviour
{
    public Transform puntoA;
    public Transform puntoB;
    public Transform player;

    public float velocidad = 3f;
    public float rangoDeteccion = 5f;
    public float rangoPersecucion = 8f;

    public float rangoAtaque = 2.5f;
    public float tiempoCast = 2f;

    public GameObject ataquePrefab;
    public Transform brazo;

    public bool esFantasma = false;

    private bool casteando = false;  
    private bool persiguiendo = false;

    private Transform objetivoActual;

    void Start()
    {
        objetivoActual = puntoA;
    }

    void Update()
    {
        if (casteando) return;

        float distanciaPlayer = Vector3.Distance(transform.position, player.position);

        if (distanciaPlayer <= rangoAtaque)
        {
            StartCoroutine(Castear());
            return;
        }

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

        IEnumerator Castear()
        {
            casteando = true;

            Vector3 mirar = player.position - transform.position;
            mirar.y = 0;

            if (mirar != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(mirar);
            }

            yield return new WaitForSeconds(tiempoCast);

            Atacar();

            yield return new WaitForSeconds(1f);

            casteando = false;
        }

        void Atacar()
        {
            GameObject ataque = Instantiate(ataquePrefab, brazo.position, brazo.rotation);

            if (esFantasma == false)
            {
                Destroy(ataque, 2f);
            }
        }
    }
}
