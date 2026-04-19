using System;
using UnityEngine;

[Serializable]
public class PlayerStat
{
    private readonly PlayerStatsData.StatConfig config;

    [SerializeField] private float currentValue;
    [SerializeField] private float max;
    [SerializeField] private int pointsAssigned;

    public string Id => config.id;
    public string DisplayName => config.displayName;
    public float CurrentValue => currentValue;
    public float Max => max;
    public float Min => config.minValue;
    public float HardMax => config.maxValue;
    public float ValuePerPoint => config.valuePerPoint;
    public int UpgradeCost => config.upgradeCost;
    public int DowngradeValue => config.downgradeValue;
    public int PointsAssigned => pointsAssigned;

    /// <summary>Evento disparado cuando el valor o el max cambian.</summary>
    public event Action<PlayerStat> OnChanged;

    public PlayerStat(PlayerStatsData.StatConfig config)
    {
        this.config = config;
        this.max = config.baseValue;
        this.currentValue = config.baseValue;
        this.pointsAssigned = 0;
    }

    /// <summary>Sube 1 punto permanente. Retorna true si se pudo.</summary>
    public bool AddPoint()
    {
        float newMax = max + config.valuePerPoint;
        if (newMax > config.maxValue) return false;

        max = newMax;
        currentValue = max; // al subir, se recarga al tope
        pointsAssigned++;
        OnChanged?.Invoke(this);
        return true;
    }

    /// <summary>Baja 1 punto permanente. Retorna true si se pudo.</summary>
    public bool RemovePoint()
    {
        float newMax = max - config.valuePerPoint;
        if (newMax < config.minValue) return false;

        max = newMax;
        if (currentValue > max) currentValue = max;
        pointsAssigned--;
        OnChanged?.Invoke(this);
        return true;
    }

    /// <summary>Modifica el valor actual (daño, regeneración, consumo de stamina, etc).</summary>
    public void Modify(float amount)
    {
        currentValue = Mathf.Clamp(currentValue + amount, 0f, max);
        OnChanged?.Invoke(this);
    }

    /// <summary>Setea el valor actual directamente, clamped.</summary>
    public void SetCurrentValue(float value)
    {
        currentValue = Mathf.Clamp(value, 0f, max);
        OnChanged?.Invoke(this);
    }

    // ── Usado por el sistema de guardado ──────────────────────────────
    public void LoadFromSave(int savedPoints, float savedCurrentValue)
    {
        pointsAssigned = savedPoints;
        max = config.baseValue + (pointsAssigned * config.valuePerPoint);
        max = Mathf.Clamp(max, config.minValue, config.maxValue);
        currentValue = Mathf.Clamp(savedCurrentValue, 0f, max);
        OnChanged?.Invoke(this);
    }
}