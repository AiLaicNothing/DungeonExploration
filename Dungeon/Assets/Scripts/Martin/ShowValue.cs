using TMPro;
using UnityEngine;

public class ShowValue : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player; // Script X
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text staminaText;

    void Update()
    {
        if (player == null) return;

        UpdateUI();
    }

    private void UpdateUI()
    {
        hpText.text = "HP: " + player.currentHealth.ToString();
        staminaText.text = "Stamina: " + player.CurrentStamina.ToString();
    }
}
