using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel intermedio que aparece al interactuar con un checkpoint.
/// Da al jugador la opción de elegir entre Teletransporte o Mejorar Estadísticas.
///
/// Flow:
///   - Open() abre este menú
///   - El usuario pulsa "Teletransporte" → cierra este, abre TeleporterPanelUI
///   - El usuario pulsa "Mejorar" → cierra este, abre CheckpointUpgradeUI
///   - El usuario pulsa "Cerrar" → todo se cierra
///
/// Va en el HUD Canvas como un panel oculto al inicio.
/// </summary>
public class CheckpointMenuUI : MonoBehaviour
{
    public static CheckpointMenuUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text checkpointNameText;

    [Header("Botones")]
    [SerializeField] private Button teleportButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button closeButton;

    [Header("Paneles asociados")]
    [Tooltip("Panel de teletransporte (con script TeleporterPanelUI).")]
    [SerializeField] private TeleporterPanelUI teleporterPanel;

    [Tooltip("Panel de mejora de stats (con script CheckpointUpgradeUI).")]
    [SerializeField] private CheckpointUpgradeUI upgradePanel;

    private string _currentCheckpointName;

    // ── Estado del cursor antes de abrir (para restaurar al cerrar) ──
    private CursorLockMode _previousCursorLock;
    private bool _previousCursorVisible;
    private bool _cursorStateSaved;

    void Awake()
    {
        Instance = this;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void OnEnable()
    {
        if (teleportButton != null) teleportButton.onClick.AddListener(OnTeleportClicked);
        if (upgradeButton != null) upgradeButton.onClick.AddListener(OnUpgradeClicked);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    void OnDisable()
    {
        if (teleportButton != null) teleportButton.onClick.RemoveListener(OnTeleportClicked);
        if (upgradeButton != null) upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Abre el menú con el nombre del checkpoint en cuestión.
    /// Libera el cursor para que el jugador pueda clicar los botones.
    /// </summary>
    public void Open(string checkpointName)
    {
        _currentCheckpointName = checkpointName;

        if (checkpointNameText != null)
            checkpointNameText.text = string.IsNullOrEmpty(checkpointName) ? "Checkpoint" : checkpointName;

        if (panelRoot != null) panelRoot.SetActive(true);

        if (UIBlockingManager.Instance != null)
            UIBlockingManager.Instance.Register(this);
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        _currentCheckpointName = null;

        if (UIBlockingManager.Instance != null)
            UIBlockingManager.Instance.Unregister(this);
    }

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    // ── Cursor handling (deprecated, ahora lo hace UIBlockingManager) ────
    private void SaveCursorStateAndUnlock() { /* manejado por UIBlockingManager */ }
    private void RestoreCursorState() { /* manejado por UIBlockingManager */ }

    // ── Acciones ─────────────────────────────────────────────────────
    private void OnTeleportClicked()
    {
        // Cerramos este panel correctamente (incluye Unregister)
        Close();

        if (teleporterPanel != null)
            teleporterPanel.Open();
        else
            Debug.LogWarning("[CheckpointMenuUI] No hay teleporterPanel asignado.");
    }

    private void OnUpgradeClicked()
    {
        // Cerramos este panel correctamente (incluye Unregister)
        Close();

        if (upgradePanel != null)
            upgradePanel.Open();
        else
            Debug.LogWarning("[CheckpointMenuUI] No hay upgradePanel asignado.");
    }
}