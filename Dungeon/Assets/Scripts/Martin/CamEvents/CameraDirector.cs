using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraDirector : MonoBehaviour
{
    public static CameraDirector Instance;

    [Header("Debug")]
    [SerializeField] private bool debug;

    [Header("Gameplay")]
    [SerializeField] private int gameplayPriority = 10;

    private readonly Dictionary<int, CinemachineVirtualCameraBase> cinematicCameras = new();
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

    private void Start()
    {
        RegisterAllCameras();

        if (CameraEventSystem.Instance != null)
            CameraEventSystem.Instance.OnCameraRequest += HandleCameraRequest;
    }

    private void OnDestroy()
    {
        if (CameraEventSystem.Instance != null)
            CameraEventSystem.Instance.OnCameraRequest -= HandleCameraRequest;
    }

    private void RegisterAllCameras()
    {
        CameraID[] cameras = FindObjectsByType<CameraID>(FindObjectsSortMode.None);

        foreach (CameraID cam in cameras)
        {
            if (cam == null || cam.VirtualCamera == null)
                continue;

            if (cinematicCameras.ContainsKey(cam.ID))
            {
                Debug.LogWarning($"Duplicate Camera ID: {cam.ID}");
                continue;
            }

            cinematicCameras.Add(cam.ID, cam.VirtualCamera);

            if (debug)
                Debug.Log($"Registered Camera ID: {cam.ID}");
        }
    }

    public void RegisterPlayerCam(CinemachineFreeLook cam)
    {
        if (cam == null)
            return;

        playerCam = cam;
        playerCam.Priority = gameplayPriority;
    }

    private void HandleCameraRequest(CameraRequest request)
    {
        if (request == null)
            return;

        CinemachineVirtualCameraBase cam = GetCameraByID(request.cameraID);

        if (cam == null)
        {
            Debug.LogError($"[CameraDirector] Camera not found: {request.cameraID}");
            return;
        }

        if (activeRoutine != null)
        {
            if (!request.interruptCurrent)
                return;

            if (currentCinematicCamera != null)
                currentCinematicCamera.Priority = 0;

            RestoreGameplayCamera();
            StopCoroutine(activeRoutine);
        }

        activeRoutine = StartCoroutine(PlayRoutine(cam, request));
    }

    private IEnumerator PlayRoutine(CinemachineVirtualCameraBase cam, CameraRequest request)
    {
        currentCinematicCamera = cam;

        if (request.followTarget != null)
            cam.Follow = request.followTarget;

        if (request.lookAtTarget != null)
            cam.LookAt = request.lookAtTarget;

        if (playerCam != null)
            playerCam.Priority = request.inactivePriority;

        cam.Priority = request.activePriority;

        request.onStarted?.Invoke();

        if (request.duration > 0f)
            yield return new WaitForSeconds(request.duration);

        cam.Priority = request.inactivePriority;

        if (request.restoreGameplayCamera)
            RestoreGameplayCamera();

        request.onFinished?.Invoke();

        currentCinematicCamera = null;
        activeRoutine = null;
    }

    private void RestoreGameplayCamera()
    {
        if (playerCam == null)
            return;

        playerCam.Priority = gameplayPriority;
    }

    private CinemachineVirtualCameraBase GetCameraByID(int id)
    {
        return cinematicCameras.TryGetValue(id, out var cam) ? cam : null;
    }
}
