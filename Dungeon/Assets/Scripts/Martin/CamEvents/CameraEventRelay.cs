using Unity.Netcode;
using UnityEngine;

public class CameraEventRelay : NetworkBehaviour
{
    public static CameraEventRelay Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void PlayForEveryone(CameraRequest request)
    {
        if (request == null)
            return;

        RequestPlayForEveryoneRpc(request.cameraID, request.duration);
    }

    public void PlayForTarget(CameraRequest request, ulong targetClientID)
    {
        if (request == null)
            return;

        RequestPlayForTargetRpc(request.cameraID, request.duration, targetClientID);
    }

    [Rpc(SendTo.Server)]
    private void RequestPlayForEveryoneRpc(int cameraID, float duration)
    {
        PlayForEveryoneClientRpc(cameraID, duration);
    }

    [Rpc(SendTo.Server)]
    private void RequestPlayForTargetRpc(int cameraID, float duration, ulong targetClientID)
    {
        PlayForTargetClientRpc(cameraID, duration, targetClientID);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayForEveryoneClientRpc(int cameraID, float duration)
    {
        if (CameraEventSystem.Instance == null)
        {
            Debug.LogError("[CameraEventRelay] CameraEventSystem missing.");
            return;
        }

        CameraEventSystem.Instance.Play(new CameraRequest
        {
            cameraID = cameraID,
            duration = duration
        });
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayForTargetClientRpc(int cameraID, float duration, ulong targetClientID)
    {
        if (NetworkManager.Singleton == null)
            return;

        if (NetworkManager.Singleton.LocalClientId != targetClientID)
            return;

        if (CameraEventSystem.Instance == null)
        {
            Debug.LogError("[CameraEventRelay] CameraEventSystem missing.");
            return;
        }

        CameraEventSystem.Instance.Play(new CameraRequest
        {
            cameraID = cameraID,
            duration = duration
        });
    }
}