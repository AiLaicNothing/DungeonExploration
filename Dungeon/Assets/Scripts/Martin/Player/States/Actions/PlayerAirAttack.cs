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
        var attackSteps = player.AirComboData.attackSteps[comboIndex];

        timer = attackSteps.duration;
        canCombo = false;
        hasHit = false;

        player.isPerformingAction = true;

        //--> Play animation
        Debug.Log($"Player attacked with {attackSteps.name}");

    }

    private void DoHit(AttackSteps attack)
    {
        Vector3 center = player.PlayerModel.transform.position + player.PlayerModel.transform.forward * attack.hitBoxOffSet.z + Vector3.up * attack.hitBoxOffSet.y;

        Collider[] hits = Physics.OverlapBox(center, attack.hitBoxSize * 0.5f, player.PlayerModel.transform.rotation);

        //player.ShowHitbox(center, attack.hitBoxSize, player.PlayerModel.transform.rotation);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Debug.Log($"Hit Enemy: {hit.name}");

                //Add damage logic

                IDamageable damageable = hit.GetComponent<IDamageable>();

                if (damageable != null)
                {
                    Vector3 hitDir = player.PlayerModel.transform.forward;

                    damageable.TakeDamage(10f * attack.hitData.damageMultiplier, attack.hitData.throwType, hitDir, attack.hitData.stunDuration, attack.hitData.keepInAir, attack.hitData.airLiftForce, attack.hitData.staggerCharge);
                }
            }
        }
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
