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
        if (player.Input.attackPressed)
        {
            //--> If is not touching the ground and has not attacked in the air
            if(!player.isGrounded && !player.hasUsedAirAttack)
            {
                //-->Do airAttack
                player.ChangeActionState(player.AirAttack_State);
            }
            else if (player.isGrounded)
            {
                //-->Do grounded attack
                player.ChangeActionState(player.basicAttack_State);
            }
        }
    }

    public override void OnExit()
    {
        base.OnExit();
    }
}
