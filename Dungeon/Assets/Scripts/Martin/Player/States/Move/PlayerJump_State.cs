using UnityEngine;

public class PlayerJump_State : PlayerStates
{
    public PlayerJump_State(PlayerController player) : base(player) { }

    public override void OnEnter()
    {
        Debug.Log("Enter Jump State");
        if (player.isPerformingAction)
        {
            return;
        }
        player.Jump();
    }

    public override void OnUpdate()
    {
        if (player.Rb.linearVelocity.y <= 0)
        {
            player.ChangeState(player.fall_State);
        }
    }
    public override void OnExit()
    {
        Debug.Log("Exit Fall State");
    }
}
