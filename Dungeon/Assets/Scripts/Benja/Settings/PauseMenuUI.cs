using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class PauseMenuUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject pausePanel;

    [Header("Botón Continuar (opcional, dentro del panel)")]
    [SerializeField] private Button resumeButton;

    private bool _isPaused = false;

    void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);

        // Cursor siempre visible
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (resumeButton != null) resumeButton.onClick.AddListener(TogglePause);
    }

    /// <summary>Conectar al evento del Action "Pause" del PlayerInput.</summary>
    public void OnPause(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        TogglePause();
    }

    public void TogglePause()
    {
        _isPaused = !_isPaused;
        if (pausePanel != null) pausePanel.SetActive(_isPaused);
        Time.timeScale = _isPaused ? 0f : 1f;
    }
}
