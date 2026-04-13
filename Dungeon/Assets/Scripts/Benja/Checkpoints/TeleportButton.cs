using UnityEngine;

public class TeleportButton : MonoBehaviour
{
    public Checkpoint checkpoint;
    public Transform player;

    public void Teleport()
    {
        player.position = checkpoint.spawnPoint.position;
    }
}