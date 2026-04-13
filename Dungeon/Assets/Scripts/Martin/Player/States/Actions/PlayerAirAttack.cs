using UnityEngine;

public class PlayerAirAttack : PlayerStates
{
    public PlayerAirAttack(PlayerController player) : base(player) { }

    //This may change because of animation duration
    float attackDuration = 0.6f;
    float timer;
    public override void OnEnter()
    {
        player.isPerformingAction = true;

        timer = attackDuration;

        player.hasUsedAirAttack = true;

        Vector3 vel = player.Rb.linearVelocity;
        vel.y = 0;
        player.Rb.linearVelocity = vel;

        //This can be change
        player.Rb.useGravity = false;

        //-->Play animation here
        Debug.Log("Enter Air Attack State");
    }

    public override void OnUpdate()
    {
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            OnExit();
        }
    }

    public override void OnExit()
    {
        player.Rb.useGravity = true;

        player.isPerformingAction = false;

        player.ChangeActionState(player.iddeAction_State);
    }
}
