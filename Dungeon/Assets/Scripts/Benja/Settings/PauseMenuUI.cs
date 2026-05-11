using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
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

    private PlayerInput playerInput;
    private InputAction pauseAction;

    void Awake()
    {
        Instance = this;
    }

    private IEnumerator Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (multiplayerWarningText != null)
            multiplayerWarningText.text = multiplayerWarningMessage;

        // Esperar hasta que exista el player local
        yield return new WaitUntil(() => LocalPlayer.Controller != null);

        SubscribeToPauseAction();
    }

    void OnDestroy()
    {
        if (pauseAction != null)
            pauseAction.performed -= OnPausePerformed;

        if (Instance == this)
            Instance = null;

        Time.timeScale = 1f;
    }

    private void SubscribeToPauseAction()
    {
        if (LocalPlayer.Controller == null)
        {
            Debug.LogWarning("LocalPlayer.Controller es null.");
            return;
        }

        playerInput = LocalPlayer.Controller.GetComponent<PlayerInput>();

        if (playerInput == null)
        {
            Debug.LogWarning("No se encontró PlayerInput.");
            return;
        }

        pauseAction = playerInput.actions["Pause"];

        if (pauseAction == null)
        {
            Debug.LogWarning("No existe la acción 'Pause'.");
            return;
        }

        pauseAction.performed += OnPausePerformed;
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        Toggle();
    }

    public void Toggle()
    {
        if (_isOpen)
            Close();
        else
            Open();
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
        bool isInMultiplayer =
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening;

        if (pausePanel != null)
            pausePanel.SetActive(_isOpen);

        if (titleText != null)
            titleText.text = isInMultiplayer
                ? multiplayerTitle
                : singleplayerTitle;

        if (multiplayerWarningRoot != null)
            multiplayerWarningRoot.SetActive(_isOpen && isInMultiplayer);

        // Solo pausa tiempo en singleplayer
        if (!isInMultiplayer)
            Time.timeScale = _isOpen ? 0f : 1f;
        else
            Time.timeScale = 1f;

        // Cursor
        Cursor.visible = _isOpen;

        Cursor.lockState = _isOpen
            ? CursorLockMode.None
            : CursorLockMode.Locked;
    }
}