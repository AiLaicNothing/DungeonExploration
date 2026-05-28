using Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class PlayerCamRegister : NetworkBehaviour
{
    [SerializeField]
    private CinemachineFreeLook playerCam;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
            return;

        if (playerCam == null)
        {
            playerCam =
                GetComponentInChildren<CinemachineFreeLook>();
        }

        if (playerCam == null)
        {
            Debug.LogError("[PlayerCamRegister] No gameplay camera found");
            return;
        }

        if (CameraDirector.Instance != null)
        {
            CameraDirector.Instance.RegisterPlayerCam(playerCam);
        }
    }
}
