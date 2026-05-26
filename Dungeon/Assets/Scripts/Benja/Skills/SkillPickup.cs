using UnityEngine;

public class SkillPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private Skill skill;

    public void Interact()
    {
        if (LocalPlayer.Controller == null)
            return;

        var inventory =
            LocalPlayer.Controller.GetComponent<PlayerSkillInventory>();

        if (inventory == null)
            return;

        int currentCharacter =
            PlayerSessionData.local.SelectedCharacter.Value;

        if ((int)skill.ownerCharacter != currentCharacter)
        {
            Debug.Log("Esta skill no pertenece a este personaje.");
            return;
        }

        inventory.UnlockSkill(skill);

        Destroy(gameObject);
    }
}