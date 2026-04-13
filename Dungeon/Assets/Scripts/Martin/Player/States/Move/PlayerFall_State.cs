using UnityEngine;

public class PlayerFall_State : PlayerStates
{
    public PlayerFall_State(PlayerController player) : base(player) { }

    public override void OnEnter()
    {
        Debug.Log("Enter Fall State");

        if (player.isPerformingAction)
        {
            return;
        }
    }

    public override void OnUpdate()
    {
        if (player.isGrounded)
        {
            if (player.Input.moveInput.magnitude > 0.1f)
            {
                player.ChangeState(player.move_State);
            }
            else
            {
                player.ChangeState(player.iddle_State);
            }
        }
    }

    public override void OnExit()
    {
        Debug.Log("Exit Fall State");
    }
}
