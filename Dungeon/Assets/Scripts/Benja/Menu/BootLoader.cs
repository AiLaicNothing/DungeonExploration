using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Primera escena del juego. Inicializa Unity Services + Auth y luego decide
/// a qué escena ir:
///   - Si el jugador no tiene nombre → escena Welcome (pantalla "escribe tu nombre")
///   - Si ya tiene → MainMenu directo
/// </summary>
public class BootLoader : MonoBehaviour
{
    [Header("Refs UI")]
    [SerializeField] private TMP_Text statusText;

    [Header("Escenas siguientes")]
    [SerializeField] private string welcomeScene = "01_Welcome";
    [SerializeField] private string mainMenuScene = "02_MainMenu";

    private IEnumerator Start()
    {
        SetStatus("Inicializando servicios...");

        var task = SessionManager.Instance.InitializeAsync();
        while (!task.IsCompleted) yield return null;

        if (task.IsFaulted)
        {
            SetStatus($"Error: {task.Exception?.GetBaseException().Message}");
            yield break;
        }

        SetStatus($"Listo. PlayerId: {PlayerProfile.PlayerId}");
        yield return new WaitForSeconds(0.3f);

        if (!PlayerProfile.HasName)
        {
            Debug.Log("[BootLoader] Primer arranque, vamos a Welcome.");
            SceneManager.LoadScene(welcomeScene);
        }
        else
        {
            Debug.Log($"[BootLoader] Bienvenido de vuelta, {PlayerProfile.Name}. Vamos al menú.");
            SceneManager.LoadScene(mainMenuScene);
        }
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log($"[BootLoader] {msg}");
    }
}