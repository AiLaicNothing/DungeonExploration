using System;
using UnityEngine;

public class CameraEventSystem : MonoBehaviour
{
    public static CameraEventSystem Instance;

    public event Action<CameraRequest> OnCameraRequest;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        } 

        Instance = this;
    }

    public void Play(CameraRequest request)
    {
        if (request == null)
            return;

        if (string.IsNullOrEmpty(request.cameraID))
        {
            Debug.LogError("[CameraEventSystem] Missing camera ID");
            return;
        }

        OnCameraRequest?.Invoke(request);
    }
}

