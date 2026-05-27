using Cinemachine;
using System.Collections;
using UnityEngine;

public class CameraDirector : MonoBehaviour
{
    public static CameraDirector Instance;

    [Header("Debug")]
    [SerializeField] private bool debug;

    [Header("Gameplay")]
    [SerializeField] private int gameplayPriority = 100;

    [Header("Cameras")]
    [SerializeField] private CinemachineVirtualCameraBase[] cinematicCameras;

    private CinemachineFreeLook playerCam;

    private CinemachineVirtualCameraBase currentCinematicCamera;

    private Coroutine activeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        if (CameraEventSystem.Instance != null)
        {
            CameraEventSystem.Instance.OnCameraRequest += HandleCameraRequest;
        }
    }

    private void OnDisable()
    {
        if (CameraEventSystem.Instance != null)
        {
            CameraEventSystem.Instance.OnCameraRequest -= HandleCameraRequest;
        }
    }

    public void RegisterPlayerCam(CinemachineFreeLook cam)
    {
        if (cam == null) return;

        playerCam = cam;
        playerCam.Priority = gameplayPriority;

        if (debug)
        {
            Debug.Log("[CameraDirector] Gameplay camera registered");
        }
    }

    private void HandleCameraRequest(CameraRequest request)
    {
        if (request == null) return;

        CinemachineVirtualCameraBase cam = GetCameraByID(request.cameraID);

        if (cam == null)
        {
            Debug.LogError($"[CameraDirector] Camera not found: {request.cameraID}");
            return;
        }

        if (activeRoutine != null)
        {
            if (!request.interruptCurrent)return;

            StopCoroutine(activeRoutine);
        }

        activeRoutine = StartCoroutine(PlayRoutine(cam, request));
    }

    private IEnumerator PlayRoutine(CinemachineVirtualCameraBase cam, CameraRequest request)
    {
        currentCinematicCamera = cam;

        if (debug)
        {
            Debug.Log($"[CameraDirector] Playing Camera: {cam.name}");
        }

        // FOLLOW / LOOKAT
        if (request.followTarget != null)
        {
            cam.Follow = request.followTarget;
        }

        if (request.lookAtTarget != null)
        {
            cam.LookAt = request.lookAtTarget;
        }

        // PRIORITIES
        if (playerCam != null)
        {
            playerCam.Priority = request.inactivePriority;
        }

        cam.Priority = request.activePriority;

        request.onStarted?.Invoke();

        if (request.duration > 0f)
        {
            yield return new WaitForSeconds(request.duration);
        }

        cam.Priority = request.inactivePriority;

        if (request.restoreGameplayCamera)
        {
            RestoreGameplayCamera();
        }

        request.onFinished?.Invoke();

        currentCinematicCamera = null;
        activeRoutine = null;
    }

    private void RestoreGameplayCamera()
    {
        if (playerCam == null) return;

        playerCam.Priority = gameplayPriority;

        if (debug)
        {
            Debug.Log("[CameraDirector] Restored gameplay camera");
        }
    }

    private CinemachineVirtualCameraBase GetCameraByID(string id)
    {
        foreach (var cam in cinematicCameras)
        {
            if (cam == null)  continue;

            if (cam.name == id) return cam;
        }

        return null;
    }
}
