using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CheckpointSkillUI : MonoBehaviour
{
    public static CheckpointSkillUI Instance;
    public bool IsOpen =>
    panelRoot != null &&
    panelRoot.activeSelf;

    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Slot Panels")]
    [SerializeField] private Button[] slotButtons = new Button[4];
    [SerializeField] private Image[] slotIcons = new Image[4];
    [SerializeField] private GameObject[] slotSelectedMarker = new GameObject[4];

    [Header("Available Skills Panel")]
    [SerializeField] private Transform availableSkillsParent;
    [SerializeField] private GameObject skillButtonPrefab;

    [Header("Info Panel")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text infoText;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button unequipButton;

    private PlayerSkillInventory inventory;
    private Skill selectedSkill;
    private int selectedSlotIndex = -1;

    private readonly List<SkillButtonUI> buttons = new();

    private void Awake()
    {
        Instance = this;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        SetupSlotButtons();
    }

    private void OnEnable()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (unequipButton != null)
            unequipButton.onClick.AddListener(UnequipSelectedSlot);
    }

    private void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (unequipButton != null)
            unequipButton.onClick.RemoveListener(UnequipSelectedSlot);

        if (inventory != null)
            inventory.OnSkillsChanged -= Refresh;

        // Safety cleanup
        if (UIBlockingManager.Instance != null)
            UIBlockingManager.Instance.Unregister(this);
    }

    // =========================================================
    // SETUP
    // =========================================================

    private void SetupSlotButtons()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int index = i;

            if (slotButtons[i] != null)
            {
                slotButtons[i].onClick.AddListener(() => SelectSlot(index));
            }
        }
    }

    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    public void Open()
    {
        if (LocalPlayer.Controller == null)
            return;

        inventory = LocalPlayer.Controller.GetComponent<PlayerSkillInventory>();

        if (inventory == null)
            return;

        inventory.OnSkillsChanged -= Refresh;
        inventory.OnSkillsChanged += Refresh;

        panelRoot.SetActive(true);

        selectedSkill = null;
        selectedSlotIndex = -1;

        Refresh();

        // Registrar UI abierta
        if (UIBlockingManager.Instance != null)
            UIBlockingManager.Instance.Register(this);
    }

    public void Close()
    {
        selectedSkill = null;
        selectedSlotIndex = -1;

        if (inventory != null)
            inventory.OnSkillsChanged -= Refresh;

        if (UIBlockingManager.Instance != null)
            UIBlockingManager.Instance.Unregister(this);

        panelRoot.SetActive(false);
    }

    // =========================================================
    // REFRESH
    // =========================================================

    private void Refresh()
    {
        if (inventory == null)
            return;

        RefreshSlots();
        RefreshAvailableSkills();
        RefreshInfo();
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < 4; i++)
        {
            Skill skill = inventory.GetEquippedSkill(i);

            if (slotIcons != null && i < slotIcons.Length && slotIcons[i] != null)
            {
                if (skill != null && skill.skillSprite != null)
                {
                    slotIcons[i].sprite = skill.skillSprite;
                    slotIcons[i].enabled = true;
                }
                else
                {
                    slotIcons[i].sprite = null;
                    slotIcons[i].enabled = false;
                }
            }

            if (slotSelectedMarker != null &&
                i < slotSelectedMarker.Length &&
                slotSelectedMarker[i] != null)
            {
                slotSelectedMarker[i].SetActive(i == selectedSlotIndex);
            }
        }
    }

    private void RefreshAvailableSkills()
    {
        if (availableSkillsParent == null || skillButtonPrefab == null)
            return;

        foreach (Transform child in availableSkillsParent)
            Destroy(child.gameObject);

        buttons.Clear();

        int selectedCharacter =
            PlayerSessionData.local != null
                ? PlayerSessionData.local.SelectedCharacter.Value
                : -1;

        var availableSkills =
            inventory.GetAvailableSkillsForCurrentCharacter(selectedCharacter);

        foreach (Skill skill in availableSkills)
        {
            GameObject obj = Instantiate(skillButtonPrefab, availableSkillsParent);

            // NEW: keep prefab scale clean for GridLayoutGroup
            obj.transform.localScale = Vector3.one;

            SkillButtonUI buttonUI = obj.GetComponent<SkillButtonUI>();
            if (buttonUI != null)
            {
                buttonUI.Setup(skill, this);
                buttonUI.SetSelected(selectedSkill == skill);
                buttons.Add(buttonUI);
            }
        }
    }

    private void RefreshInfo()
    {
        if (inventory == null)
            return;

        // ==============================
        // SELECTED SKILL
        // ==============================
        if (selectedSkill != null)
        {
            if (nameText != null)
                nameText.text = selectedSkill.skillName;

            if (infoText != null)
            {
                infoText.text =
                    $"Cost: {selectedSkill.cost}\n" +
                    $"Cooldown: {selectedSkill.cooldown}\n\n" +
                    $"Select a slot to equip.";
            }

            return;
        }

        // ==============================
        // SELECTED SLOT
        // ==============================
        if (selectedSlotIndex >= 0)
        {
            Skill slotSkill = inventory.GetEquippedSkill(selectedSlotIndex);

            if (slotSkill != null)
            {
                if (nameText != null)
                    nameText.text = slotSkill.skillName;

                if (infoText != null)
                {
                    infoText.text =
                        $"Cost: {slotSkill.cost}\n" +
                        $"Cooldown: {slotSkill.cooldown}\n\n" +
                        $"Select another slot to swap.";
                }
            }
            else
            {
                if (nameText != null)
                    nameText.text = $"Slot {selectedSlotIndex + 1}";

                if (infoText != null)
                {
                    infoText.text =
                        $"Empty Slot\n\n" +
                        $"Select a skill to equip.";
                }
            }

            return;
        }

        // ==============================
        // NOTHING SELECTED
        // ==============================
        if (nameText != null)
            nameText.text = "Skills";

        if (infoText != null)
            infoText.text = "Select a skill or slot.";
    }

    // =========================================================
    // SELECTION LOGIC
    // =========================================================

    public void SelectSkill(Skill skill)
    {
        if (skill == null || inventory == null)
            return;

        selectedSkill = skill;

        // If a slot is already selected, commit immediately
        if (selectedSlotIndex >= 0)
        {
            TryCommitSelection();
            return;
        }

        RefreshInfo();
        RefreshAvailableSkills();
    }

    public void SelectSlot(int slotIndex)
    {
        if (inventory == null)
            return;

        if (slotIndex < 0 || slotIndex >= 4)
            return;

        // If a skill is already selected, equip it to this slot
        if (selectedSkill != null)
        {
            TryCommitSelection(slotIndex);
            return;
        }

        // If clicking the same slot twice, deselect it
        if (selectedSlotIndex == slotIndex)
        {
            ClearSelection();
            Refresh();
            return;
        }

        // If another slot was already selected, swap both
        if (selectedSlotIndex >= 0)
        {
            // NEW: request swap through the networked inventory
            inventory.RequestSwapSlots(selectedSlotIndex, slotIndex);

            ClearSelection();
            Refresh();
            return;
        }

        // Otherwise just select the slot
        selectedSlotIndex = slotIndex;
        RefreshInfo();
        RefreshSlots();
    }

    private void TryCommitSelection()
    {
        if (selectedSkill == null || selectedSlotIndex < 0)
            return;

        // NEW: request equip through the networked inventory
        inventory.RequestEquipSkillToSlot(selectedSkill, selectedSlotIndex);

        ClearSelection();
        Refresh();
    }

    private void TryCommitSelection(int slotIndex)
    {
        selectedSlotIndex = slotIndex;
        TryCommitSelection();
    }

    private void ClearSelection()
    {
        selectedSkill = null;
        selectedSlotIndex = -1;
    }

    // =========================================================
    // UNEQUIP
    // =========================================================

    public void UnequipSelectedSlot()
    {
        if (inventory == null)
            return;

        if (selectedSlotIndex < 0)
            return;

        // NEW: request unequip through the networked inventory
        inventory.RequestUnequipSlot(selectedSlotIndex);

        ClearSelection();
        Refresh();
    }
}