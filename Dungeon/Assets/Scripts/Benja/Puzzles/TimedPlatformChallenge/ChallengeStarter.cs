using Unity.Netcode;
using UnityEngine;

public class ChallengeStarter : NetworkBehaviour, IInteractable
{
    [SerializeField]
    private TimedPlatformChallenge challenge;

    public void Interact()
    {
        StartChallengeServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartChallengeServerRpc()
    {
        challenge.StartChallenge();
    }
}