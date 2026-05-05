using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class AmbushWave
{
    [Tooltip("Nombre descriptivo (solo para identificar en el inspector).")]
    public string name = "Oleada";

    [Tooltip("Prefabs de enemigos a instanciar al iniciar la oleada.")]
    public List<GameObject> enemyPrefabs = new();

    [Tooltip("Spawn points donde aparecerán los enemyPrefabs. Si hay más enemigos que spawn points, se reusan.")]
    public List<Transform> spawnPoints = new();

    [Tooltip("Enemigos ya colocados en la escena que se activarán (SetActive(true)) al iniciar la oleada.")]
    public List<GameObject> sceneEnemies = new();

    [Tooltip("Segundos de espera antes de spawnear esta oleada (después de completarse la anterior).")]
    public float delayBeforeSpawn = 0f;

    [Tooltip("Opcional: secuencia de cámaras a reproducir AL INICIAR esta oleada (antes del spawn).\n" +
             "Útil para mostrar la llegada de refuerzos. Si es null, la oleada empieza directamente.")]
    public CinemachineSequence introWaveSequence;
}


public class AmbushZone : MonoBehaviour
{
    public enum State { Idle, Sequence, InCombat, Completed }

    [Header("Activación")]
    [Tooltip("Si está activo, el player puede activarla al entrar al collider de este GameObject.")]
    [SerializeField] private bool triggerOnPlayerEnter = true;
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Si está activo, la zona solo se activa una vez por sesión (después se queda Completed).")]
    [SerializeField] private bool oneTime = true;

    [Header("Bloqueadores")]
    [Tooltip("Lista de cosas que se bloquean al iniciar y se desbloquean al completar. " +
             "Cualquier MonoBehaviour que implemente IAmbushBlocker.")]
    [SerializeField] private List<MonoBehaviour> blockers = new();

    [Header("Oleadas")]
    [SerializeField] private List<AmbushWave> waves = new();

    [Header("Cinemáticas")]
    [Tooltip("Opcional: secuencia de cámaras al INICIAR la emboscada (mostrar puertas cerrándose, enemigos apareciendo).")]
    [SerializeField] private CinemachineSequence introSequence;

    [Tooltip("Opcional: secuencia de cámaras al COMPLETAR la emboscada (mostrar puertas abriéndose).")]
    [SerializeField] private CinemachineSequence outroSequence;

    [Header("Eventos UnityEvent (opcional, se disparan junto a los Action)")]
    public UnityEvent onAmbushStarted;
    public UnityEvent onWaveStarted;
    public UnityEvent onWaveCleared;
    public UnityEvent onAmbushCompleted;

    // Eventos C# (para suscripción desde código)
    public event Action OnAmbushStarted;
    public event Action<int> OnWaveStarted;     // índice de oleada
    public event Action<int> OnWaveCleared;     // índice de oleada
    public event Action OnAmbushCompleted;

    public State CurrentState { get; private set; } = State.Idle;
    public int CurrentWaveIndex { get; private set; } = -1;

    // Enemigos vivos de la oleada actual
    private readonly HashSet<IKillable> _aliveEnemies = new();
    // GameObjects spawneados (para limpieza)
    private readonly List<GameObject> _spawnedThisAmbush = new();
    // Índices de oleadas que ya fueron spawneadas (evita doble spawn por UnityEvent)
    private readonly HashSet<int> _wavesSpawned = new();

    // ── Activación ────────────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[AmbushZone:{name}] OnTriggerEnter con: {other.name} (tag: {other.tag})");

        if (!triggerOnPlayerEnter)
        {
            Debug.Log($"[AmbushZone:{name}] triggerOnPlayerEnter está desactivado, ignoro.");
            return;
        }
        if (CurrentState != State.Idle)
        {
            Debug.Log($"[AmbushZone:{name}] estado actual es {CurrentState}, ignoro.");
            return;
        }

        // Buscamos el tag tanto en el collider que entró como en sus padres.
        // Esto es necesario porque el collider del player suele estar en un hijo
        // (ej: "Player_Model") mientras que el tag "Player" está en la raíz.
        if (!IsPlayer(other))
        {
            Debug.Log($"[AmbushZone:{name}] no se encontró un GameObject con tag '{playerTag}' ni en {other.name} ni en sus padres, ignoro.");
            return;
        }

        Debug.Log($"[AmbushZone:{name}] TRIGGERING AMBUSH!");
        TriggerAmbush();
    }

    /// <summary>Verifica si el collider o alguno de sus padres tiene el tag del player.</summary>
    private bool IsPlayer(Collider col)
    {
        Transform t = col.transform;
        while (t != null)
        {
            if (t.CompareTag(playerTag)) return true;
            t = t.parent;
        }
        return false;
    }

    /// <summary>Activa la emboscada manualmente. Llamable desde otros scripts.</summary>
    public void TriggerAmbush()
    {
        if (CurrentState != State.Idle)
        {
            Debug.LogWarning($"[AmbushZone] {name} ya está activa o completada.");
            return;
        }

        StartCoroutine(AmbushRoutine());
    }

    // ── Flujo principal ───────────────────────────────────────────────
    private IEnumerator AmbushRoutine()
    {
        CurrentState = State.Sequence;
        OnAmbushStarted?.Invoke();
        onAmbushStarted?.Invoke();

        // 1. Reproducir cinemática de intro (si existe).
        // Las puertas se bloquean DESDE la cinemática usando UnityEvent en cada shot,
        // no aquí — así puedes sincronizar visualmente "shot que muestra puerta + cerrarla".
        // Lo mismo aplica para el spawn de la primera oleada: se puede disparar como
        // UnityEvent (SpawnFirstWaveFromEvent) en el shot que muestra el área de spawn.
        // Si no usas cinemática, las puertas se bloquean inmediatamente como fallback.
        if (introSequence != null)
        {
            introSequence.Play();
            while (introSequence.IsPlaying) yield return null;
        }
        else
        {
            BlockAll();
        }

        // 2. Iniciar combate por oleadas
        CurrentState = State.InCombat;

        for (int i = 0; i < waves.Count; i++)
        {
            CurrentWaveIndex = i;
            var wave = waves[i];

            if (wave.delayBeforeSpawn > 0f)
                yield return new WaitForSeconds(wave.delayBeforeSpawn);

            // Cinemática opcional ANTES de esta oleada (mostrar refuerzos llegando, etc.)
            if (wave.introWaveSequence != null)
            {
                wave.introWaveSequence.Play();
                while (wave.introWaveSequence.IsPlaying) yield return null;
            }

            // Si la oleada NO fue spawneada ya por un UnityEvent durante la cinemática,
            // la spawneamos ahora como fallback.
            if (!_wavesSpawned.Contains(i))
            {
                SpawnWave(wave);
                _wavesSpawned.Add(i);
            }

            OnWaveStarted?.Invoke(i);
            onWaveStarted?.Invoke();

            // Esperar a que mueran todos los enemigos
            while (_aliveEnemies.Count > 0)
                yield return null;

            OnWaveCleared?.Invoke(i);
            onWaveCleared?.Invoke();
        }

        // 3. Cinemática de outro (si existe). Las puertas se desbloquean
        // DESDE la cinemática con UnityEvents. Si no hay cinemática, fallback inmediato.
        if (outroSequence != null)
        {
            outroSequence.Play();
            while (outroSequence.IsPlaying) yield return null;
        }
        else
        {
            UnblockAll();
        }

        // 4. Completar
        CurrentState = State.Completed;
        OnAmbushCompleted?.Invoke();
        onAmbushCompleted?.Invoke();

        if (!oneTime)
        {
            _spawnedThisAmbush.Clear();
            _wavesSpawned.Clear();
            CurrentWaveIndex = -1;
            CurrentState = State.Idle;
        }
    }

    // ── Métodos públicos para llamar desde UnityEvents de cinemáticas ─
    /// <summary>
    /// Spawnea la primera oleada (índice 0). Pensado para llamarse desde un UnityEvent
    /// en un shot de la introSequence (por ejemplo, en el shot que muestra el área de spawn).
    /// Si la oleada ya fue spawneada, no hace nada.
    /// </summary>
    public void SpawnFirstWaveFromEvent()
    {
        SpawnWaveByIndex(0);
    }

    /// <summary>
    /// Spawnea una oleada específica por índice. Pensado para llamarse desde UnityEvents
    /// en cinemáticas per-wave (introWaveSequence de cada oleada).
    /// Si la oleada ya fue spawneada, no hace nada.
    /// </summary>
    public void SpawnWaveByIndex(int index)
    {
        if (index < 0 || index >= waves.Count)
        {
            Debug.LogWarning($"[AmbushZone] Índice de oleada inválido: {index}");
            return;
        }
        if (_wavesSpawned.Contains(index)) return;

        SpawnWave(waves[index]);
        _wavesSpawned.Add(index);
    }

    // ── Spawn ─────────────────────────────────────────────────────────
    private void SpawnWave(AmbushWave wave)
    {
        // Spawnear prefabs
        for (int i = 0; i < wave.enemyPrefabs.Count; i++)
        {
            var prefab = wave.enemyPrefabs[i];
            if (prefab == null) continue;

            Transform spawnPoint = wave.spawnPoints.Count > 0
                ? wave.spawnPoints[i % wave.spawnPoints.Count]
                : transform;

            var instance = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            _spawnedThisAmbush.Add(instance);
            RegisterEnemy(instance);
        }

        // Activar enemigos de la escena
        foreach (var sceneEnemy in wave.sceneEnemies)
        {
            if (sceneEnemy == null) continue;
            sceneEnemy.SetActive(true);
            RegisterEnemy(sceneEnemy);
        }
    }

    private void RegisterEnemy(GameObject enemy)
    {
        // Buscamos el componente que implemente IKillable, ya sea en el root o en hijos
        var killable = enemy.GetComponent<IKillable>();
        if (killable == null) killable = enemy.GetComponentInChildren<IKillable>();

        if (killable == null)
        {
            Debug.LogWarning($"[AmbushZone] El enemigo '{enemy.name}' no implementa IKillable. " +
                             $"No se podrá detectar su muerte. Añade IKillable a tu script de enemigo.");
            return;
        }

        _aliveEnemies.Add(killable);
        killable.OnKilled += HandleEnemyKilled;
    }

    private void HandleEnemyKilled(IKillable enemy)
    {
        enemy.OnKilled -= HandleEnemyKilled;
        _aliveEnemies.Remove(enemy);
    }

    // ── Bloqueo ───────────────────────────────────────────────────────
    /// <summary>Bloquea todas las entradas. Público para que pueda llamarse desde
    /// UnityEvents de los shots de la cinemática (ej: "cerrar puertas en este shot").</summary>
    public void BlockAll()
    {
        foreach (var b in blockers)
        {
            if (b is IAmbushBlocker blocker) blocker.Block();
            else if (b != null) Debug.LogWarning($"[AmbushZone] '{b.name}' no implementa IAmbushBlocker.");
        }
    }

    /// <summary>Desbloquea todas las entradas. Público para uso desde UnityEvents.</summary>
    public void UnblockAll()
    {
        foreach (var b in blockers)
        {
            if (b is IAmbushBlocker blocker) blocker.Unblock();
        }
    }
}