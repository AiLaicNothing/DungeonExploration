using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Inventario de skills desbloqueadas + equipadas.
/// Basado en IDs (compatible con Netcode).
/// </summary>
public class PlayerSkillInventory : NetworkBehaviour
{
    [Header("All Skills Database")]
    [SerializeField] private List<Skill> allSkills = new();

    [Header("Config")]
    [SerializeField] private int maxSkillSlots = 4;

    public System.Action OnSkillsChanged;

    private readonly HashSet<string> unlockedSkills = new();

    public IReadOnlyCollection<string> UnlockedSkills => unlockedSkills;

    private string[] equippedSkillIds;

    private void Awake()
    {
        equippedSkillIds = new string[maxSkillSlots];
    }

    public Skill GetSkillById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        return allSkills.Find(s => s != null && s.skillId == id);
    }

    public bool HasSkillUnlocked(Skill skill)
    {
        return skill != null && unlockedSkills.Contains(skill.skillId);
    }

    public bool HasSkillUnlocked(string skillId)
    {
        return !string.IsNullOrEmpty(skillId) && unlockedSkills.Contains(skillId);
    }

    public int GetEquippedSlotIndex(Skill skill)
    {
        if (skill == null) return -1;

        return GetEquippedSlotIndex(skill.skillId);
    }

    public int GetEquippedSlotIndex(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return -1;

        for (int i = 0; i < maxSkillSlots; i++)
        {
            if (equippedSkillIds[i] == skillId)  return i;
        }

        return -1;
    }

    public int GetEmptySlotIndex()
    {
        for (int i = 0; i < maxSkillSlots; i++)
        {
            if (string.IsNullOrEmpty(equippedSkillIds[i])) return i;
        }

        return -1;
    }

    public bool IsEquipped(Skill skill)
    {
        if (skill == null) return false;

        return GetEquippedSlotIndex(skill) != -1;
    }

    // =========================================================
    // UNLOCK
    // =========================================================

    public bool UnlockSkill(Skill skill)
    {
        if (skill == null || string.IsNullOrEmpty(skill.skillId)) return false;

        if (unlockedSkills.Contains(skill.skillId)) return false;

        unlockedSkills.Add(skill.skillId);

        Debug.Log($"[SkillInventory] Unlocked: {skill.skillName}");

        OnSkillsChanged?.Invoke();
        return true;
    }

    // =========================================================
    // EQUIP / UNEQUIP / SWAP
    // =========================================================

    public bool EquipSkill(Skill skill, int slotIndex)
    {
        if (skill == null) return false;

        if (!HasSkillUnlocked(skill)) return false;

        if (slotIndex < 0 || slotIndex >= maxSkillSlots) return false;

        // Remove this skill from any other slot first to avoid duplicates
        int existingSlot = GetEquippedSlotIndex(skill);
        if (existingSlot >= 0 && existingSlot != slotIndex)
        {
            equippedSkillIds[existingSlot] = null;
        }

        equippedSkillIds[slotIndex] = skill.skillId;

        OnSkillsChanged?.Invoke();
        return true;
    }

    // =========================================================
    // METHOD USED BY UI
    // =========================================================
    public bool TryEquipSkillToSlot(Skill skill, int slotIndex)
    {
        if (skill == null) return false;

        if (!HasSkillUnlocked(skill)) return false;

        if (slotIndex < 0 || slotIndex >= maxSkillSlots) return false;

        int sourceSlot = GetEquippedSlotIndex(skill);
        Skill targetSkill = GetEquippedSkill(slotIndex);

        // Same slot, nothing to do
        if (sourceSlot == slotIndex) return true;

        // Skill already equipped somewhere else
        if (sourceSlot >= 0)
        {
            if (targetSkill == null)
            {
                equippedSkillIds[sourceSlot] = null;
                equippedSkillIds[slotIndex] = skill.skillId;
            }
            else
            {
                equippedSkillIds[sourceSlot] = targetSkill.skillId;
                equippedSkillIds[slotIndex] = skill.skillId;
            }

            OnSkillsChanged?.Invoke();
            return true;
        }

        // Skill not equipped yet
        if (targetSkill == null)
        {
            equippedSkillIds[slotIndex] = skill.skillId;

            OnSkillsChanged?.Invoke();
            return true;
        }

        // Target slot occupied, try to move its skill to an empty slot
        int emptySlot = GetEmptySlotIndex();
        if (emptySlot == -1)
        {
            Debug.LogWarning("[SkillInventory] No empty slots to move the current skill.");
            return false;
        }

        equippedSkillIds[emptySlot] = targetSkill.skillId;
        equippedSkillIds[slotIndex] = skill.skillId;

        OnSkillsChanged?.Invoke();
        return true;
    }

    public void UnequipSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSkillSlots) return;

        equippedSkillIds[slotIndex] = null;

        OnSkillsChanged?.Invoke();
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
                OnSkillsChanged?.Invoke();
                return;
            }
        }
    }

    public void SwapSlots(int slotA, int slotB)
    {
        if (slotA < 0 || slotA >= maxSkillSlots) return;

        if (slotB < 0 || slotB >= maxSkillSlots) return;

        if (slotA == slotB) return;

        (equippedSkillIds[slotA], equippedSkillIds[slotB]) = (equippedSkillIds[slotB], equippedSkillIds[slotA]);

        OnSkillsChanged?.Invoke();
    }

    // =========================================================
    // GET EQUIPPED
    // =========================================================

    public Skill GetEquippedSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSkillSlots) return null;

        return GetSkillById(equippedSkillIds[slotIndex]);
    }

    // =========================================================
    // AVAILABLE SKILLS
    // =========================================================

    public List<Skill> GetAvailableSkillsForCurrentCharacter(int selectedCharacter)
    {
        CharacterType currentCharacter = (CharacterType)selectedCharacter;

        List<Skill> result = new();

        foreach (var skill in allSkills)
        {
            if (skill == null)  continue;

            if (!unlockedSkills.Contains(skill.skillId)) continue;

            if (skill.ownerCharacter != currentCharacter) continue;

            if (IsEquipped(skill)) continue;

            result.Add(skill);
        }

        return result.OrderBy(s => s.skillName).ToList();
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

        if (ids != null)
        {
            for (int i = 0; i < ids.Length && i < maxSkillSlots; i++)
            {
                equippedSkillIds[i] = ids[i];
            }
        }

        OnSkillsChanged?.Invoke();
    }

    public List<string> CaptureUnlockedSkills()
    {
        return new List<string>(unlockedSkills);
    }

    public void RestoreUnlockedSkills(List<string> savedSkills)
    {
        unlockedSkills.Clear();

        if (savedSkills != null)
        {
            foreach (var id in savedSkills)
            {
                if (!string.IsNullOrEmpty(id)) unlockedSkills.Add(id);
            }
        }

        OnSkillsChanged?.Invoke();
    }

    // Optional helper if you want to clear everything
    public void ClearAll()
    {
        unlockedSkills.Clear();

        for (int i = 0; i < maxSkillSlots; i++) equippedSkillIds[i] = null;

        OnSkillsChanged?.Invoke();
    }
}