using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

#if CINEMACHINE_V3 || CINEMACHINE_3_0_0_OR_NEWER
using Unity.Cinemachine;
#else
using Cinemachine;
#endif

public class PuzzleReceiver : NetworkBehaviour
{
    [Header("Save System")]
    [SerializeField] private string receiverID;

    public string ReceiverID => receiverID;
    public enum LogicMode { AND, OR }

    [Header("Lógica")]
    public LogicMode logicMode = LogicMode.AND;

    [Header("Targets")]
    public List<MonoBehaviour> targets;

    [Header("Cámara")]
#if CINEMACHINE_V3 || CINEMACHINE_3_0_0_OR_NEWER
    [SerializeField] private CinemachineCamera puzzleCamera;
#else
    [SerializeField] private CinemachineVirtualCameraBase puzzleCamera;
#endif

    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 0;
    [SerializeField] private float cameraDuration = 2.5f;
    [SerializeField] private bool cameraTriggerOnlyOnce = true;

    private List<IActivator> _activators = new();

    private bool _currentState = false;
    public bool IsActive => _currentState;
    private bool _cameraHasTriggered = false;
    private Coroutine _cameraRoutine;

    private void Awake()
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
        Debug.Log("[PuzzleReceiver] Evaluate llamado");

        if (!IsServer)
        {
            Debug.Log("[PuzzleReceiver] Ignorado porque no soy servidor");
            return;
        }

        Debug.Log($"[PuzzleReceiver] Activators registrados: {_activators.Count}");

        foreach (var a in _activators)
        {
            Debug.Log($"Activator: {a} | Active = {a.IsActive}");
        }

        bool shouldBeActive = logicMode switch
        {
            LogicMode.AND => _activators.TrueForAll(a => a.IsActive),
            LogicMode.OR => _activators.Exists(a => a.IsActive),
            _ => false
        };

        Debug.Log($"[PuzzleReceiver] shouldBeActive = {shouldBeActive}");

        if (shouldBeActive == _currentState)
        {
            Debug.Log("[PuzzleReceiver] Estado no cambió");
            return;
        }

        _currentState = shouldBeActive;

        Debug.Log($"[PuzzleReceiver] Nuevo estado = {_currentState}");

        foreach (var target in targets)
        {
            Debug.Log($"[PuzzleReceiver] Activando target: {target}");

            if (target is IActivatable activatable)
            {
                if (_currentState)
                {
                    Debug.Log("[PuzzleReceiver] Activate()");
                    activatable.Activate();
                }
                else
                {
                    Debug.Log("[PuzzleReceiver] Deactivate()");
                    activatable.Deactivate();
                }
            }
            else
            {
                Debug.LogWarning($"[PuzzleReceiver] {target} NO implementa IActivatable");
            }
        }
    }

    [ClientRpc]
    private void TriggerCameraCutawayClientRpc()
    {
        if (puzzleCamera == null) return;
        if (cameraTriggerOnlyOnce && _cameraHasTriggered) return;

        _cameraHasTriggered = true;

        if (_cameraRoutine != null)
            StopCoroutine(_cameraRoutine);

        _cameraRoutine = StartCoroutine(CameraRoutine());
    }

    private System.Collections.IEnumerator CameraRoutine()
    {
        puzzleCamera.Priority = activePriority;

        yield return new WaitForSeconds(cameraDuration);

        puzzleCamera.Priority = inactivePriority;

        _cameraRoutine = null;
    }
    /// <summary>
    /// Restauración directa desde save.
    /// SOLO SERVIDOR.
    /// </summary>
    public void SetStateDirectly(bool active)
    {
        if (!IsServer)
            return;

        _currentState = active;

        foreach (var target in targets)
        {
            if (target is IActivatable activatable)
            {
                if (_currentState)
                    activatable.Activate();
                else
                    activatable.Deactivate();
            }
        }

        Debug.Log($"[PuzzleReceiver] Estado restaurado manualmente: {_currentState}");
    }
}