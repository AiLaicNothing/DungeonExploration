using Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class LockOnTarget : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CinemachineFreeLook freeLook;
    [SerializeField] private PlayerInputHandler input;
    [SerializeField] private CinemachineInputProvider inputProvider;

    [Header("UI")]
    [SerializeField] private Image aimIcon;

    [Header("Settings")]
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private Vector2 targetLockOffset;
    [SerializeField] private float minDistance = 1.5f;
    [SerializeField] private float maxDistance = 15f;
    [SerializeField] private float lockSpeed = 3f;

    public bool isTargeting { get; private set; }

    private Transform currentTarget;
    private float maxAngle = 90f;

    void Start()
    {
        // Disable default Cinemachine input
        freeLook.m_XAxis.m_InputAxisName = "";
        freeLook.m_YAxis.m_InputAxisName = "";
    }

    void Update()
    {
        HandleLockInput();

        if (isTargeting && currentTarget != null)
        {
            UpdateLockCamera();
        }
        else
        {
            // Free look using Input System
            freeLook.m_XAxis.m_InputAxisValue = input.lookInput.x;
            freeLook.m_YAxis.m_InputAxisValue = input.lookInput.y;
        }

        if(aimIcon != null)
        {
            aimIcon.gameObject.SetActive(isTargeting);
        }
    }


    void HandleLockInput()
    {
        if (!input.onLockTarget) return;

        if (isTargeting)
        {
            ClearTarget();
        }
        else
        {
            AssignTarget();
        }
    }

    void UpdateLockCamera()
    {
        //if (!currentTarget)
        //{
        //    ClearTarget();
        //    return;
        //}

        //float distance = Vector3.Distance(transform.position, currentTarget.position);

        //if (distance < minDistance)
        //    return;

        //Vector3 viewPos = mainCamera.WorldToViewportPoint(currentTarget.position);

        //// UI follow
        //if (aimIcon != null)
        //{
        //    aimIcon.transform.position = mainCamera.WorldToScreenPoint(currentTarget.position);
        //}

        //float x = (viewPos.x - 0.5f + targetLockOffset.x) * lockSpeed;
        //float y = (viewPos.y - 0.5f + targetLockOffset.y) * lockSpeed;

        //freeLook.m_XAxis.m_InputAxisValue = x;
        //freeLook.m_YAxis.m_InputAxisValue = y;

        if (!currentTarget) { ClearTarget(); return; }

        // 1. Dirección hacia el objetivo
        Vector3 dirToTarget = currentTarget.position - freeLook.Follow.position;

        // --- EJE X (ROTACIÓN HORIZONTAL) ---
        float targetX = Mathf.Atan2(dirToTarget.x, dirToTarget.z) * Mathf.Rad2Deg;
        freeLook.m_XAxis.Value = Mathf.LerpAngle(freeLook.m_XAxis.Value, targetX, Time.deltaTime * lockSpeed);

        // --- EJE Y (ALTURA / VERTICAL) ---
        // Calculamos el ángulo vertical (pitch)
        float distanceXZ = new Vector2(dirToTarget.x, dirToTarget.z).magnitude;
        float angleY = Mathf.Atan2(dirToTarget.y, distanceXZ) * Mathf.Rad2Deg;

        // Mapeo de ángulo a rango 0-1 del FreeLook
        // Normalmente: -45 grados es arriba (0), 45 grados es abajo (1). 
        // Ajusta estos valores según los límites que tengas en el Inspector de tu FreeLook.
        float minAngle = -40f;
        float maxAngle = 40f;

        // InverseLerp nos da un valor de 0 a 1 basado en el ángulo actual
        // Nota: El eje Y en FreeLook suele estar invertido (0 arriba, 1 abajo)
        float targetYNormalized = Mathf.InverseLerp(maxAngle, minAngle, angleY);

        // Aplicar con suavizado
        freeLook.m_YAxis.Value = Mathf.Lerp(freeLook.m_YAxis.Value, targetYNormalized, Time.deltaTime * lockSpeed);
    }

    void AssignTarget()
    {
        GameObject target = GetClosestTarget();

        if (target != null)
        {
            currentTarget = target.transform;
            isTargeting = true;

            if(inputProvider != null)
            {
                inputProvider.enabled = false;
            }
        }
    }

    void ClearTarget()
    {
        isTargeting = false;
        currentTarget = null;


        if (inputProvider != null)
        {
            inputProvider.enabled = true;
        }
    }

    GameObject GetClosestTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);

        GameObject closest = null;
        float minDist = maxDistance;

        foreach (var enemy in enemies)
        {
            Vector3 dir = enemy.transform.position - transform.position;
            float dist = dir.magnitude;

            if (dist > maxDistance)
                continue;

            float angle = Vector3.Angle(mainCamera.transform.forward, dir.normalized);

            if (angle > maxAngle)
                continue;

            if (dist < minDist)
            {
                closest = enemy;
                minDist = dist;
            }
        }

        return closest;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
}
