using TMPro;
using UnityEngine;

public class StatModifierCube : MonoBehaviour
{
    public enum StatTarget
    {
        Health,
        Mana,
        Stamina,
        PhysicalDamage,
        MagicalDamage,
        HealthRegen,
        StaminaRegen,
        ManaRegen
    }

    [Header("Configuración")]
    [Tooltip("Qué stat modifica este cubo.")]
    [SerializeField] private StatTarget targetStat = StatTarget.Health;

    [Tooltip("Cuánto sube/baja el Max de la stat por click.")]
    [SerializeField] private float modifyAmount = 10f;

    [Header("UI World Space")]
    [Tooltip("Texto que muestra el nombre de la stat que modifica este cubo.")]
    [SerializeField] private TMP_Text statNameText;

    void Start()
    {
        if (statNameText != null)
            statNameText.text = targetStat.ToString();
    }

    void OnValidate()
    {
        if (statNameText != null)
            statNameText.text = targetStat.ToString();
    }

    public void OnIncreaseClicked()
    {
        PlayerStat stat = GetTargetStat();
        if (stat == null) return;

        float newMax = stat.Max + modifyAmount;

        if (newMax > stat.HardMax)
        {
            Debug.Log($"[StatModifierCube] {targetStat} ya está en el máximo ({stat.HardMax}).");
            return;
        }

        int pointsToAdd = Mathf.RoundToInt(modifyAmount / stat.ValuePerPoint);
        for (int i = 0; i < pointsToAdd; i++)
        {
            if (!stat.AddPoint()) break;
        }

        Debug.Log($"[StatModifierCube] {targetStat} subió a {stat.Max}");
    }

    public void OnDecreaseClicked()
    {
        PlayerStat stat = GetTargetStat();
        if (stat == null) return;

        float newMax = stat.Max - modifyAmount;

        if (newMax < stat.Min)
        {
            Debug.Log($"[StatModifierCube] {targetStat} ya está en el mínimo ({stat.Min}).");
            return;
        }

        int pointsToRemove = Mathf.RoundToInt(modifyAmount / stat.ValuePerPoint);
        for (int i = 0; i < pointsToRemove; i++)
        {
            if (!stat.RemovePoint()) break;
        }

        Debug.Log($"[StatModifierCube] {targetStat} bajó a {stat.Max}");
    }

    private PlayerStat GetTargetStat()
    {
        var s = PlayerStats.Instance;
        if (s == null) { Debug.LogError("[StatModifierCube] PlayerStats.Instance es null."); return null; }

        return targetStat switch
        {
            StatTarget.Health => s.Health,
            StatTarget.Mana => s.Mana,
            StatTarget.Stamina => s.Stamina,
            StatTarget.PhysicalDamage => s.PhysicalDamage,
            StatTarget.MagicalDamage => s.MagicalDamage,
            StatTarget.HealthRegen => s.HealthRegen,
            StatTarget.StaminaRegen => s.StaminaRegen,
            StatTarget.ManaRegen => s.ManaRegen,
            _ => null
        };
    }
}