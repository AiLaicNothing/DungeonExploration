using UnityEngine;
using UnityEngine.Rendering;

public class PlayerBasicAttack : PlayerStates
{
    public PlayerBasicAttack(PlayerController player) : base(player) { }

    int comboIndex;
    float timer;
    bool canCombo;
    bool hasHit;
    public override void OnEnter()
    {
        comboIndex = 0;

        Debug.Log("Enter Basic Attack State");

        StartAttack();
    }

    public override void OnUpdate()
    {
        if (player.Input.hasDashed && player.HasStamina(player.DashCost))
        {
            player.ChangeActionState(player.dash_State);
            return;
        }

        var attackSteps = player.ComboData.attackSteps[comboIndex];

        timer -= Time.deltaTime;

        float elapsed = attackSteps.duration - timer;

        if (elapsed >= attackSteps.hitTime && !hasHit)
        {
            //DoHit(attackSteps);
            player.RequestMeleeAttack(comboIndex, true);
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

            if (comboIndex < player.ComboData.attackSteps.Length - 1)
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

        player.StartAttackMove(attackSteps);

        //--> Play animation
        Debug.Log($"Player attacked with {attackSteps.name}");

    }

    private bool CheckMeleeInput(AttackInputType type)
    {
        if (type  == AttackInputType.Melee)
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
