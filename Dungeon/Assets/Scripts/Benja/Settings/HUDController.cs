using UnityEngine;

public class HUDController : MonoBehaviour
{
    [Tooltip("Si se asigna, se activa/desactiva este GameObject. Si no, se usa el del propio script.")]
    [SerializeField] private GameObject hudRoot;

    void Start()
    {
        if (hudRoot == null) hudRoot = gameObject;

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnHudVisibilityChanged += SetHudVisible;
            SetHudVisible(!SettingsManager.Instance.HideHud);
        }
    }

    void OnDestroy()
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.OnHudVisibilityChanged -= SetHudVisible;
    }

    private void SetHudVisible(bool visible)
    {
        if (hudRoot != null) hudRoot.SetActive(visible);
    }
}
