using Unity.Netcode;
using UnityEngine;

public class ChallengeGoal : NetworkBehaviour, IInteractable
{
    [SerializeField]
    private TimedPlatformChallenge challenge;

    public void Interact()
    {
        CompleteChallengeServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void CompleteChallengeServerRpc()
    {
        challenge.CompleteChallenge();
    }
}