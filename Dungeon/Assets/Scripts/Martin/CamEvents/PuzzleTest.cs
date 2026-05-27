using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PuzzleTest : NetworkBehaviour, IInteractable
{
    [Header("Camera")]
    [SerializeField] private string cameraID = "Test";
    [SerializeField] private float duration = 3f;

    [Header("Puzzle")]
    [SerializeField] private bool isActive;

    public void Interact()
    {
        if (isActive)  return;

        if (LocalPlayer.Controller == null) return;

        ulong clientID = LocalPlayer.Controller.OwnerClientId;

        ActivatePuzzleServerRpc(clientID);
    }

    [Rpc(SendTo.Server)]
    private void ActivatePuzzleServerRpc(ulong clientID)
    {
        if (isActive) return;

        isActive = true;

        // PLAY CAMERA ONLY FOR PLAYER WHO ACTIVATED IT
        CameraRequest request = new CameraRequest
        {
            cameraID = cameraID,
            duration = duration
        };

        CameraEventRelay.Instance.PlayForTarget(request, clientID);

        // ACTIVATE GAMEPLAY LOGIC
        StartCoroutine(Activate());

        // OPTIONAL VISUAL FX FOR EVERYONE
        ActivePuzzleClientRpc();
    }

    private IEnumerator Activate()
    {
        Debug.Log("Puzzle Activated");

        // Example:
        // door open
        // platform move
        // etc

        yield return new WaitForSeconds(duration);

        Debug.Log("Puzzle Finished");
    }

    [ClientRpc]
    private void ActivePuzzleClientRpc()
    {
        Debug.Log("Puzzle has been activated");
    }
}