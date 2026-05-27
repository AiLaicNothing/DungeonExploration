using UnityEngine;

public class SkillPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private Skill skill;
    public void Interact()
    {
        if (skill == null)
            return;

        if (LocalPlayer.Controller == null)
            return;

        var inventory = LocalPlayer.Controller.GetComponent<PlayerSkillInventory>();

        if (inventory == null)
            return;

        if (PlayerSessionData.local == null)
            return;

        int currentCharacter = PlayerSessionData.local.SelectedCharacter.Value;

        if ((int)skill.ownerCharacter != currentCharacter)
        {
            Debug.Log("Esta skill no pertenece a este personaje.");
            return;
        }

        inventory.RequestUnlockSkill(skill);

        // If this pickup is networked, despawn it on the server instead of Destroy.
        Destroy(gameObject);
    }
}