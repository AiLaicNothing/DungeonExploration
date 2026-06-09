using UnityEngine;
using System.Collections;

public class SkillPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private CharacterSkillEntry[] skills = new CharacterSkillEntry[2];
    private PlayerSkillInventory inventory;




    private IEnumerator Start()
    {
        while (LocalPlayer.Controller == null)
            yield return null;

        inventory =
            LocalPlayer.Controller.GetComponent<PlayerSkillInventory>();

        if (inventory == null)
            yield break;

        inventory.OnSkillsChanged += RefreshVisibility;

        RefreshVisibility();
    }



    public void Interact()
    {
        if (LocalPlayer.Controller == null)
            return;

        var inventory = LocalPlayer.Controller.GetComponent<PlayerSkillInventory>();

        if (inventory == null)
            return;

        if (PlayerSessionData.local == null)
            return;

        CharacterType currentCharacter =
            (CharacterType)PlayerSessionData.local.SelectedCharacter.Value;

        Skill skillToUnlock =
            GetSkillForCharacter(currentCharacter);

        if (skillToUnlock == null)
            return;

        if (inventory.HasSkillUnlocked(skillToUnlock))
        {
            Debug.Log("Ya tienes esta habilidad.");
            return;
        }

        inventory.RequestUnlockSkill(skillToUnlock);

        InteractionUI.Instance.HideUI();

        // Si el objeto es networked, usa Despawn en el servidor.
        gameObject.SetActive(false);
    }

    private void RefreshVisibility()
    {
        if (inventory == null)
            return;

        if (PlayerSessionData.local == null)
            return;

        CharacterType currentCharacter =
            (CharacterType)PlayerSessionData.local.SelectedCharacter.Value;

        Skill skill =
            GetSkillForCharacter(currentCharacter);

        if (skill == null)
            return;

        bool shouldShow =
            !inventory.HasSkillUnlocked(skill);

        gameObject.SetActive(shouldShow);
    }

    public static void RefreshAll()
    {
        var pickups =
            FindObjectsByType<SkillPickup>(
                FindObjectsSortMode.None);

        foreach (var pickup in pickups)
        {
            pickup.RefreshVisibility();
        }
    }

    private Skill GetSkillForCharacter(CharacterType character)
    {
        foreach (CharacterSkillEntry entry in skills)
        {
            if (entry == null)
                continue;

            if (entry.skill == null)
                continue;

            if (entry.character == character)
                return entry.skill;
        }

        return null;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();

        if (player == null || !player.IsOwner)
            return;

        CharacterType currentCharacter =
            (CharacterType)PlayerSessionData.local.SelectedCharacter.Value;

        Skill skill = GetSkillForCharacter(currentCharacter);

        if (skill == null)
            return;

        InteractionUI.Instance.SetUp($"Desbloquear {skill.skillName}");
        InteractionUI.Instance.ShowUI();
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();

        if (player == null || !player.IsOwner)
            return;

        InteractionUI.Instance.HideUI();
    }
}