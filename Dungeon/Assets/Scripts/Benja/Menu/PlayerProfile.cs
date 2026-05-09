using Unity.Services.Authentication;
using UnityEngine;

/// <summary>
/// Identidad del jugador local. Combina:
///   - PlayerId: el ID único de Unity Authentication (persistente entre sesiones)
///   - Name: nombre elegido por el jugador, guardado en PlayerPrefs
///
/// El PlayerId solo está disponible DESPUÉS de SessionManager.InitializeAsync().
/// El Name está disponible siempre desde el primer arranque.
/// </summary>
public static class PlayerProfile
{
    private const string KEY_NAME = "player_profile_name";
    private const string KEY_FIRST_LAUNCH_DONE = "player_profile_first_launch_done";

    /// <summary>
    /// Nombre que el jugador eligió. Persiste en PlayerPrefs.
    /// Se usa en lobby, in-game, etc.
    /// </summary>
    public static string Name
    {
        get => PlayerPrefs.GetString(KEY_NAME, "");
        set
        {
            PlayerPrefs.SetString(KEY_NAME, value);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// ID único del jugador, dado por Unity Authentication.
    /// Solo válido después de inicializar Unity Services + Auth.
    /// Persiste entre sesiones automáticamente (vinculado a la cuenta anónima de Unity).
    /// </summary>
    public static string PlayerId
    {
        get
        {
            if (AuthenticationService.Instance == null) return null;
            if (!AuthenticationService.Instance.IsSignedIn) return null;
            return AuthenticationService.Instance.PlayerId;
        }
    }

    /// <summary>
    /// True si el jugador ya completó la pantalla de bienvenida (introdujo nombre).
    /// </summary>
    public static bool HasCompletedFirstLaunch
    {
        get => PlayerPrefs.GetInt(KEY_FIRST_LAUNCH_DONE, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(KEY_FIRST_LAUNCH_DONE, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    /// <summary>True si el nombre está listo para ser usado.</summary>
    public static bool HasName => !string.IsNullOrWhiteSpace(Name);

    /// <summary>Reset (para debug / botón "borrar perfil").</summary>
    public static void Clear()
    {
        PlayerPrefs.DeleteKey(KEY_NAME);
        PlayerPrefs.DeleteKey(KEY_FIRST_LAUNCH_DONE);
        PlayerPrefs.Save();
    }
}