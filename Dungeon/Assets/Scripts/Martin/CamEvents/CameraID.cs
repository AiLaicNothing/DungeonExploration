using Cinemachine;
using UnityEngine;

public class CameraID : MonoBehaviour
{
    [SerializeField] private int id;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;

    public int ID => id;
    public CinemachineVirtualCamera VirtualCamera => virtualCamera;

    private void Reset()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
    }
}
