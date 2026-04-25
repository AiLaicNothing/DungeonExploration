using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private PlayerStatsData data;

    // ── Diccionario de stats por ID ───────────────────────────────────
    private Dictionary<string, PlayerStat> statsById;
    public IEnumerable<PlayerStat> AllStats => statsById.Values;

    // ── Accesos rápidos para mantener compatibilidad con PlayerController ──
    public PlayerStat Health => GetStat("health");
    public PlayerStat Mana => GetStat("mana");
    public PlayerStat Stamina => GetStat("stamina");
    public PlayerStat PhysicalDamage => GetStat("physicalDamage");
    public PlayerStat MagicalDamage => GetStat("magicalDamage");
    public PlayerStat HealthRegen => GetStat("healthRegen");
    public PlayerStat StaminaRegen => GetStat("staminaRegen");
    public PlayerStat ManaRegen => GetStat("manaRegen");

    // ── Sistema de puntos ─────────────────────────────────────────────
    [SerializeField] private int _upgradePoints;
    public int upgradePoints => _upgradePoints; // lowercase para compatibilidad con tu UI
    public int UpgradePoints => _upgradePoints;
    public int TotalPointsEarned { get; private set; }

    // ── Eventos ───────────────────────────────────────────────────────
    public event Action<string, float> OnStatChanged;
    public event Action<int> OnPointsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeStats();
    }

    private void InitializeStats()
    {
        statsById = new Dictionary<string, PlayerStat>();

        foreach (var config in data.stats)
        {
            if (string.IsNullOrEmpty(config.id))
            {
                Debug.LogError($"[PlayerStats] Hay un StatConfig sin ID en {data.name}. Skip.");
                continue;
            }
            if (statsById.ContainsKey(config.id))
            {
                Debug.LogError($"[PlayerStats] ID duplicado: '{config.id}'. Skip.");
                continue;
            }

            var stat = new PlayerStat(config);
            stat.OnChanged += s => OnStatChanged?.Invoke(s.Id, s.CurrentValue);
            statsById[config.id] = stat;
        }

        _upgradePoints = data.startingPoints;
        TotalPointsEarned = data.startingPoints;
    }

    private void Update()
    {
        if (Health != null && HealthRegen != null && Health.CurrentValue < Health.Max)
            Health.Modify(HealthRegen.CurrentValue * Time.deltaTime);

        if (Stamina != null && StaminaRegen != null && Stamina.CurrentValue < Stamina.Max)
            Stamina.Modify(StaminaRegen.CurrentValue * Time.deltaTime);

        if (Mana != null && ManaRegen != null && Mana.CurrentValue < Mana.Max)
            Mana.Modify(ManaRegen.CurrentValue * Time.deltaTime);
    }

    public PlayerStat GetStat(string id)
    {
        if (statsById == null) return null;
        statsById.TryGetValue(id, out var stat);
        return stat;
    }

    // ── Sistema de tradeoff ───────────────────────────────────────────
    public bool ApplyTradeoff(List<string> increaseIds, List<string> decreaseIds)
    {
        if (increaseIds == null) increaseIds = new List<string>();
        if (decreaseIds == null) decreaseIds = new List<string>();

        if (increaseIds.Count == 0 && decreaseIds.Count == 0) return false;

        int upgradeCost = 0;
        foreach (var id in increaseIds)
        {
            var s = GetStat(id);
            if (s == null) { Debug.LogError($"[Tradeoff] Stat '{id}' no existe."); return false; }
            if (s.Max + s.ValuePerPoint > s.HardMax) { Debug.LogWarning($"[Tradeoff] {s.Id} ya está al máximo."); return false; }
            upgradeCost += s.UpgradeCost;
        }

        int downgradeValue = 0;
        foreach (var id in decreaseIds)
        {
            var s = GetStat(id);
            if (s == null) { Debug.LogError($"[Tradeoff] Stat '{id}' no existe."); return false; }
            if (s.Max - s.ValuePerPoint < s.Min) { Debug.LogWarning($"[Tradeoff] {s.Id} ya está al mínimo."); return false; }
            downgradeValue += s.DowngradeValue;
        }

        int pointsNeeded = Mathf.Max(0, upgradeCost - downgradeValue);
        if (pointsNeeded > _upgradePoints)
        {
            Debug.LogWarning($"[Tradeoff] Faltan puntos. Necesita {pointsNeeded}, tiene {_upgradePoints}.");
            return false;
        }

        foreach (var id in decreaseIds) GetStat(id).RemovePoint();
        foreach (var id in increaseIds) GetStat(id).AddPoint();

        _upgradePoints -= pointsNeeded;
        OnPointsChanged?.Invoke(_upgradePoints);
        return true;
    }

    public bool ApplyTradeoff(string increaseId, string decreaseId)
    {
        var inc = increaseId != null ? new List<string> { increaseId } : new List<string>();
        var dec = decreaseId != null ? new List<string> { decreaseId } : new List<string>();
        return ApplyTradeoff(inc, dec);
    }

    // ── Puntos ────────────────────────────────────────────────────────
    public void AddUpgradePoints(int amount)
    {
        if (amount <= 0) return;
        _upgradePoints += amount;
        TotalPointsEarned += amount;
        OnPointsChanged?.Invoke(_upgradePoints);
    }

    /// <summary>
    /// Resetea todas las stats al baseValue del ScriptableObject
    /// y vacía el pool de puntos. Usado por Savesystem.ResetInPlace().
    /// </summary>
    public void ResetToDefaults()
    {
        InitializeStats();

        if (statsById != null)
        {
            foreach (var stat in statsById.Values)
                OnStatChanged?.Invoke(stat.Id, stat.CurrentValue);
        }

        OnPointsChanged?.Invoke(_upgradePoints);
    }

    // ── Compatibilidad con PlayerController ───────────────────────────
    public bool HasResource(ResourceType type, float cost) => type switch
    {
        ResourceType.Stamina => Stamina != null && Stamina.CurrentValue >= cost,
        ResourceType.Mana => Mana != null && Mana.CurrentValue >= cost,
        ResourceType.Health => Health != null && Health.CurrentValue >= cost,
        _ => true
    };

    // ── Guardado / Carga ──────────────────────────────────────────────
    public PlayerStatsSaveData GetSaveData()
    {
        var save = new PlayerStatsSaveData
        {
            upgradePoints = _upgradePoints,
            totalPointsEarned = TotalPointsEarned,
            stats = new List<StatSaveEntry>()
        };

        foreach (var kv in statsById)
        {
            save.stats.Add(new StatSaveEntry
            {
                id = kv.Key,
                pointsAssigned = kv.Value.PointsAssigned,
                currentValue = kv.Value.CurrentValue
            });
        }
        return save;
    }

    public void LoadFromSaveData(PlayerStatsSaveData save)
    {
        if (save == null) return;

        _upgradePoints = save.upgradePoints;
        TotalPointsEarned = save.totalPointsEarned;

        if (save.stats != null)
        {
            foreach (var entry in save.stats)
            {
                var stat = GetStat(entry.id);
                stat?.LoadFromSave(entry.pointsAssigned, entry.currentValue);
            }
        }

        OnPointsChanged?.Invoke(_upgradePoints);
    }
}
