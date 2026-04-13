using UnityEngine;

public class PlayerIddle_State : PlayerStates
{
    public PlayerIddle_State(PlayerController player) : base(player) { }

    public override void OnEnter()
    {
        Debug.Log("Enter Iddle State");
        if (player.isPerformingAction)
        {
            return;
        }
        //--> Play the animation
    }

    public override void OnUpdate()
    {
        if (!player.isGrounded)
        {
            player.ChangeState(player.fall_State);
        }

        if(player.Input.moveInput.magnitude > 0.1f)
        {
            player.ChangeState(player.move_State);
            return;
        }

        if(player.Input.hasJumped)
        {
            player.ChangeState(player.jump_State);
        }
    }
}
