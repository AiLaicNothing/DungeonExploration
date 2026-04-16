using UnityEngine;
using System;

[Serializable]
public class RuntimeStat
{
    public StatDefinition definition;

    private float _currentValue;

    public float CurrentValue
    {
        get => _currentValue;
        set => _currentValue = Mathf.Clamp(value, definition.minValue, definition.maxValue);
    }

    public float Min => definition.minValue;
    public float Max => definition.maxValue;
    public string Id => definition.statId;

    public RuntimeStat(StatDefinition def, float initialValue)
    {
        definition = def;
        CurrentValue = initialValue;
    }

    public float Modify(float amount)
    {
        float prev = _currentValue;
        CurrentValue = _currentValue + amount;
        return _currentValue - prev;
    }

    public bool CanModify(float amount)
    {
        float result = _currentValue + amount;
        return result >= Min && result <= Max;
    }
}