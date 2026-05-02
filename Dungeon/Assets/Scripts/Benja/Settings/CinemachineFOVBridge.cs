using Cinemachine;
using UnityEngine;

public class CinemachineFOVBridge : MonoBehaviour
{
    [SerializeField] private CinemachineFreeLook freeLook;

    void Awake()
    {
        if (freeLook == null) freeLook = GetComponent<CinemachineFreeLook>();
    }

    void Start()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnFOVChanged += ApplyFOV;
            ApplyFOV(SettingsManager.Instance.FOV);
        }
    }

    void OnDestroy()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnFOVChanged -= ApplyFOV;
    }

    private void ApplyFOV(float fov)
    {
        if (freeLook == null) return;
        // FreeLook tiene 3 rigs: Top, Middle, Bottom. Aplicamos a los 3.
        freeLook.m_Lens.FieldOfView = fov;
        for (int i = 0; i < 3; i++)
        {
            var rig = freeLook.GetRig(i);
            if (rig != null) rig.m_Lens.FieldOfView = fov;
        }
    }
}
