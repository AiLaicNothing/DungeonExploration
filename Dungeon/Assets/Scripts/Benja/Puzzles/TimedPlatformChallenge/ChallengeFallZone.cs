using Unity.Netcode;
using UnityEngine;

public class ChallengeFallZone : NetworkBehaviour
{
    [SerializeField]
    private Transform respawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
            return;

        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player == null)
            return;

        player.TeleportToPosition(
            respawnPoint.position);
    }

}