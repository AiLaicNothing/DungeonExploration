using UnityEngine;

public class PlayerBasicAttack : PlayerStates
{
    public PlayerBasicAttack(PlayerController player) : base(player) { }

    int comboIndex;
    float timer;
    bool canCombo;
    public override void OnEnter()
    {
        comboIndex = 0;

        Debug.Log("Enter Basic Attack State");
    }

    public override void OnUpdate()
    {
        var attackSteps = player.ComboData.attackSteps[comboIndex];
        timer -= Time.deltaTime;

        float elapsed = attackSteps.duration - timer;

        //--> Check if it can continue combo / recive input window to continue
        if (elapsed <= attackSteps.comboWindowStart && elapsed >= attackSteps.comboWindowEnd)
        {
            canCombo = true;
        }

        //--> Check if recive input to continue combo
        if (canCombo && player.Input.AttackBuffered)
        {
            player.Input.UseAttackBufer();

            if (comboIndex < player.ComboData.attackSteps.Length - 1)
            {
                comboIndex++;
                StartAttack();
                return;
            }
        }

        //--> The attack end when the window to continue combo end
        if (elapsed <= attackSteps.comboWindowEnd)
        {
            OnExit();
        }
    }

    

    public override void OnExit()
    {
        player.isPerformingAction = false;

        player.ChangeActionState(player.iddeAction_State);
    }

    private void StartAttack()
    {
        var attackSteps = player.ComboData.attackSteps[comboIndex];

        timer = attackSteps.duration;
        canCombo = false;

        player.isPerformingAction = true;

        //--> Play animation
        Debug.Log($"Player attacked with {attackSteps.name}");
    }
}
