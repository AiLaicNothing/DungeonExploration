using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cubo de debug en el mundo que sube/baja una stat específica del jugador local.
/// Útil para prototipar. En multiplayer, modifica la stat del jugador local
/// (no afecta a los demás).
/// </summary>
public class StatModifierCube : MonoBehaviour
{
    public enum StatType
    {
        Health, Mana, Stamina, PhysicalDamage, MagicalDamage,
        HealthRegen, StaminaRegen, ManaRegen
    }

    [Header("Config")]
    [SerializeField] private StatType statType = StatType.Health;
    [SerializeField] private bool addPoint = true; // true = añade punto, false = lo quita
    [SerializeField] private TMP_Text labelText; // se autocompleta con el nombre de la stat

    [Header("UI")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private GameObject panel; // panel world-space siempre activo

    void Start()
    {
        if (labelText != null)
            labelText.text = (addPoint ? "+ " : "- ") + statType.ToString();

        if (panel != null)
            panel.SetActive(true);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (LocalPlayer.Stats == null)
        {
            Debug.LogWarning("[StatModifierCube] No hay LocalPlayer.Stats.");
            return;
        }

        string id = ToId(statType);
        if (string.IsNullOrEmpty(id)) return;

        // Para subir/bajar puntos en multiplayer usamos RequestApplyTradeoff
        // (que es autoritativo de servidor y respeta los costos).
        if (addPoint)
            LocalPlayer.Stats.RequestApplyTradeoff(new[] { id }, new string[0]);
        else
            LocalPlayer.Stats.RequestApplyTradeoff(new string[0], new[] { id });
    }

    private string ToId(StatType type) => type switch
    {
        StatType.Health => "health",
        StatType.Mana => "mana",
        StatType.Stamina => "stamina",
        StatType.PhysicalDamage => "physicalDamage",
        StatType.MagicalDamage => "magicalDamage",
        StatType.HealthRegen => "healthRegen",
        StatType.StaminaRegen => "staminaRegen",
        StatType.ManaRegen => "manaRegen",
        _ => null
    };
}