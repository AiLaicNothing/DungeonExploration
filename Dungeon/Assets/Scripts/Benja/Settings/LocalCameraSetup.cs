using Cinemachine;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Configura la cámara virtual del Player Prefab para que solo se active
/// si soy el dueño del jugador.
///
/// Para Cinemachine 2.x.
///
/// Setup:
///   - Añadir al Player Prefab root
///   - Asignar 'virtualCamera' al GameObject hijo con la FreeLook/VCam
///   - Si quieres asignar Follow/LookAt en runtime, asigna 'followTarget' y/o 'lookAtTarget'
///     (déjalos vacíos para mantener la configuración del prefab)
/// </summary>
public class LocalCameraSetup : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineVirtualCameraBase virtualCamera;

    [Tooltip("Si está vacío, mantiene el Follow que tenga el prefab.")]
    [SerializeField] private Transform followTarget;

    [Tooltip("Si está vacío, mantiene el LookAt que tenga el prefab.")]
    [SerializeField] private Transform lookAtTarget;

    [Header("Priorities")]
    [SerializeField] private int ownerPriority = 20;
    [SerializeField] private int remotePriority = 0;

    public override void OnNetworkSpawn()
    {
        if (virtualCamera == null)
        {
            Debug.LogError("[LocalCameraSetup] virtualCamera no asignada.");
            return;
        }

        if (IsOwner)
        {
            virtualCamera.Priority = ownerPriority;
            virtualCamera.gameObject.SetActive(true);

            // Solo asignar Follow/LookAt si están especificados en el inspector
            // (si no, mantenemos los que tenga el prefab)
            if (followTarget != null) virtualCamera.Follow = followTarget;
            if (lookAtTarget != null) virtualCamera.LookAt = lookAtTarget;
        }
        else
        {
            virtualCamera.Priority = remotePriority;
            virtualCamera.gameObject.SetActive(false);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (virtualCamera != null)
            virtualCamera.Priority = remotePriority;
    }
}