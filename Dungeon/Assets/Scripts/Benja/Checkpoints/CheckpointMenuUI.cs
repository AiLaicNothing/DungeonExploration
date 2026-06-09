using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel principal del checkpoint.
/// Desde aquí puedes:
/// - Teletransportarte
/// - Mejorar stats
/// - Cambiar skills
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
    [SerializeField] private Button skillsButton;
    [SerializeField] private Button closeButton;

    [Header("Panels")]
    [SerializeField] private TeleporterPanelUI teleporterPanel;

    [SerializeField] private CheckpointUpgradeUI upgradePanel;

    [SerializeField] private CheckpointSkillUI skillPanel;

    private string currentCheckpointName;

    public bool IsOpen =>
        panelRoot != null &&
        panelRoot.activeSelf;

    private void Awake()
    {
        Instance = this;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        if (teleportButton != null)
            teleportButton.onClick.AddListener(OnTeleportClicked);

        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnUpgradeClicked);

        if (skillsButton != null)
            skillsButton.onClick.AddListener(OnSkillsClicked);

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }
    }

    private void OnDisable()
    {
        if (teleportButton != null)
            teleportButton.onClick.RemoveListener(OnTeleportClicked);

        if (upgradeButton != null)
            upgradeButton.onClick.RemoveListener(OnUpgradeClicked);

        if (skillsButton != null)
            skillsButton.onClick.RemoveListener(OnSkillsClicked);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    public void Open(string checkpointName)
    {
        currentCheckpointName = checkpointName;

        if (checkpointNameText != null)
        {
            checkpointNameText.text =
                string.IsNullOrEmpty(checkpointName)
                ? "Checkpoint"
                : checkpointName;
        }

        panelRoot?.SetActive(true);

        InteractionUI.Instance?.HideUI();

        UIBlockingManager.Instance?.Register(this);
    }
    public void Reopen()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    private void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }



    public void Close()
    {
        UIBlockingManager.Instance?.Unregister(this);

        Hide();

        currentCheckpointName = string.Empty;

        if (!UIBlockingManager.IsAnyUIOpen)
            InteractionUI.Instance?.ShowUI();
    }
    // =========================================================
    // BUTTONS
    // =========================================================

    private void OnTeleportClicked()
    {
        if (teleporterPanel != null)
            teleporterPanel.Open();
        else
            Debug.LogWarning("[CheckpointMenuUI] Missing TeleporterPanel.");

        Hide();
    }

    private void OnUpgradeClicked()
    {
        if (upgradePanel != null)
            upgradePanel.Open();

        Hide();
    }

    private void OnSkillsClicked()
    {
        Hide();

        if (skillPanel != null)
            skillPanel.Open();
        else
            Debug.LogWarning("[CheckpointMenuUI] Missing SkillPanel.");
    }
}