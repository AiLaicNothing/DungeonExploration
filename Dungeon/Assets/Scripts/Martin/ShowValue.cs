using TMPro;
using UnityEngine;

/// <summary>
/// HUD que muestra HP/Stamina/Mana del player LOCAL (el del cliente actual).
/// Se conecta automáticamente al PlayerController local cuando spawnea.
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
        // Si el player ya existe (UI tardía), nos conectamos ya. Si no, esperamos al evento.
        LocalPlayer.SubscribeOrInvokeIfReady(OnLocalPlayerReady);
    }

    void OnDisable()
    {
        LocalPlayer.Unsubscribe(OnLocalPlayerReady);

        if (_player != null && _player.Stats != null)
            _player.Stats.OnStatChanged -= HandleStatChanged;

        _player = null;
    }

    private void OnLocalPlayerReady(PlayerController controller)
    {
        _player = controller;

        // Suscribirse a los cambios del NetworkVariable. Cada vez que el servidor
        // modifica una stat, los clientes reciben el cambio y disparan este evento.
        _player.Stats.OnStatChanged += HandleStatChanged;

        // Refresco inicial
        UpdateAll();
    }

    private void HandleStatChanged(string id, float value)
    {
        // Solo nos interesan estas 3 para el HUD
        if (id == "health" || id == "stamina" || id == "mana")
            UpdateAll();
    }

    private void UpdateAll()
    {
        if (_player == null) return;

        if (hpText != null)
            hpText.text = $"HP: {_player.CurrentHealth:F0} / {_player.MaxHealth:F0}";

        if (staminaText != null)
            staminaText.text = $"Stamina: {_player.CurrentStamina:F0} / {_player.MaxStamina:F0}";

        if (manaText != null)
            manaText.text = $"Mana: {_player.CurrentMana:F0} / {_player.MaxMana:F0}";
    }
}