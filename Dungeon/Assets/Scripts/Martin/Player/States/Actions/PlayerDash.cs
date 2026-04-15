using UnityEngine;

public class PlayerDash : PlayerStates
{
    public PlayerDash(PlayerController player) : base(player) { }

    float timer;
    Vector3 dashDir;
    float dashSpeed;
    public override void OnEnter()
    {
        Debug.Log("Enter Dash State");

        // Check stamina
        if (!player.HasStamina(player.DashCost))
        {
            player.ChangeActionState(player.iddeAction_State);
            return;
        }

        player.ConsumeStamina(player.DashCost);

        timer = player.DashDuration;
        dashSpeed = player.DashDistance / player.DashDuration;

        player.isPerformingAction = true;

        //--> Check input movement, if it doesnt recive one dash forward else toward the input direction
        Vector2 input = player.Input.moveInput;

        if (input.magnitude > 0.1f)
        {
            Vector3 camForward = Camera.main.transform.forward;
            camForward.y = 0;
            camForward.Normalize();

            Vector3 camRight = Camera.main.transform.right;
            camRight.y = 0;
            camRight.Normalize();

            dashDir = (camForward * input.y + camRight * input.x).normalized;
        }
        else
        {
            dashDir = player.PlayerModel.transform.forward;
        }

        player.Rb.useGravity = false;

        // Optional animation
        // player.Animator.Play("Dash");

    }

    public override void OnUpdate()
    {
        timer -= Time.deltaTime;

        // Maintain constant dash velocity
        Vector3 velocity = dashDir * dashSpeed;
        velocity.y = 0;

        player.Rb.linearVelocity = velocity;

        if (timer <= 0)
        {
            player.ChangeActionState(player.iddeAction_State);
        }
    }

    public override void OnExit()
    {
        player.Rb.useGravity = true;

        player.isPerformingAction = false;

        Debug.Log("Exit Dash State");
    }
}
