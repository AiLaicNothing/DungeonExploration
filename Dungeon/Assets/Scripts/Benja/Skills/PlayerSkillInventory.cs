using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Inventario de skills desbloqueadas + equipadas.
/// Basado en IDs (compatible con Netcode).
/// </summary>
public class PlayerSkillInventory : NetworkBehaviour
{
    [Header("All Skills Database")]
    [SerializeField]
    private List<Skill> allSkills = new();
    public System.Action OnSkillsChanged;
    [Header("Config")]
    [SerializeField]
    private int maxSkillSlots = 4;

    // =========================================================
    // UNLOCKED
    // =========================================================

    private readonly HashSet<string> unlockedSkills = new();

    public IReadOnlyCollection<string> UnlockedSkills => unlockedSkills;

    // =========================================================
    // EQUIPPED
    // =========================================================

    private string[] equippedSkillIds;

    private void Awake()
    {
        equippedSkillIds = new string[maxSkillSlots];
    }

    // =========================================================
    // HELPERS
    // =========================================================

    public Skill GetSkillById(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        return allSkills.Find(s => s != null && s.skillId == id);
    }

    // =========================================================
    // CHECKS
    // =========================================================

    public bool HasSkill(Skill skill)
    {
        return skill != null && unlockedSkills.Contains(skill.skillId);
    }

    public bool IsEquipped(Skill skill)
    {
        if (skill == null)
            return false;

        for (int i = 0; i < maxSkillSlots; i++)
        {
            if (equippedSkillIds[i] == skill.skillId)
                return true;
        }

        return false;
    }

    // =========================================================
    // UNLOCK
    // =========================================================

    public bool UnlockSkill(Skill skill)
    {
        if (skill == null || string.IsNullOrEmpty(skill.skillId))
            return false;

        if (unlockedSkills.Contains(skill.skillId))
            return false;

        unlockedSkills.Add(skill.skillId);

        Debug.Log($"[SkillInventory] Unlocked: {skill.skillName}");

        return true;
    }

    // =========================================================
    // EQUIP
    // =========================================================

    public bool EquipSkill(Skill skill, int slotIndex)
    {
        if (skill == null)
            return false;

        if (!HasSkill(skill))
            return false;

        if (slotIndex < 0 || slotIndex >= maxSkillSlots)
            return false;

        equippedSkillIds[slotIndex] = skill.skillId;

        OnSkillsChanged?.Invoke(); // 👈 IMPORTANTE

        return true;
    }

    public void UnequipSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSkillSlots)
            return;

        equippedSkillIds[slotIndex] = null;

        OnSkillsChanged?.Invoke(); // 👈 IMPORTANTE
    }

    public void UnequipSkill(Skill skill)
    {
        if (skill == null)
            return;

        for (int i = 0; i < maxSkillSlots; i++)
        {
            if (equippedSkillIds[i] == skill.skillId)
            {
                equippedSkillIds[i] = null;
                return;
            }
        }
    }

    // =========================================================
    // GET EQUIPPED
    // =========================================================

    public Skill GetEquippedSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSkillSlots)
            return null;

        return GetSkillById(equippedSkillIds[slotIndex]);
    }

    // =========================================================
    // SAVE / LOAD
    // =========================================================

    public string[] CaptureEquippedSkillIds()
    {
        return (string[])equippedSkillIds.Clone();
    }

    public void RestoreEquippedSkills(string[] ids)
    {
        equippedSkillIds = new string[maxSkillSlots];

        if (ids == null)
            return;

        for (int i = 0; i < ids.Length && i < maxSkillSlots; i++)
            equippedSkillIds[i] = ids[i];
    }

    public void RestoreUnlockedSkills(List<string> savedSkills)
    {
        unlockedSkills.Clear();

        if (savedSkills == null)
            return;

        foreach (var id in savedSkills)
        {
            if (!string.IsNullOrEmpty(id))
                unlockedSkills.Add(id);
        }
    }
    public List<Skill> GetUnlockedSkillsForCurrentCharacter(int selectedCharacter)
    {
        CharacterType currentCharacter = (CharacterType)selectedCharacter;

        List<Skill> result = new();

        foreach (var skill in allSkills)
        {
            if (skill == null)
                continue;

            if (!unlockedSkills.Contains(skill.skillId))
                continue;

            if (skill.ownerCharacter != currentCharacter)
                continue;

            result.Add(skill);
        }

        return result;
    }
    public List<string> CaptureUnlockedSkills()
    {
        return new List<string>(unlockedSkills);
    }
}