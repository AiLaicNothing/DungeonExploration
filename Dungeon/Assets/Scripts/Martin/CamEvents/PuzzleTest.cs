using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PuzzleTest : NetworkBehaviour, IInteractable
{
    [Header("Camera")]
    [SerializeField] private int cameraID = 1;
    [SerializeField] private float duration = 3f;

    [Header("Puzzle")]
    [SerializeField] private bool isActive;

    public void Interact()
    {
        if (isActive)
            return;

        ActivatePuzzleServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void ActivatePuzzleServerRpc(RpcParams rpcParams = default)
    {
        if (isActive)
            return;

        isActive = true;

        ulong senderClientID = rpcParams.Receive.SenderClientId;

        CameraRequest request = new CameraRequest
        {
            cameraID = cameraID,
            duration = duration
        };

        if (CameraEventRelay.Instance != null)
        {
            CameraEventRelay.Instance.PlayForTarget(request, senderClientID);
        }
        else
        {
            Debug.LogError("[PuzzleTest] CameraEventRelay missing.");
        }

        StartCoroutine(Activate());
    }

    private IEnumerator Activate()
    {
        Debug.Log("Puzzle Activated");

        yield return new WaitForSeconds(duration);

        Debug.Log("Puzzle Finished");
    }
}