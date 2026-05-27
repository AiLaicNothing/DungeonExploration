using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
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

    public event Action OnSkillsChanged;

    // SERVER-AUTHORITATIVE NETWORK STATE
    private NetworkList<FixedString64Bytes> unlockedSkillIds;
    private NetworkList<FixedString64Bytes> equippedSkillIds;

    private void Awake()
    {
        unlockedSkillIds = new NetworkList<FixedString64Bytes>();
        equippedSkillIds = new NetworkList<FixedString64Bytes>();
    }


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        unlockedSkillIds.OnListChanged += HandleNetworkListChanged;
        equippedSkillIds.OnListChanged += HandleNetworkListChanged;

        if (IsServer)
        {
            EnsureEquippedSlotsInitialized();
        }

        OnSkillsChanged?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        unlockedSkillIds.OnListChanged -= HandleNetworkListChanged;
        equippedSkillIds.OnListChanged -= HandleNetworkListChanged;
    }

    private void HandleNetworkListChanged(NetworkListEvent<FixedString64Bytes> _)
    {
        OnSkillsChanged?.Invoke();
    }

    private void EnsureEquippedSlotsInitialized()
    {
        while (equippedSkillIds.Count < maxSkillSlots)
        {
            equippedSkillIds.Add(default);
        }
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

    public bool HasSkillUnlocked(Skill skill)
    {
        return skill != null && HasSkillUnlocked(skill.skillId);
    }

    public bool HasSkillUnlocked(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
            return false;

        return unlockedSkillIds.Contains(new FixedString64Bytes(skillId));
    }

    public bool IsEquipped(Skill skill)
    {
        if (skill == null)
            return false;

        return GetEquippedSlotIndex(skill) != -1;
    }

    public int GetEquippedSlotIndex(Skill skill)
    {
        if (skill == null)
            return -1;

        return GetEquippedSlotIndex(skill.skillId);
    }

    public int GetEquippedSlotIndex(string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
            return -1;

        var id = new FixedString64Bytes(skillId);

        for (int i = 0; i < equippedSkillIds.Count; i++)
        {
            if (equippedSkillIds[i] == id)
                return i;
        }

        return -1;
    }

    public int GetEmptySlotIndex()
    {
        for (int i = 0; i < equippedSkillIds.Count; i++)
        {
            if (equippedSkillIds[i].Length == 0)
                return i;
        }

        return -1;
    }

    // =========================================================
    // REQUESTS (CLIENT -> SERVER)
    // =========================================================

    public void RequestUnlockSkill(Skill skill)
    {
        if (skill == null || string.IsNullOrEmpty(skill.skillId))
            return;

        if (IsServer)
        {
            UnlockSkillServer(skill.skillId);
            return;
        }

        RequestUnlockSkillServerRpc(skill.skillId);
    }

    public void RequestEquipSkillToSlot(Skill skill, int slotIndex)
    {
        if (skill == null || string.IsNullOrEmpty(skill.skillId))
            return;

        if (IsServer)
        {
            EquipSkillToSlotServer(skill.skillId, slotIndex);
            return;
        }

        RequestEquipSkillToSlotServerRpc(skill.skillId, slotIndex);
    }

    public void RequestSwapSlots(int slotA, int slotB)
    {
        if (IsServer)
        {
            SwapSlotsServer(slotA, slotB);
            return;
        }

        RequestSwapSlotsServerRpc(slotA, slotB);
    }

    public void RequestUnequipSlot(int slotIndex)
    {
        if (IsServer)
        {
            UnequipSlotServer(slotIndex);
            return;
        }

        RequestUnequipSlotServerRpc(slotIndex);
    }

    [ServerRpc]
    private void RequestUnlockSkillServerRpc(string skillId)
    {
        UnlockSkillServer(skillId);
    }

    [ServerRpc]
    private void RequestEquipSkillToSlotServerRpc(string skillId, int slotIndex)
    {
        EquipSkillToSlotServer(skillId, slotIndex);
    }

    [ServerRpc]
    private void RequestSwapSlotsServerRpc(int slotA, int slotB)
    {
        SwapSlotsServer(slotA, slotB);
    }

    [ServerRpc]
    private void RequestUnequipSlotServerRpc(int slotIndex)
    {
        UnequipSlotServer(slotIndex);
    }

    // =========================================================
    // SERVER LOGIC
    // =========================================================

    private void UnlockSkillServer(string skillId)
    {
        if (!IsServer)
            return;

        if (string.IsNullOrEmpty(skillId))
            return;

        var id = new FixedString64Bytes(skillId);

        if (unlockedSkillIds.Contains(id))
            return;

        unlockedSkillIds.Add(id);

        OnSkillsChanged?.Invoke();
        Debug.Log($"[SkillInventory] Unlocked: {skillId}");
    }

    private bool EquipSkillToSlotServer(string skillId, int slotIndex)
    {
        if (!IsServer)
            return false;

        if (string.IsNullOrEmpty(skillId))
            return false;

        if (slotIndex < 0 || slotIndex >= maxSkillSlots)
            return false;

        EnsureEquippedSlotsInitialized();

        if (!HasSkillUnlocked(skillId))
            return false;

        var skillIdFS = new FixedString64Bytes(skillId);

        int sourceSlot = GetEquippedSlotIndex(skillId);
        FixedString64Bytes targetSkillId = equippedSkillIds[slotIndex];

        // Same slot
        if (sourceSlot == slotIndex)
            return true;

        // Skill already equipped somewhere else
        if (sourceSlot >= 0)
        {
            if (targetSkillId.Length == 0)
            {
                equippedSkillIds[sourceSlot] = default;
                equippedSkillIds[slotIndex] = skillIdFS;
            }
            else
            {
                equippedSkillIds[sourceSlot] = targetSkillId;
                equippedSkillIds[slotIndex] = skillIdFS;
            }

            OnSkillsChanged?.Invoke();
            return true;
        }

        // Skill not equipped yet
        if (targetSkillId.Length == 0)
        {
            equippedSkillIds[slotIndex] = skillIdFS;
            OnSkillsChanged?.Invoke();
            return true;
        }

        // Target occupied, move old skill to an empty slot if possible
        int emptySlot = GetEmptySlotIndex();
        if (emptySlot == -1)
        {
            Debug.LogWarning("[SkillInventory] No empty slots to move the current skill.");
            return false;
        }

        equippedSkillIds[emptySlot] = targetSkillId;
        equippedSkillIds[slotIndex] = skillIdFS;

        OnSkillsChanged?.Invoke();
        return true;
    }

    private bool SwapSlotsServer(int slotA, int slotB)
    {
        if (!IsServer)
            return false;

        if (slotA < 0 || slotA >= maxSkillSlots)
            return false;

        if (slotB < 0 || slotB >= maxSkillSlots)
            return false;

        EnsureEquippedSlotsInitialized();

        if (slotA == slotB)
            return true;

        (equippedSkillIds[slotA], equippedSkillIds[slotB]) =
            (equippedSkillIds[slotB], equippedSkillIds[slotA]);

        OnSkillsChanged?.Invoke();
        return true;
    }

    private bool UnequipSlotServer(int slotIndex)
    {
        if (!IsServer)
            return false;

        if (slotIndex < 0 || slotIndex >= maxSkillSlots)
            return false;

        EnsureEquippedSlotsInitialized();

        equippedSkillIds[slotIndex] = default;

        OnSkillsChanged?.Invoke();
        return true;
    }

    // =========================================================
    // GET EQUIPPED
    // =========================================================

    public Skill GetEquippedSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= equippedSkillIds.Count)
            return null;

        string id = equippedSkillIds[slotIndex].ToString();

        if (string.IsNullOrEmpty(id))
            return null;

        return GetSkillById(id);
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
            if (skill == null)
                continue;

            if (!HasSkillUnlocked(skill))
                continue;

            if (skill.ownerCharacter != currentCharacter)
                continue;

            if (IsEquipped(skill))
                continue;

            result.Add(skill);
        }

        return result.OrderBy(s => s.skillName).ToList();
    }

    // =========================================================
    // SAVE / LOAD
    // =========================================================

    public List<string> CaptureUnlockedSkills()
    {
        List<string> result = new List<string>(unlockedSkillIds.Count);

        for (int i = 0; i < unlockedSkillIds.Count; i++)
        {
            result.Add(unlockedSkillIds[i].ToString());
        }

        return result;
    }
    public string[] CaptureEquippedSkillIds()
    {
        string[] result = new string[maxSkillSlots];

        for (int i = 0; i < maxSkillSlots; i++)
        {
            if (i < equippedSkillIds.Count)
                result[i] = equippedSkillIds[i].ToString();
            else
                result[i] = string.Empty;
        }

        return result;
    }

    public void RestoreUnlockedSkills(List<string> savedSkills)
    {
        if (!IsServer)
            return;

        unlockedSkillIds.Clear();

        if (savedSkills != null)
        {
            foreach (var id in savedSkills)
            {
                if (!string.IsNullOrEmpty(id))
                    unlockedSkillIds.Add(new FixedString64Bytes(id));
            }
        }

        OnSkillsChanged?.Invoke();
    }


    public void RestoreEquippedSkills(string[] ids)
    {
        if (!IsServer)
            return;

        EnsureEquippedSlotsInitialized();

        for (int i = 0; i < maxSkillSlots; i++)
        {
            if (ids != null && i < ids.Length && !string.IsNullOrEmpty(ids[i]))
                equippedSkillIds[i] = new FixedString64Bytes(ids[i]);
            else
                equippedSkillIds[i] = default;
        }

        OnSkillsChanged?.Invoke();
    }

    public void ClearAll()
    {
        if (!IsServer)
            return;

        unlockedSkillIds.Clear();

        EnsureEquippedSlotsInitialized();

        for (int i = 0; i < maxSkillSlots; i++)
            equippedSkillIds[i] = default;

        OnSkillsChanged?.Invoke();
    }

}