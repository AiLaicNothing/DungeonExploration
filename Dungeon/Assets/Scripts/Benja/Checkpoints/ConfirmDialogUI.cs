using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Diálogo de confirmación Sí/No reutilizable.
/// Cualquier sistema puede llamar Show() con un mensaje y un callback.
///
/// Setup:
///   - GameObject en HUD Canvas con este script
///   - panelRoot: GameObject que se activa/desactiva
///   - titleText, messageText: TMP_Text del diálogo
///   - yesButton, noButton: botones Sí y No
/// </summary>
public class ConfirmDialogUI : MonoBehaviour
{
    public static ConfirmDialogUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private TMP_Text yesButtonLabel;
    [SerializeField] private TMP_Text noButtonLabel;

    private System.Action _onYes;
    private System.Action _onNo;

    void Awake()
    {
        Instance = this;
        if (panelRoot != null) panelRoot.SetActive(false);

        if (yesButton != null) yesButton.onClick.AddListener(OnYesClicked);
        if (noButton != null) noButton.onClick.AddListener(OnNoClicked);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (yesButton != null) yesButton.onClick.RemoveListener(OnYesClicked);
        if (noButton != null) noButton.onClick.RemoveListener(OnNoClicked);
    }

    /// <summary>
    /// Muestra el diálogo. Las acciones se llaman según qué botón pulse el usuario.
    /// </summary>
    public void Show(string title, string message,
                     System.Action onYes, System.Action onNo = null,
                     string yesLabel = "Sí", string noLabel = "No")
    {
        if (titleText != null) titleText.text = title;
        if (messageText != null) messageText.text = message;
        if (yesButtonLabel != null) yesButtonLabel.text = yesLabel;
        if (noButtonLabel != null) noButtonLabel.text = noLabel;

        _onYes = onYes;
        _onNo = onNo;

        if (panelRoot != null) panelRoot.SetActive(true);

        // Registramos en UIBlockingManager para que bloquee input mientras está abierto
        if (UIBlockingManager.Instance != null)
            UIBlockingManager.Instance.Register(this);
    }

    private void OnYesClicked()
    {
        var action = _onYes;
        Hide();
        action?.Invoke();
    }

    private void OnNoClicked()
    {
        var action = _onNo;
        Hide();
        action?.Invoke();
    }

    private void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        _onYes = null;
        _onNo = null;

        if (UIBlockingManager.Instance != null)
            UIBlockingManager.Instance.Unregister(this);
    }
}