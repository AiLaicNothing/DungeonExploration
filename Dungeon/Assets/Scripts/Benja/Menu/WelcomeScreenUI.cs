using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pantalla "Bienvenido, escribe tu nombre" que aparece la primera vez que
/// el jugador abre el juego (cuando PlayerProfile.HasName == false).
///
/// Setup mínimo de la escena:
/// - Canvas con TMP_InputField (nombre), Button (continuar), TMP_Text (label/error)
/// - GameObject con este script
/// </summary>
public class WelcomeScreenUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private TMP_Text playerIdText;  // opcional, para mostrar el PlayerId

    [Header("Validación")]
    [SerializeField] private int minNameLength = 2;
    [SerializeField] private int maxNameLength = 16;

    [Header("Escena destino")]
    [SerializeField] private string mainMenuScene = "02_MainMenu";

    void Start()
    {
        if (titleText != null)
            titleText.text = "Bienvenido";

        if (errorText != null) errorText.text = "";

        // Si por algún motivo ya hay nombre (test, debug, etc.), prellenar
        if (PlayerProfile.HasName && nameInput != null)
            nameInput.text = PlayerProfile.Name;

        // Mostrar el PlayerId (informativo)
        if (playerIdText != null)
            playerIdText.text = $"ID: {PlayerProfile.PlayerId ?? "(no disponible)"}";

        // Configurar botón
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);

        // Validar en tiempo real para habilitar/deshabilitar el botón
        if (nameInput != null)
            nameInput.onValueChanged.AddListener(_ => UpdateContinueButtonInteractable());

        UpdateContinueButtonInteractable();
    }

    private void UpdateContinueButtonInteractable()
    {
        if (continueButton == null) return;
        string trimmed = nameInput != null ? nameInput.text.Trim() : "";
        continueButton.interactable = trimmed.Length >= minNameLength && trimmed.Length <= maxNameLength;
    }

    private void OnContinueClicked()
    {
        string trimmed = nameInput.text.Trim();

        if (trimmed.Length < minNameLength)
        {
            ShowError($"El nombre debe tener al menos {minNameLength} caracteres.");
            return;
        }
        if (trimmed.Length > maxNameLength)
        {
            ShowError($"El nombre no puede tener más de {maxNameLength} caracteres.");
            return;
        }

        PlayerProfile.Name = trimmed;
        PlayerProfile.HasCompletedFirstLaunch = true;

        Debug.Log($"[WelcomeScreenUI] Perfil creado: nombre='{trimmed}', id={PlayerProfile.PlayerId}");

        SceneManager.LoadScene(mainMenuScene);
    }

    private void ShowError(string msg)
    {
        if (errorText != null) errorText.text = msg;
    }
}