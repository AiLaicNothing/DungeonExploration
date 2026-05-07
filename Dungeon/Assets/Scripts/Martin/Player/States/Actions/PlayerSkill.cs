using UnityEngine;

public class PlayerSkill : PlayerStates
{
    public PlayerSkill(PlayerController player) : base(player) { }

    Skill currentSkill;
    int skillIndex;

    float timer;
    bool isCasting = true;

    Vector3 saveTargetPoint;
    public override void OnEnter()
    {
        //--> Check thatk skill is not null
        if (currentSkill == null)
        {
            player.ChangeActionState(player.iddeAction_State);
            return;
        }

        //--> Check that the is not in cooldown
        if (!player.IsSkillReady(skillIndex))
        {
            player.ChangeActionState(player.iddeAction_State);
            return;
        }

        //--> Check if the player has enough resources to cast
        if(!player.HasResource(currentSkill.resourceType, currentSkill.cost))
        {
            player.ChangeActionState(player.iddeAction_State);
            return;
        }

        player.ConsumeResource(currentSkill.resourceType, currentSkill.cost);

        player.isPerformingAction = true;

        isCasting = true;
        timer = currentSkill.castTime;

        saveTargetPoint = player.GetViewPoint();

        //--> call animator and play the animation name of casting
        //player.Animator.Play(currentSkill.castAnimation);
    }

    public override void OnUpdate()
    {
        //--> If casting
        if (isCasting)
        {
            //--> If player dash interrupt and cancel the skill casting
            if (player.Input.hasDashed || player.Input.hasJumped)
            {
                player.ChangeActionState(player.iddeAction_State);
                return;
            }
        }

        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            if (isCasting)
            {
                isCasting = false;

                //player.Animator.Play(currentSkill.actionAnimation);

                // Execute skill logic

                // Local execute handle thing that manage the Rb for things like movement
                currentSkill.LocalExecute(player, saveTargetPoint);

                // Server pass data to server things like hits
                player.RequestSkill(skillIndex, saveTargetPoint);

                // Set cooldown
                player.TriggerCooldown(skillIndex);

                // Optional: short action duration
                // This is the duration of the casting???
                timer = currentSkill.actionTime;
            }
            else
            {
                player.ChangeActionState(player.iddeAction_State);
            }
        }
    }

    public override void OnExit() 
    {
        player.isPerformingAction = false;
        player.blockVelocity = false;
    }

    public void SetSkill(Skill skillToUse, int index)
    {
        currentSkill = skillToUse;
        skillIndex = index;
    }
}
