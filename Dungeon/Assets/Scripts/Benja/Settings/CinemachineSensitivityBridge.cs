using Cinemachine;
using UnityEngine;

public class CinemachineSensitivityBridge : MonoBehaviour
{
    [Tooltip("Si no se asigna, busca un CinemachineFreeLook en este GameObject.")]
    [SerializeField] private CinemachineFreeLook freeLook;

    [Header("Sensibilidad base (multiplicada por el slider)")]
    [Tooltip("MaxSpeed base del eje X horizontal. El slider lo multiplica.")]
    [SerializeField] private float baseSpeedX = 300f;
    [Tooltip("MaxSpeed base del eje Y vertical. El slider lo multiplica.")]
    [SerializeField] private float baseSpeedY = 2f;

    void Awake()
    {
        if (freeLook == null)
            freeLook = GetComponent<CinemachineFreeLook>();

        if (freeLook == null)
        {
            Debug.LogError("[CinemachineSensitivityBridge] No se encontró CinemachineFreeLook.");
            return;
        }
    }

    void Start()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingsChanged += ApplySettings;
            ApplySettings(); // aplicar valores cargados al iniciar
        }
        else
        {
            Debug.LogWarning("[CinemachineSensitivityBridge] No hay SettingsManager en la escena.");
        }
    }

    void OnDestroy()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnSettingsChanged -= ApplySettings;
    }

    private void ApplySettings()
    {
        if (freeLook == null) return;

        var s = SettingsManager.Instance;
        if (s == null) return;

        // Sensibilidad: multiplica la velocidad base por el factor del slider
        freeLook.m_XAxis.m_MaxSpeed = baseSpeedX * s.Sensitivity;
        freeLook.m_YAxis.m_MaxSpeed = baseSpeedY * s.Sensitivity;

        // Invertir ejes
        freeLook.m_XAxis.m_InvertInput = s.InvertX;
        freeLook.m_YAxis.m_InvertInput = s.InvertY;
    }
}