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

        if (player.Input.AttackBuffered)
        {
            var type = player.Input.bufferedAttackType;

            player.Input.UseAttackBufer();

            //--> Input [F] melee atatack for both of them [Melee/Range]
            if (type == AttackInputType.Melee)
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

            //--> Input is mouse left button
            if (type == AttackInputType.Primary)
            {
                //--> If player is range do shoot else do melee attack
                if (player.IsRange)
                {
                    player.ChangeActionState(player.shoot_State);
                }
                else
                {
                    if (!player.isGrounded && !player.hasUsedAirAttack)
                    {
                        player.ChangeActionState(player.airAttack_State);
                    }
                    else if (player.isGrounded)
                    {
                        player.ChangeActionState(player.basicAttack_State);
                    }
                }
                return;
                
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
