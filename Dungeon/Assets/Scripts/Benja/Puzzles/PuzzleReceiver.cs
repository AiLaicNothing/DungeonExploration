using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if CINEMACHINE_V3 || CINEMACHINE_3_0_0_OR_NEWER
using Unity.Cinemachine;
#else
using Cinemachine;
#endif

public class PuzzleReceiver : MonoBehaviour
{
    public enum LogicMode { AND, OR }

    [Header("Lógica (Ambos o solo uno)")]
    public LogicMode logicMode = LogicMode.AND;

    [Header("Qué activa")]
    public List<MonoBehaviour> targets;

    [Header("Cámara de Cutaway (opcional)")]
    [Tooltip("Si se asigna, al completarse el puzle la cámara sube su prioridad temporalmente para mostrar el mecanismo.")]
#if CINEMACHINE_V3 || CINEMACHINE_3_0_0_OR_NEWER
    [SerializeField] private CinemachineCamera puzzleCamera;
#else
    [SerializeField] private CinemachineVirtualCameraBase puzzleCamera;
#endif
    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 0;
    [Tooltip("Segundos que la cámara muestra el mecanismo antes de volver a la del jugador.")]
    [SerializeField] private float cameraDuration = 2.5f;
    [Tooltip("Si está activo, el cutaway solo ocurre la primera vez que el puzle se completa.")]
    [SerializeField] private bool cameraTriggerOnlyOnce = true;

    private List<IActivator> _activators = new();
    private bool _currentState = false;
    private bool _cameraHasTriggered = false;
    private Coroutine _cameraRoutine;

    void Awake()
    {
        if (puzzleCamera != null)
            puzzleCamera.Priority = inactivePriority;
    }

    public void RegisterActivator(IActivator activator)
    {
        if (!_activators.Contains(activator))
            _activators.Add(activator);
    }

    public void Evaluate()
    {
        bool shouldBeActive = logicMode switch
        {
            LogicMode.AND => _activators.TrueForAll(a => a.IsActive),
            LogicMode.OR => _activators.Exists(a => a.IsActive),
            _ => false
        };

        if (shouldBeActive == _currentState) return;
        _currentState = shouldBeActive;

        foreach (var target in targets)
        {
            if (target is IActivatable activatable)
            {
                if (_currentState) activatable.Activate();
                else activatable.Deactivate();
            }
        }

        if (_currentState)
            TriggerCameraCutaway();
        else
            CancelCameraCutaway();
    }

    private void TriggerCameraCutaway()
    {
        if (puzzleCamera == null) return;
        if (cameraTriggerOnlyOnce && _cameraHasTriggered) return;

        _cameraHasTriggered = true;

        if (_cameraRoutine != null) StopCoroutine(_cameraRoutine);
        _cameraRoutine = StartCoroutine(CameraRoutine());
    }

    private void CancelCameraCutaway()
    {
        if (_cameraRoutine != null)
        {
            StopCoroutine(_cameraRoutine);
            _cameraRoutine = null;
        }
        if (puzzleCamera != null)
            puzzleCamera.Priority = inactivePriority;
    }

    private IEnumerator CameraRoutine()
    {
        puzzleCamera.Priority = activePriority;
        yield return new WaitForSeconds(cameraDuration);
        puzzleCamera.Priority = inactivePriority;
        _cameraRoutine = null;
    }
}