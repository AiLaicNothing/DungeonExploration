using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Sistema de rejoin: guarda el ID de la última sesión a la que el jugador
/// se unió, y permite reconectarse si la sesión sigue activa.
///
/// Vive como singleton persistente. Auto-guarda cuando el jugador entra a una sala.
/// </summary>
public class RejoinManager : MonoBehaviour
{
    public static RejoinManager Instance { get; private set; }

    private const string KEY_LAST_SESSION_ID = "rejoin_last_session_id";
    private const string KEY_LAST_SESSION_NAME = "rejoin_last_session_name";
    private const string KEY_LAST_SESSION_TIME = "rejoin_last_session_time_ticks";

    [Tooltip("Ventana de tiempo en segundos durante la cual se considera 'reciente' una sesión.")]
    [SerializeField] private int rejoinWindowSeconds = 600; // 10 minutos por defecto

    public string LastSessionId => PlayerPrefs.GetString(KEY_LAST_SESSION_ID, "");
    public string LastSessionName => PlayerPrefs.GetString(KEY_LAST_SESSION_NAME, "");

    /// <summary>True si hay una sesión guardada que está dentro de la ventana de rejoin.</summary>
    public bool HasRecentSession
    {
        get
        {
            if (string.IsNullOrEmpty(LastSessionId)) return false;
            long ticks = long.Parse(PlayerPrefs.GetString(KEY_LAST_SESSION_TIME, "0"));
            if (ticks == 0) return false;

            DateTime savedAt = new DateTime(ticks, DateTimeKind.Utc);
            return (DateTime.UtcNow - savedAt).TotalSeconds < rejoinWindowSeconds;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.OnSessionJoined += SaveCurrentSession;
            SessionManager.Instance.OnSessionLeft += ClearSession;
        }
    }

    private void OnDisable()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.OnSessionJoined -= SaveCurrentSession;
            SessionManager.Instance.OnSessionLeft -= ClearSession;
        }
    }

    /// <summary>Guarda la sesión actual del SessionManager (llamado automáticamente).</summary>
    private void SaveCurrentSession()
    {
        var s = SessionManager.Instance.CurrentSession;
        if (s == null) return;

        PlayerPrefs.SetString(KEY_LAST_SESSION_ID, s.Id);
        PlayerPrefs.SetString(KEY_LAST_SESSION_NAME, s.Name);
        PlayerPrefs.SetString(KEY_LAST_SESSION_TIME, DateTime.UtcNow.Ticks.ToString());
        PlayerPrefs.Save();

        Debug.Log($"[RejoinManager] Guardado para rejoin: {s.Name} ({s.Id})");
    }

    /// <summary>Borra la sesión guardada (cuando el jugador sale voluntariamente).</summary>
    public void ClearSession()
    {
        PlayerPrefs.DeleteKey(KEY_LAST_SESSION_ID);
        PlayerPrefs.DeleteKey(KEY_LAST_SESSION_NAME);
        PlayerPrefs.DeleteKey(KEY_LAST_SESSION_TIME);
        PlayerPrefs.Save();

        Debug.Log("[RejoinManager] Sesión guardada borrada.");
    }

    /// <summary>
    /// Intenta reconectar a la última sesión. Retorna true si funcionó.
    /// Si la sesión ya no existe, retorna false y limpia el dato guardado.
    /// </summary>
    public async Task<bool> TryRejoin()
    {
        if (!HasRecentSession)
        {
            Debug.Log("[RejoinManager] No hay sesión reciente para reconectar.");
            return false;
        }

        string sessionId = LastSessionId;
        Debug.Log($"[RejoinManager] Intentando reconectar a {sessionId}...");

        bool ok = await SessionManager.Instance.JoinSessionById(sessionId);

        if (!ok)
        {
            Debug.LogWarning("[RejoinManager] La sesión ya no existe o no se puede acceder. Limpiando.");
            ClearSession();
        }

        return ok;
    }
}