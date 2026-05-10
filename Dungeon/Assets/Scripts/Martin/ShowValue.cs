using TMPro;
using UnityEngine;

/// <summary>
/// HUD que muestra HP/Stamina/Mana del player LOCAL.
/// Se conecta automáticamente al PlayerController local cuando spawnea
/// y espera a que las stats estén sincronizadas antes de mostrar valores.
/// </summary>
public class ShowValue : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text staminaText;
    [SerializeField] private TMP_Text manaText;

    private PlayerController _player;

    void OnEnable()
    {
        LocalPlayer.SubscribeOrInvokeIfReady(OnLocalPlayerReady);
    }

    void OnDisable()
    {
        LocalPlayer.Unsubscribe(OnLocalPlayerReady);
        UnsubscribeFromStats();
        _player = null;
    }

    private void OnLocalPlayerReady(PlayerController controller)
    {
        UnsubscribeFromStats();
        _player = controller;

        if (_player.Stats == null)
        {
            Debug.LogWarning("[ShowValue] PlayerController sin Stats asignados.");
            return;
        }

        // Esperar a que las stats estén sincronizadas antes de leer
        _player.Stats.SubscribeOrInvokeWhenReady(OnStatsReady);
    }

    private void OnStatsReady()
    {
        if (_player == null || _player.Stats == null) return;

        _player.Stats.OnStatChanged += HandleStatChanged;
        UpdateAll();
    }

    private void UnsubscribeFromStats()
    {
        if (_player != null && _player.Stats != null)
            _player.Stats.OnStatChanged -= HandleStatChanged;
    }

    private void HandleStatChanged(string id, float value)
    {
        if (id == "health" || id == "stamina" || id == "mana")
            UpdateAll();
    }

    private void UpdateAll()
    {
        if (_player == null || _player.Stats == null) return;
        if (!_player.Stats.IsStatsReady) return;

        if (hpText != null)
            hpText.text = $"HP: {_player.CurrentHealth:F0} / {_player.MaxHealth:F0}";

        if (staminaText != null)
            staminaText.text = $"Stamina: {_player.CurrentStamina:F0} / {_player.MaxStamina:F0}";

        if (manaText != null)
            manaText.text = $"Mana: {_player.CurrentMana:F0} / {_player.MaxMana:F0}";
    }
}