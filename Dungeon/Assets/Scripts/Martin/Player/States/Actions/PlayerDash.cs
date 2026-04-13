using UnityEngine;

public class PlayerDash : PlayerStates
{
    public PlayerDash(PlayerController player) : base(player) { }

    public override void OnEnter()
    {
        player.isPerformingAction = true;
        Debug.Log("Enter Dash State");
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
    }

    public override void OnExit()
    {
        player.isPerformingAction = false;
        base.OnExit();
    }
}
