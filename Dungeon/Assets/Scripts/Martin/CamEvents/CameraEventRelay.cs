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

    // =========================================================
    // PUBLIC
    // =========================================================

    public void PlayForEveryone(CameraRequest request)
    {
        if (request == null) return;

        RequestPlayForEveryoneRpc(request.cameraID, request.duration);
    }

    public void PlayForTarget(CameraRequest request, ulong targetClientID)
    {
        if (request == null) return;

        ulong followID = 0;
        ulong lookID = 0;

        if (request.followTarget != null)
        {
            NetworkObject netObj = request.followTarget.GetComponent<NetworkObject>();

            if (netObj != null)
            {
                followID = netObj.NetworkObjectId;
            }
        }

        if (request.lookAtTarget != null)
        {
            NetworkObject netObj = request.lookAtTarget.GetComponent<NetworkObject>();

            if (netObj != null)
            {
                lookID = netObj.NetworkObjectId;
            }
        }

        RequestPlayForTargetRpc(request.cameraID, request.duration, followID, lookID, targetClientID);
    }

    // =========================================================
    // SERVER RPC
    // =========================================================

    [Rpc(SendTo.Server)]
    private void RequestPlayForEveryoneRpc(string cameraID, float duration)
    {
        PlayForEveryoneClientRpc(cameraID, duration);
    }

    [Rpc(SendTo.Server)]
    private void RequestPlayForTargetRpc(string cameraID, float duration, ulong followTargetID, ulong lookAtTargetID, ulong targetClientID)
    {
        ClientRpcParams rpcParams = new ClientRpcParams { Send = new ClientRpcSendParams {TargetClientIds = new ulong[] { targetClientID } } };

        PlayForTargetClientRpc(cameraID, duration, followTargetID, lookAtTargetID, rpcParams);
    }

    // =========================================================
    // CLIENT RPC
    // =========================================================

    [ClientRpc]
    private void PlayForEveryoneClientRpc(string cameraID, float duration)
    {
        CameraRequest request = new CameraRequest
        {
            cameraID = cameraID,
            duration = duration
        };

        CameraEventSystem.Instance.Play(request);
    }

    [ClientRpc]
    private void PlayForTargetClientRpc(string cameraID, float duration, ulong followTargetID, ulong lookAtTargetID, ClientRpcParams rpcParams = default)
    {
        Transform followTarget = null;
        Transform lookAtTarget = null;

        if (followTargetID != 0)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue( followTargetID, out var followObj))
            {
                followTarget = followObj.transform;
            }
        }

        if (lookAtTargetID != 0)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(lookAtTargetID, out var lookObj))
            {
                lookAtTarget = lookObj.transform;
            }
        }

        CameraRequest request = new CameraRequest
        {
            cameraID = cameraID,
            duration = duration,
            followTarget = followTarget,
            lookAtTarget = lookAtTarget
        };

        CameraEventSystem.Instance.Play(request);
    }
}
