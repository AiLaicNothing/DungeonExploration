using Cinemachine;
using UnityEngine;

/// <summary>
/// Bloquea el movimiento de la cámara Cinemachine cuando hay UI abierta.
///
/// Para Cinemachine 2.x (la versión que usa este proyecto).
///
/// Setup:
///   - Añadir este script al MISMO GameObject que tiene CinemachineFreeLook
///     o CinemachineVirtualCamera con CinemachineInputProvider.
///
/// El script desactiva el CinemachineInputProvider cuando hay UI abierta,
/// lo que detiene la lectura del mouse y por tanto la rotación de cámara.
/// </summary>
public class CameraInputBlocker : MonoBehaviour
{
    private CinemachineInputProvider _inputProvider;
    private bool _wasBlocked;

    void Awake()
    {
        // Buscar el InputProvider en este GameObject o en hijos
        _inputProvider = GetComponent<CinemachineInputProvider>();
        if (_inputProvider == null)
            _inputProvider = GetComponentInChildren<CinemachineInputProvider>();

        if (_inputProvider == null)
            Debug.LogWarning("[CameraInputBlocker] No se encontró CinemachineInputProvider en este GameObject ni en hijos.");
    }

    void Update()
    {
        if (_inputProvider == null) return;

        bool shouldBlock = UIBlockingManager.IsAnyUIOpen;

        if (shouldBlock != _wasBlocked)
        {
            _inputProvider.enabled = !shouldBlock;
            _wasBlocked = shouldBlock;
        }
    }
}