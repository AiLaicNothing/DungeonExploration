using UnityEngine;

public class PlayerMove_State : PlayerStates
{
    public PlayerMove_State(PlayerController player) : base(player) { }
    public override void OnEnter()
    {
        //--> Play the animation
    }

    public override void OnUpdate()
    {
        if (!player.isGrounded)
        {
            player.ChangeState(player.fall_State);
        }

        if (player.Input.moveInput.magnitude < 0.1f)
        {
            player.ChangeState(player.iddle_State);
            return;
        }

        if (player.Input.hasJumped)
        {
            player.ChangeState(player.jump_State);
        }
    }
}
