using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillButtonUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image skillIcon;

    [Header("Selection")]
    [SerializeField] private Image selectionFrame;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    [Header("States")]
    [SerializeField] private GameObject equippedMark;

    private Skill skill;
    private CheckpointSkillUI ownerUI;

    public void Setup(Skill targetSkill, CheckpointSkillUI ui, bool isEquipped = false)
    {
        skill = targetSkill;
        ownerUI = ui;

        if (skillIcon != null)
            skillIcon.sprite = skill.skillSprite;

        if (equippedMark != null)
            equippedMark.SetActive(isEquipped);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        SetSelected(false);
    }

    public void OnClick()
    {
        Debug.Log("CLICKED SKILL");

        if (skill == null || ownerUI == null)
            return;

        ownerUI.SelectSkill(skill);
    }

    public Skill GetSkill()
    {
        return skill;
    }

    public void SetSelected(bool value)
    {
        if (selectionFrame != null)
        {
            selectionFrame.color =
                value ? selectedColor : normalColor;
        }
    }

    public void SetEquipped(bool value)
    {
        if (equippedMark != null)
            equippedMark.SetActive(value);
    }
}