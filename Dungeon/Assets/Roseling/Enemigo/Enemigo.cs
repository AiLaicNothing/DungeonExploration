using UnityEngine;
using System.Collections;

public class Enemigo : MonoBehaviour, IDamageable
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


    [Header("Stats")]
    public float maxHP = 100f;
    [SerializeField] private float currentHP;

    [Header("Behavior")]
    public bool canBeAffected = true;

    [Header("Physics")]
    public Rigidbody rb;

    [Header("Forces")]
    public float pushForce = 5f;
    public float airForce = 7f;

    [Header("Air Control")]
    public float airHangTime = 0.4f;
    public float fallGravityMultiplier = 2f;

    Coroutine airRoutine;

    [Header("State")]
    public bool isStunned;

    Coroutine stunCoroutine;

    void Awake()
    {
        currentHP = maxHP;

        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        objetivoActual = puntoA;
    }

    void Update()
    {
        if (currentHP <= 0)
        {
            Destroy(gameObject);
        }

        if (casteando || isStunned) return;

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

    public void TakeDamage(float amount, ThrowType throwType, Vector3 hitDirection, float stunDuration, bool keepOnAir, float airLift, float StaggerBuild)
    {
        currentHP -= amount;

        if (!canBeAffected)
            return;

        ApplyThrow(throwType, hitDirection);

        if (keepOnAir)
        {
            SustainAir(airLift);
        }

        ApplyStun(stunDuration);
    }

    void ApplyStun(float duration)
    {
        if (stunCoroutine != null)
            StopCoroutine(stunCoroutine);

        stunCoroutine = StartCoroutine(StunRoutine(duration));
    }

    IEnumerator StunRoutine(float duration)
    {
        isStunned = true;

        Debug.Log("Enemy stunned");

        yield return new WaitForSeconds(duration);

        isStunned = false;

        Debug.Log("Enemy recovered from stun");
    }
    void SustainAir(float lift)
    {
        Vector3 vel = rb.linearVelocity;

        if (vel.y < 0)
            vel.y = 0;

        vel.y += lift;

        rb.linearVelocity = vel;

        Debug.Log("Air sustained");
    }

    void ApplyThrow(ThrowType type, Vector3 dir)
    {
        switch (type)
        {
            case ThrowType.Push:
                Push(dir);
                break;

            case ThrowType.Airbone:
                Launch(dir);
                break;
        }
    }

    IEnumerator AirHangRoutine()
    {
        // Wait until reaching top (velocity close to zero)
        while (rb.linearVelocity.y > 0.1f)
        {
            yield return null;
        }

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.useGravity = false;

        Debug.Log("Enemy hanging in air");

        yield return new WaitForSeconds(airHangTime);

        rb.useGravity = true;

        rb.linearVelocity += Vector3.down * fallGravityMultiplier;

        Debug.Log("Enemy falling");
    }

    void Push(Vector3 dir)
    {
        Vector3 force = dir * pushForce;
        force.y = 0;

        rb.AddForce(force * 10, ForceMode.Impulse);
    }

    void Launch(Vector3 dir)
    {
        Vector3 force = dir * pushForce + Vector3.up * airForce;

        rb.AddForce(Vector3.up * airForce * 10, ForceMode.Impulse);

        if (airRoutine != null)
            StopCoroutine(airRoutine);

        airRoutine = StartCoroutine(AirHangRoutine());
    }
}
