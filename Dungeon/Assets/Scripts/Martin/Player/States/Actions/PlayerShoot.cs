using UnityEngine;

public class PlayerShoot : PlayerStates
{
    public PlayerShoot(PlayerController player) : base(player) { }

    public override void OnEnter()
    {
        Debug.Log("Shoot State");
    }

    public override void OnUpdate()
    {
        player.ChangeActionState(player.iddeAction_State);
    }

    public override void OnExit()
    {
    }
}
