using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Componente de stats por jugador. NO singleton.
/// Hereda de NetworkBehaviour: el servidor es la autoridad sobre todos los valores,
/// los clientes los reciben automáticamente vía NetworkVariable.
///
/// Setup: añadir como componente al prefab del Player (junto a NetworkObject + PlayerController).
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class PlayerStats : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerStatsData data;

    // ── Stats sincronizadas (servidor autoritativo) ───────────────────
    // Una NetworkList paralela para currentValue y otra para max, indexadas igual que data.stats.
    // Usamos arrays porque NetworkList<float> existe pero un array de NetworkVariable es más explícito.
    // Aquí usamos NetworkList<float> que NGO soporta directamente.
    private NetworkList<float> _currentValues;
    private NetworkList<float> _maxValues;
    private NetworkList<int> _pointsAssigned;

    // Puntos disponibles para gastar (sincronizado)
    private NetworkVariable<int> _upgradePoints = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> _totalPointsEarned = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

    // ── Diccionario local de stats (config, no estado runtime) ────────
    private Dictionary<string, PlayerStat> _statsById;
    private Dictionary<string, int> _idToIndex; // id -> índice en NetworkList

    public IEnumerable<PlayerStat> AllStats => _statsById?.Values;

    // ── Eventos C# (locales, los clientes los disparan al recibir cambios de NetworkVariable) ──
    /// <summary>Disparado cuando una stat cambia. (id, currentValue)</summary>
    public event Action<string, float> OnStatChanged;
    /// <summary>Disparado cuando los upgradePoints cambian.</summary>
    public event Action<int> OnPointsChanged;

    // ── Acceso público para lectura (cualquiera puede leer) ───────────
    public int UpgradePoints => _upgradePoints.Value;
    public int upgradePoints => _upgradePoints.Value; // alias lowercase para compatibilidad
    public int TotalPointsEarned => _totalPointsEarned.Value;

    /// <summary>
    /// True cuando las NetworkList de stats están sincronizadas con el servidor.
    /// En el servidor, esto es true tras OnNetworkSpawn.
    /// En el cliente, esto se vuelve true cuando llegan los datos iniciales.
    /// </summary>
    public bool IsStatsReady => _currentValues != null
                              && _currentValues.Count > 0
                              && _currentValues.Count == _maxValues.Count
                              && _currentValues.Count == data.stats.Count;

    /// <summary>
    /// Disparado cuando las stats están listas para ser leídas.
    /// Útil para UIs que se inicializan al spawnear el player.
    /// Si ya están listas cuando te suscribes, se invoca inmediatamente.
    /// </summary>
    public event Action OnStatsReady;
    private bool _statsReadyFired = false;

    public float GetCurrentValue(string id)
    {
        if (!_idToIndex.TryGetValue(id, out int i)) return 0f;
        if (i < 0 || i >= _currentValues.Count) return 0f; // listas aún sin sincronizar
        return _currentValues[i];
    }

    public float GetMaxValue(string id)
    {
        if (!_idToIndex.TryGetValue(id, out int i)) return 0f;
        if (i < 0 || i >= _maxValues.Count) return 0f;
        return _maxValues[i];
    }

    public int GetPointsAssigned(string id)
    {
        if (!_idToIndex.TryGetValue(id, out int i)) return 0;
        if (i < 0 || i >= _pointsAssigned.Count) return 0;
        return _pointsAssigned[i];
    }

    /// <summary>
    /// Suscribe un callback al evento OnStatsReady, o lo invoca inmediatamente
    /// si las stats ya están listas. Patrón parecido al de LocalPlayer.SubscribeOrInvokeIfReady.
    /// </summary>
    public void SubscribeOrInvokeWhenReady(Action callback)
    {
        if (callback == null) return;
        OnStatsReady += callback;
        if (IsStatsReady) callback();
    }

    // ── Atajos al estilo del singleton anterior ───────────────────────
    public StatView Health => new StatView(this, "health");
    public StatView Mana => new StatView(this, "mana");
    public StatView Stamina => new StatView(this, "stamina");
    public StatView PhysicalDamage => new StatView(this, "physicalDamage");
    public StatView MagicalDamage => new StatView(this, "magicalDamage");
    public StatView HealthRegen => new StatView(this, "healthRegen");
    public StatView StaminaRegen => new StatView(this, "staminaRegen");
    public StatView ManaRegen => new StatView(this, "manaRegen");

    public PlayerStat GetStat(string id) => _statsById != null && _statsById.TryGetValue(id, out var s) ? s : null;

    // ── Lifecycle ─────────────────────────────────────────────────────
    private void Awake()
    {
        // Inicializa las NetworkList ANTES de OnNetworkSpawn
        _currentValues = new NetworkList<float>();
        _maxValues = new NetworkList<float>();
        _pointsAssigned = new NetworkList<int>();

        BuildLocalConfig();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Solo el servidor inicializa los valores
            for (int i = 0; i < data.stats.Count; i++)
            {
                _currentValues.Add(data.stats[i].baseValue);
                _maxValues.Add(data.stats[i].baseValue);
                _pointsAssigned.Add(0);
            }

            _upgradePoints.Value = data.startingPoints;
            _totalPointsEarned.Value = data.startingPoints;
        }

        // Suscribirse a cambios para emitir eventos C# locales
        _currentValues.OnListChanged += OnCurrentValuesListChanged;
        _maxValues.OnListChanged += OnMaxValuesListChanged;
        _upgradePoints.OnValueChanged += (oldV, newV) => OnPointsChanged?.Invoke(newV);

        // Si ya está listo (caso del servidor: acaba de llenar las listas), disparar el evento
        TryFireStatsReady();
    }

    public override void OnNetworkDespawn()
    {
        _currentValues.OnListChanged -= OnCurrentValuesListChanged;
        _maxValues.OnListChanged -= OnMaxValuesListChanged;
        _statsReadyFired = false;
    }

    /// <summary>
    /// Dispara OnStatsReady la primera vez que detectamos las listas pobladas.
    /// Se llama desde OnNetworkSpawn (servidor) y desde los OnListChanged (cliente).
    /// </summary>
    private void TryFireStatsReady()
    {
        if (_statsReadyFired) return;
        if (!IsStatsReady) return;

        _statsReadyFired = true;
        Debug.Log($"[PlayerStats] Stats listas (cliente {NetworkManager.Singleton.LocalClientId}, owner {OwnerClientId})");
        OnStatsReady?.Invoke();
    }

    private void BuildLocalConfig()
    {
        _statsById = new Dictionary<string, PlayerStat>();
        _idToIndex = new Dictionary<string, int>();

        for (int i = 0; i < data.stats.Count; i++)
        {
            var cfg = data.stats[i];
            if (string.IsNullOrEmpty(cfg.id))
            {
                Debug.LogError($"[PlayerStats] StatConfig sin ID en {data.name}.");
                continue;
            }
            _statsById[cfg.id] = new PlayerStat(cfg);
            _idToIndex[cfg.id] = i;
        }
    }

    private void OnCurrentValuesListChanged(NetworkListEvent<float> e)
    {
        // Cualquier cambio en la lista (incluido el primer poblado) puede significar que ya estamos listos
        TryFireStatsReady();

        if (e.Type != NetworkListEvent<float>.EventType.Value) return;
        // Buscamos el ID por índice para emitir el evento
        foreach (var kv in _idToIndex)
        {
            if (kv.Value == e.Index)
            {
                OnStatChanged?.Invoke(kv.Key, e.Value);
                return;
            }
        }
    }

    private void OnMaxValuesListChanged(NetworkListEvent<float> e)
    {
        if (e.Type != NetworkListEvent<float>.EventType.Value) return;
        // Cuando el max sube, también puede dispararse OnStatChanged para refrescar la UI
        foreach (var kv in _idToIndex)
        {
            if (kv.Value == e.Index)
            {
                OnStatChanged?.Invoke(kv.Key, _currentValues[e.Index]);
                return;
            }
        }
    }

    // ── REGEN PASIVA (solo en servidor) ───────────────────────────────
    private void Update()
    {
        if (!IsServer) return;

        TryRegen("health", "healthRegen");
        TryRegen("stamina", "staminaRegen");
        TryRegen("mana", "manaRegen");
    }

    private void TryRegen(string statId, string regenStatId)
    {
        if (!_idToIndex.TryGetValue(statId, out int idx)) return;
        if (!_idToIndex.TryGetValue(regenStatId, out int regenIdx)) return;

        float current = _currentValues[idx];
        float max = _maxValues[idx];
        if (current >= max) return;

        float regenPerSecond = _currentValues[regenIdx];
        float newValue = Mathf.Min(current + regenPerSecond * Time.deltaTime, max);
        _currentValues[idx] = newValue;
    }

    // ── MODIFICACIÓN DE STATS (autoritativo de servidor) ──────────────
    /// <summary>
    /// Modifica el currentValue de una stat. Solo se ejecuta en el servidor.
    /// Si lo llama un cliente, se redirige automáticamente vía RPC.
    /// </summary>
    public void Modify(string statId, float amount)
    {
        if (IsServer) Modify_Internal(statId, amount);
        else ModifyServerRpc(statId, amount);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ModifyServerRpc(FixedString64Bytes statId, float amount)
    {
        Modify_Internal(statId.Value, amount);
    }

    private void Modify_Internal(string statId, float amount)
    {
        if (!_idToIndex.TryGetValue(statId, out int idx)) return;
        float current = _currentValues[idx];
        float max = _maxValues[idx];
        _currentValues[idx] = Mathf.Clamp(current + amount, 0f, max);
    }

    /// <summary>Setea el currentValue directamente (autoritativo).</summary>
    public void SetCurrentValue(string statId, float value)
    {
        if (IsServer) SetCurrent_Internal(statId, value);
        else SetCurrentValueServerRpc(statId, value);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetCurrentValueServerRpc(FixedString64Bytes statId, float value)
    {
        SetCurrent_Internal(statId.Value, value);
    }

    private void SetCurrent_Internal(string statId, float value)
    {
        if (!_idToIndex.TryGetValue(statId, out int idx)) return;
        _currentValues[idx] = Mathf.Clamp(value, 0f, _maxValues[idx]);
    }

    // ── PUNTOS DE MEJORA (autoritativo) ───────────────────────────────
    /// <summary>Añade puntos al pool. Llamado por checkpoints (en servidor).</summary>
    public void AddUpgradePoints(int amount)
    {
        if (!IsServer)
        {
            AddUpgradePointsServerRpc(amount);
            return;
        }
        if (amount <= 0) return;
        _upgradePoints.Value += amount;
        _totalPointsEarned.Value += amount;
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddUpgradePointsServerRpc(int amount) => AddUpgradePoints(amount);

    /// <summary>
    /// Aplica un tradeoff. El cliente lo pide vía RPC, el servidor valida y aplica.
    /// </summary>
    public void RequestApplyTradeoff(string[] increaseIds, string[] decreaseIds)
    {
        // Convertir a FixedString para RPC
        var inc = new NetworkArrayString(increaseIds);
        var dec = new NetworkArrayString(decreaseIds);
        ApplyTradeoffServerRpc(inc, dec);
    }

    [ServerRpc(RequireOwnership = true)]
    private void ApplyTradeoffServerRpc(NetworkArrayString increaseIds, NetworkArrayString decreaseIds)
    {
        var incList = increaseIds.ToList();
        var decList = decreaseIds.ToList();

        // Validar costos
        int upgradeCost = 0, downgradeValue = 0;

        foreach (var id in incList)
        {
            if (!_statsById.TryGetValue(id, out var s)) return;
            int idx = _idToIndex[id];
            if (_maxValues[idx] + s.ValuePerPoint > s.HardMax) return;
            upgradeCost += s.UpgradeCost;
        }
        foreach (var id in decList)
        {
            if (!_statsById.TryGetValue(id, out var s)) return;
            int idx = _idToIndex[id];
            if (_maxValues[idx] - s.ValuePerPoint < s.MinValue) return;
            downgradeValue += s.DowngradeValue;
        }

        int needed = Mathf.Max(0, upgradeCost - downgradeValue);
        if (needed > _upgradePoints.Value) return;

        // Aplicar
        foreach (var id in decList) RemovePoint_Internal(id);
        foreach (var id in incList) AddPoint_Internal(id);

        _upgradePoints.Value -= needed;
    }

    private void AddPoint_Internal(string id)
    {
        if (!_idToIndex.TryGetValue(id, out int idx)) return;
        var stat = _statsById[id];
        float newMax = _maxValues[idx] + stat.ValuePerPoint;
        if (newMax > stat.HardMax) return;
        _maxValues[idx] = newMax;
        _currentValues[idx] = newMax; // recargar al subir
        _pointsAssigned[idx] = _pointsAssigned[idx] + 1;
    }

    private void RemovePoint_Internal(string id)
    {
        if (!_idToIndex.TryGetValue(id, out int idx)) return;
        var stat = _statsById[id];
        if (_pointsAssigned[idx] <= 0) return;
        float newMax = _maxValues[idx] - stat.ValuePerPoint;
        if (newMax < stat.MinValue) return;
        _maxValues[idx] = newMax;
        if (_currentValues[idx] > newMax) _currentValues[idx] = newMax;
        _pointsAssigned[idx] = _pointsAssigned[idx] - 1;
    }

    /// <summary>
    /// Sube un punto a una stat sin gastar upgradePoints. SOLO LLAMAR EN SERVIDOR
    /// y solo para restaurar snapshot al reconectar un jugador.
    /// </summary>
    public void RestorePointToStat(string id)
    {
        if (!IsServer) return;
        AddPoint_Internal(id);
    }

    /// <summary>
    /// Restaura los puntos disponibles y total ganados desde un snapshot.
    /// SOLO LLAMAR EN SERVIDOR.
    /// </summary>
    public void RestoreUpgradePoints(int available, int totalEarned)
    {
        if (!IsServer) return;
        _upgradePoints.Value = available;
        _totalPointsEarned.Value = totalEarned;
    }

    // ── HasResource: lectura local, sin RPC ───────────────────────────
    public bool HasResource(ResourceType type, float cost) => type switch
    {
        ResourceType.Stamina => GetCurrentValue("stamina") >= cost,
        ResourceType.Mana => GetCurrentValue("mana") >= cost,
        ResourceType.Health => GetCurrentValue("health") >= cost,
        _ => true
    };
}

// ─────────────────────────────────────────────────────────────────────
// StatView: estructura ligera que imita la API antigua de PlayerStat.
// Permite que el código existente siga escribiendo `Health.CurrentValue` en lugar de
// `GetCurrentValue("health")`. Internamente solo enruta llamadas al PlayerStats.
// ─────────────────────────────────────────────────────────────────────
public readonly struct StatView
{
    private readonly PlayerStats _owner;
    private readonly string _id;

    public StatView(PlayerStats owner, string id) { _owner = owner; _id = id; }

    public float CurrentValue => _owner.GetCurrentValue(_id);
    public float Max => _owner.GetMaxValue(_id);
    public int PointsAssigned => _owner.GetPointsAssigned(_id);

    public void Modify(float amount) => _owner.Modify(_id, amount);
    public void SetCurrentValue(float value) => _owner.SetCurrentValue(_id, value);
}

// ─────────────────────────────────────────────────────────────────────
// Wrapper para enviar arrays de strings vía RPC (NGO no soporta string[] directamente).
// ─────────────────────────────────────────────────────────────────────
public struct NetworkArrayString : INetworkSerializable
{
    private FixedString64Bytes[] _items;

    public NetworkArrayString(string[] strings)
    {
        _items = new FixedString64Bytes[strings?.Length ?? 0];
        if (strings != null)
            for (int i = 0; i < strings.Length; i++) _items[i] = strings[i];
    }

    public List<string> ToList()
    {
        var list = new List<string>();
        if (_items != null)
            foreach (var s in _items) list.Add(s.Value);
        return list;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
    {
        int len = _items?.Length ?? 0;
        s.SerializeValue(ref len);
        if (s.IsReader) _items = new FixedString64Bytes[len];
        for (int i = 0; i < len; i++) s.SerializeValue(ref _items[i]);
    }
}