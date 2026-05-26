using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CheckpointSkillUI : MonoBehaviour
{
    public static CheckpointSkillUI Instance;

    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    private Skill selectedSkill;

    [Header("Content")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject skillButtonPrefab;

    [Header("UI Info")]
    [SerializeField] private TMP_Text equippedText;
    [SerializeField] private TMP_Text selectedSkillText;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button unequipButton;

    private PlayerSkillInventory inventory;

    private List<SkillButtonUI> buttons = new();

    private void Awake()
    {
        Instance = this;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (equipButton != null)
            equipButton.onClick.AddListener(EquipSelectedSkill);

        if (unequipButton != null)
            unequipButton.onClick.AddListener(UnequipSelectedSkill);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void OnDisable()
    {
        if (equipButton != null)
            equipButton.onClick.RemoveListener(EquipSelectedSkill);

        if (unequipButton != null)
            unequipButton.onClick.RemoveListener(UnequipSelectedSkill);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    public void Open()
    {
        if (LocalPlayer.Controller == null)
            return;

        inventory = LocalPlayer.Controller.GetComponent<PlayerSkillInventory>();

        if (inventory == null)
            return;

        panelRoot.SetActive(true);

        selectedSkill = null;

        if (selectedSkillText != null)
            selectedSkillText.text = "Selected: None";

        Refresh();
    }

    public void Close()
    {
        selectedSkill = null;

        if (selectedSkillText != null)
            selectedSkillText.text = "";

        panelRoot.SetActive(false);
    }

    private void Refresh()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        buttons.Clear();

        int selectedCharacter =
            PlayerSessionData.local.SelectedCharacter.Value;

        var unlockedSkills =
            inventory.GetUnlockedSkillsForCurrentCharacter(selectedCharacter);

        foreach (Skill skill in unlockedSkills)
        {
            GameObject obj =
                Instantiate(skillButtonPrefab, contentParent);

            SkillButtonUI buttonUI =
                obj.GetComponent<SkillButtonUI>();

            buttonUI.Setup(skill, this);

            buttons.Add(buttonUI);
        }

        RefreshEquippedText();
    }

    public void SelectSkill(Skill skill)
    {
        selectedSkill = skill;

        if (selectedSkillText != null)
            selectedSkillText.text = $"Selected: {skill.skillName}";

        foreach (var btn in buttons)
        {
            btn.SetSelected(btn.GetSkill() == skill);
        }
    }

    // =========================================================
    // EQUIP
    // =========================================================

    public void EquipSelectedSkill()
    {
        if (selectedSkill == null || inventory == null)
            return;

        if (inventory.IsEquipped(selectedSkill))
        {
            Debug.Log("Ya está equipada.");
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            if (inventory.GetEquippedSkill(i) == null)
            {
                inventory.EquipSkill(selectedSkill, i);
                RefreshEquippedText();
                return;
            }
        }

        Debug.Log("No empty slots.");
    }

    // =========================================================
    // UNEQUIP
    // =========================================================

    public void UnequipSelectedSkill()
    {
        if (selectedSkill == null || inventory == null)
            return;

        inventory.UnequipSkill(selectedSkill);
        RefreshEquippedText();
    }

    // =========================================================
    // UI
    // =========================================================

    private void RefreshEquippedText()
    {
        equippedText.text = "";

        for (int i = 0; i < 4; i++)
        {
            Skill skill = inventory.GetEquippedSkill(i);

            string name = skill == null ? "Empty" : skill.skillName;

            equippedText.text += $"Slot {i + 1}: {name}\n";
        }
    }
}