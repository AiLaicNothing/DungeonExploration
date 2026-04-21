using UnityEngine;

public class PlayerIddleAction : PlayerStates
{
    public PlayerIddleAction(PlayerController player) : base(player) { }

    public override void OnEnter()
    {
        Debug.Log("Enter Iddle Action State");
    }

    public override void OnUpdate()
    {
        if (player.Input.hasDashed && player.HasStamina(player.DashCost))
        {
            player.ChangeActionState(player.dash_State);
            return;
        }

        if (player.Input.attackPressed)
        {
            //--> If is not touching the ground and has not attacked in the air
            if (!player.isGrounded && !player.hasUsedAirAttack)
            {
                //-->Do airAttack
                player.ChangeActionState(player.airAttack_State);
            }
            else if (player.isGrounded)
            {
                //-->Do grounded attack
                player.ChangeActionState(player.basicAttack_State);
            }
        }

        int index = player.Input.skillPressedIndex;

        if (index != -1)
        {
            var skill = player.GetSkill(index);

            if (skill != null && player.IsSkillReady(index))
            {
                player.skill_State.SetSkill(skill, index);
                player.ChangeActionState(player.skill_State);
                return;
            }
        }
    }

    public override void OnExit()
    {
        base.OnExit();
    }
}
