using UnityEngine;

public class PlayerAirAttack : PlayerStates
{
    public PlayerAirAttack(PlayerController player) : base(player) { }

    //This may change because of animation duration

    int comboIndex;
    float timer;
    bool canCombo;
    bool hasHit;

    public override void OnEnter()
    {
        player.isPerformingAction = true;

        player.blockVelocity = true;

        comboIndex = 0;

        player.hasUsedAirAttack = true;

        Vector3 vel = player.Rb.linearVelocity;
        vel.y = 0;
        player.Rb.linearVelocity = vel;

        //This can be change
        player.Rb.useGravity = false;

        StartAttack();

        //-->Play animation here
        Debug.Log("Enter Air Attack State");
    }

    public override void OnUpdate()
    {
        if (player.Input.hasDashed && player.HasStamina(player.DashCost))
        {
            player.ChangeActionState(player.dash_State);
            return;
        }
        var attackSteps = player.AirComboData.attackSteps[comboIndex];

        timer -= Time.deltaTime;

        float elapsed = attackSteps.duration - timer;

        if (elapsed >= attackSteps.hitTime && !hasHit)
        {
            //DoHit(attackSteps);
            player.RequestMeleeAttack(comboIndex, false);
            hasHit = true;
        }

        //--> Check if it can continue combo / recive input window to continue
        if (elapsed >= attackSteps.comboWindowStart && elapsed <= attackSteps.comboWindowEnd)
        {
            canCombo = true;
        }

        //--> Check if recive input to continue combo
        if (canCombo && player.Input.AttackBuffered)
        {
            var type = player.Input.bufferedAttackType;

            if (!CheckMeleeInput(type))
            {
                return;
            }

            player.Input.UseAttackBufer();

            if (comboIndex < player.AirComboData.attackSteps.Length - 1)
            {
                comboIndex++;
                StartAttack();
                return;
            }
        }

        //--> The attack end when the window to continue combo end
        if (elapsed > attackSteps.comboWindowEnd)
        {
            player.ChangeActionState(player.iddeAction_State);
        }
    }

    public override void OnExit()
    {
        player.Rb.useGravity = true;

        player.isPerformingAction = false;

        player.blockVelocity = false;
    }

    private void StartAttack()
    {
        var attackSteps = player.ComboData.attackSteps[comboIndex];

        timer = attackSteps.duration;
        canCombo = false;
        hasHit = false;

        player.isPerformingAction = true;
        player.blockVelocity = true;

        player.PlayAttackVfxLocal(comboIndex, true);
        player.RequestAttackVfx(comboIndex, true);

        //--> Play animation
        Debug.Log($"Player attacked with {attackSteps.name}");

    }

    private bool CheckMeleeInput(AttackInputType type)
    {
        if (type == AttackInputType.Melee)
        {
            return true;
        }

        if (!player.IsRange && type == AttackInputType.Primary)
        {
            return true;
        }

        return false;
    }
}
