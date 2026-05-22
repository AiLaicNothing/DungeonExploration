using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Sistema de rejoin:
/// - Guarda la última sesión jugada
/// - Permite reconectar
/// - Mantiene sesiones persistentes incluso si sales al menú
///
/// IMPORTANTE:
/// - Ya NO se borra automáticamente al salir
/// - Solo se limpia si:
///     • la sesión expiró
///     • el usuario la elimina manualmente
///     • el rejoin falla
///
/// Vive como singleton persistente.
/// </summary>
public class RejoinManager : MonoBehaviour
{
    public static RejoinManager Instance { get; private set; }

    private const string KEY_LAST_SESSION_ID =
        "rejoin_last_session_id";

    private const string KEY_LAST_SESSION_NAME =
        "rejoin_last_session_name";

    private const string KEY_LAST_SESSION_TIME =
        "rejoin_last_session_time_ticks";

    [Header("Config")]

    [Tooltip(
        "Tiempo máximo para considerar válida una sesión reciente."
    )]
    [SerializeField]
    private int rejoinWindowSeconds = 600;

    // ════════════════════════════════════════════════════════════════
    // PROPERTIES
    // ════════════════════════════════════════════════════════════════

    public string LastSessionId =>
        PlayerPrefs.GetString(KEY_LAST_SESSION_ID, "");

    public string LastSessionName =>
        PlayerPrefs.GetString(KEY_LAST_SESSION_NAME, "");

    /// <summary>
    /// True si existe una sesión almacenada.
    /// </summary>
    public bool HasStoredSession =>
        !string.IsNullOrEmpty(LastSessionId);

    /// <summary>
    /// True si la sesión guardada sigue dentro
    /// de la ventana de rejoin.
    /// </summary>
    public bool HasRecentSession
    {
        get
        {
            if (!HasStoredSession)
                return false;

            long ticks = long.Parse(
                PlayerPrefs.GetString(
                    KEY_LAST_SESSION_TIME,
                    "0"
                )
            );

            if (ticks == 0)
                return false;

            DateTime savedAt =
                new DateTime(ticks, DateTimeKind.Utc);

            return
                (DateTime.UtcNow - savedAt).TotalSeconds
                < rejoinWindowSeconds;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // UNITY
    // ════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (SessionManager.Instance != null)
        {
            // SOLO guardar automáticamente
            SessionManager.Instance.OnSessionJoined += SaveCurrentSession;
        }
    }

    private void OnDisable()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.OnSessionJoined -= SaveCurrentSession;
        }
    }

    // ════════════════════════════════════════════════════════════════
    // SAVE SESSION
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Guarda automáticamente la sesión actual.
    /// </summary>
    private void SaveCurrentSession()
    {
        if (SessionManager.Instance == null)
            return;

        var session = SessionManager.Instance.CurrentSession;

        if (session == null)
            return;

        PlayerPrefs.SetString(
            KEY_LAST_SESSION_ID,
            session.Id
        );

        PlayerPrefs.SetString(
            KEY_LAST_SESSION_NAME,
            session.Name
        );

        PlayerPrefs.SetString(
            KEY_LAST_SESSION_TIME,
            DateTime.UtcNow.Ticks.ToString()
        );

        PlayerPrefs.Save();

        Debug.Log(
            $"[RejoinManager] Rejoin guardado: " +
            $"{session.Name} ({session.Id})"
        );
    }

    // ════════════════════════════════════════════════════════════════
    // CLEAR SESSION
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Borra manualmente la sesión guardada.
    /// </summary>
    public void ClearSession()
    {
        PlayerPrefs.DeleteKey(KEY_LAST_SESSION_ID);
        PlayerPrefs.DeleteKey(KEY_LAST_SESSION_NAME);
        PlayerPrefs.DeleteKey(KEY_LAST_SESSION_TIME);

        PlayerPrefs.Save();

        Debug.Log(
            "[RejoinManager] Datos de rejoin borrados."
        );
    }

    // ════════════════════════════════════════════════════════════════
    // TRY REJOIN
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Intenta reconectar a la última sesión.
    /// </summary>
    public async Task<bool> TryRejoin()
    {
        if (!HasRecentSession)
        {
            Debug.Log(
                "[RejoinManager] No hay sesión reciente."
            );

            return false;
        }

        string sessionId = LastSessionId;

        Debug.Log(
            $"[RejoinManager] Intentando rejoin a {sessionId}..."
        );

        bool success =
            await SessionManager.Instance
                .JoinSessionById(sessionId);

        if (!success)
        {
            Debug.LogWarning(
                "[RejoinManager] " +
                "La sesión ya no existe. Limpiando datos."
            );

            ClearSession();
        }

        return success;
    }
}