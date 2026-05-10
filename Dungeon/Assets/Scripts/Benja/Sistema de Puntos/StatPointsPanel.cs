using TMPro;
using UnityEngine;

/// <summary>
/// Panel pequeño que muestra los puntos disponibles del jugador local.
/// Se actualiza automáticamente cuando el servidor cambia el valor.
/// </summary>
public class StatPointsPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text pointsText;

    private PlayerStats _stats;

    void OnEnable()
    {
        LocalPlayer.SubscribeOrInvokeIfReady(OnLocalPlayerReady);
    }

    void OnDisable()
    {
        LocalPlayer.Unsubscribe(OnLocalPlayerReady);
        if (_stats != null) _stats.OnPointsChanged -= UpdateText;
        _stats = null;
    }

    private void OnLocalPlayerReady(PlayerController controller)
    {
        if (controller.Stats == null) return;

        _stats = controller.Stats;
        _stats.OnPointsChanged += UpdateText;

        // Esperar a que las stats estén sincronizadas para mostrar el primer valor
        _stats.SubscribeOrInvokeWhenReady(() => UpdateText(_stats.UpgradePoints));
    }

    private void UpdateText(int value)
    {
        if (pointsText != null)
            pointsText.text = $"Puntos disponibles: {value}";
    }
}