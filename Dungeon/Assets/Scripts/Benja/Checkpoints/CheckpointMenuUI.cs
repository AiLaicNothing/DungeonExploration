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

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (UIBlockingManager.Instance != null)
            UIBlockingManager.Instance.Register(this);
    }

    public void Close()
    {
        if (UIBlockingManager.Instance != null)
            UIBlockingManager.Instance.Unregister(this);

        if (panelRoot != null)
            panelRoot.SetActive(false);

        currentCheckpointName = string.Empty;

        if (InteractionUI.Instance != null)
            InteractionUI.Instance.ShowUI();
    }
    // =========================================================
    // BUTTONS
    // =========================================================

    private void OnTeleportClicked()
    {
        Close();

        if (teleporterPanel != null)
            teleporterPanel.Open();
        else
            Debug.LogWarning("[CheckpointMenuUI] Missing TeleporterPanel.");
    }

    private void OnUpgradeClicked()
    {
        Close();

        if (upgradePanel != null)
            upgradePanel.Open();
        else
            Debug.LogWarning("[CheckpointMenuUI] Missing UpgradePanel.");
    }

    private void OnSkillsClicked()
    {
        Close(); // 👈 FIX IMPORTANTE

        if (skillPanel != null)
            skillPanel.Open();
        else
            Debug.LogWarning("[CheckpointMenuUI] Missing SkillPanel.");
    }
}