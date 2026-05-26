using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillButtonUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private Image icon;

    private Skill skill;
    private CheckpointSkillUI ownerUI;

    public void Setup(Skill targetSkill, CheckpointSkillUI ui)
    {
        skill = targetSkill;
        ownerUI = ui;

        if (skillNameText != null)
            skillNameText.text = skill.skillName;
    }

    public void OnClick()
    {
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
        if (icon != null)
            icon.color = value ? Color.yellow : Color.white;
    }
}