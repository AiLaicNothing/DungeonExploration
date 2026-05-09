using UnityEngine;

/// <summary>
/// Clase de configuración de una stat individual.
/// En multiplayer ya no almacena estado runtime (currentValue, max, etc.) — eso vive en
/// NetworkVariables del PlayerStats para ser sincronizable.
///
/// Esta clase sigue siendo útil como "vista" de la config del ScriptableObject por ID.
/// </summary>
public class PlayerStat
{
    private readonly PlayerStatsData.StatConfig config;

    public string Id => config.id;
    public string DisplayName => config.displayName;
    public float BaseValue => config.baseValue;
    public float MinValue => config.minValue;
    public float HardMax => config.maxValue;
    public float ValuePerPoint => config.valuePerPoint;
    public int UpgradeCost => config.upgradeCost;
    public int DowngradeValue => config.downgradeValue;

    public PlayerStat(PlayerStatsData.StatConfig config)
    {
        this.config = config;
    }
}