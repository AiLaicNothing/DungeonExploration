using UnityEngine;
using System.Collections.Generic;
using System;
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Stat Definitions (arrastra los ScriptableObjects)")]
    public StatDefinition healthDef;
    public StatDefinition manaDef;
    public StatDefinition staminaDef;
    public StatDefinition physDamageDef;
    public StatDefinition magicDamageDef;
    public StatDefinition healthRegenDef;
    public StatDefinition manaRegenDef;
    public StatDefinition staminaRegenDef;

    [Header("Puntos de mejora")]
    public int upgradePoints = 0;

    public RuntimeStat Health { get; private set; }
    public RuntimeStat Mana { get; private set; }
    public RuntimeStat Stamina { get; private set; }
    public RuntimeStat PhysicalDamage { get; private set; }
    public RuntimeStat MagicDamage { get; private set; }
    public RuntimeStat HealthRegen { get; private set; }
    public RuntimeStat ManaRegen { get; private set; }
    public RuntimeStat StaminaRegen { get; private set; }

    public List<RuntimeStat> AllStats { get; private set; }

    public event Action<string, float> OnStatChanged;
    public event Action<int> OnPointsChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitStats();
        Load();
    }

    void InitStats()
    {
        Health = new RuntimeStat(healthDef, LoadFloat(healthDef));
        Mana = new RuntimeStat(manaDef, LoadFloat(manaDef));
        Stamina = new RuntimeStat(staminaDef, LoadFloat(staminaDef));
        PhysicalDamage = new RuntimeStat(physDamageDef, LoadFloat(physDamageDef));
        MagicDamage = new RuntimeStat(magicDamageDef, LoadFloat(magicDamageDef));
        HealthRegen = new RuntimeStat(healthRegenDef, LoadFloat(healthRegenDef));
        ManaRegen = new RuntimeStat(manaRegenDef, LoadFloat(manaRegenDef));
        StaminaRegen = new RuntimeStat(staminaRegenDef, LoadFloat(staminaRegenDef));

        AllStats = new List<RuntimeStat>
        {
            Health, Mana, Stamina,
            PhysicalDamage, MagicDamage,
            HealthRegen, ManaRegen, StaminaRegen
        };
    }

    void Update()
    {
        RegenerateStat(Health, HealthRegen.CurrentValue);
        RegenerateStat(Mana, ManaRegen.CurrentValue);
        RegenerateStat(Stamina, StaminaRegen.CurrentValue);
    }

    void RegenerateStat(RuntimeStat stat, float regenRate)
    {
        if (regenRate <= 0) return;
        float prev = stat.CurrentValue;
        stat.Modify(regenRate * Time.deltaTime);
        if (stat.CurrentValue != prev)
            OnStatChanged?.Invoke(stat.Id, stat.CurrentValue);
    }

    public bool ApplyTradeoff(string increaseId, string decreaseId)
    {
        var increase = GetStat(increaseId);
        var decrease = GetStat(decreaseId);

        if (increase == null || decrease == null) return false;
        if (increaseId == decreaseId) return false;
        if (!increase.CanModify(2f)) return false;
        if (!decrease.CanModify(-1f)) return false;
        if (upgradePoints <= 0) return false;

        increase.Modify(2f);
        decrease.Modify(-1f);
        upgradePoints--;

        OnStatChanged?.Invoke(increaseId, increase.CurrentValue);
        OnStatChanged?.Invoke(decreaseId, decrease.CurrentValue);
        OnPointsChanged?.Invoke(upgradePoints);

        Save();
        return true;
    }

    public void AddUpgradePoints(int amount)
    {
        upgradePoints += amount;
        PlayerPrefs.SetInt("upgradePoints", upgradePoints);
        OnPointsChanged?.Invoke(upgradePoints);
    }

    public RuntimeStat GetStat(string id)
    {
        foreach (var stat in AllStats)
            if (stat.Id == id) return stat;
        return null;
    }

    float LoadFloat(StatDefinition def) =>
        PlayerPrefs.GetFloat(def.statId, def.baseValue);

    public void Save()
    {
        foreach (var stat in AllStats)
            PlayerPrefs.SetFloat(stat.Id, stat.CurrentValue);
        PlayerPrefs.SetInt("upgradePoints", upgradePoints);
        PlayerPrefs.Save();
    }

    public void Load()
    {
        foreach (var stat in AllStats)
            stat.CurrentValue = PlayerPrefs.GetFloat(stat.Id, stat.definition.baseValue);
        upgradePoints = PlayerPrefs.GetInt("upgradePoints", 0);
    }

    public void ResetAll()
    {
        foreach (var stat in AllStats)
            stat.CurrentValue = stat.definition.baseValue;
        upgradePoints = 0;
        Save();
    }
}