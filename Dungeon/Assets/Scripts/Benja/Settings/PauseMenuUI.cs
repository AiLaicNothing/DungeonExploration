using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Menú local del jugador. En multiplayer NO pausa el mundo — solo abre las opciones.
/// En singleplayer SÍ congela Time.timeScale.
///
/// Tiene 5 tabs gestionadas por PauseTabSwitcher.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    public static PauseMenuUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private TMP_Text titleText;

    [Header("Títulos según contexto")]
    [SerializeField] private string singleplayerTitle = "Pausa";
    [SerializeField] private string multiplayerTitle = "Menú";

    [Header("Aviso multiplayer")]
    [SerializeField] private GameObject multiplayerWarningRoot;
    [SerializeField] private TMP_Text multiplayerWarningText;
    [SerializeField, TextArea]
    private string multiplayerWarningMessage =
        "El mundo sigue corriendo. No puedes pausar la partida.";

    [Header("Botones cabecera")]
    [SerializeField] private Button closeButton;



    private bool _isOpen = false;
    public bool IsOpen => _isOpen;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (closeButton != null) closeButton.onClick.AddListener(Close);

        if (multiplayerWarningText != null)
            multiplayerWarningText.text = multiplayerWarningMessage;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }

    /// <summary>Conectar al evento del Action "Pause" del PlayerInput.</summary>
    public void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        Toggle();
    }

    public void Toggle()
    {
        if (_isOpen) Close();
        else Open();
    }

    public void Open()
    {
        _isOpen = true;
        ApplyState();
    }

    public void Close()
    {
        _isOpen = false;
        ApplyState();
    }

    private void ApplyState()
    {
        bool isInMultiplayer = NetworkManager.Singleton != null
                            && NetworkManager.Singleton.IsListening;

        if (pausePanel != null) pausePanel.SetActive(_isOpen);

        if (titleText != null)
            titleText.text = isInMultiplayer ? multiplayerTitle : singleplayerTitle;

        if (multiplayerWarningRoot != null)
            multiplayerWarningRoot.SetActive(_isOpen && isInMultiplayer);

        // Tiempo: solo se pausa en singleplayer
        if (!isInMultiplayer)
            Time.timeScale = _isOpen ? 0f : 1f;
        else
            Time.timeScale = 1f;

        // Registrar en UIBlockingManager para bloquear input gameplay
        if (UIBlockingManager.Instance != null)
        {
            if (_isOpen) UIBlockingManager.Instance.Register(this);
            else UIBlockingManager.Instance.Unregister(this);
        }
    }
}