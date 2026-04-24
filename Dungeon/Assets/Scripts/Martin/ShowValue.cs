using TMPro;
using UnityEngine;

public class ShowValue : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text staminaText;
    [SerializeField] private TMP_Text manaText;

    void Update()
    {
        if (player == null) return;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (hpText != null)
            hpText.text = $"HP: {player.CurrentHealth:F0} / {player.MaxHealth:F0}";

        if (staminaText != null)
            staminaText.text = $"Stamina: {player.CurrentStamina:F0} / {player.MaxStamina:F0}";

        if (manaText != null)
            manaText.text = $"Mana: {player.CurrentMana:F0} / {player.MaxMana:F0}";
    }
}
