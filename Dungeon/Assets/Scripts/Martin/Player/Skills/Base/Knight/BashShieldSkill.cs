using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Bash Shield Skill")]
public class BashShieldSkill : Skill
{
    public float dashSpeed;
    public float duration;

    public HitData hitData;
    public Vector3 hitBoxSize;
    public Vector3 hitBoxOffset;

    public override void LocalExecute(PlayerController player, Vector3 targetPoint)
    {
        // Only rotate if lock-on target exists
        if (player.LockTarget != null && player.LockTarget.isTargeting && player.LockTarget.CurrentTarget != null)
        {
            Vector3 targetPos = player.LockTarget.CurrentTarget.position;

            Vector3 dir = (targetPos - player.transform.position).normalized;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.01f)
            {
                player.PlayerModel.rotation = Quaternion.LookRotation(dir);
            }
        }

        // No target -> keep current forward direction

        player.StartCoroutine(BashMove(player));
    }
    public override void ServerExecute(PlayerController player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        player.StartCoroutine(BashShield(player));
    }

    private IEnumerator BashMove(PlayerController player)
    {

        player.blockVelocity = false;

        float timer = duration;

        while (timer > 0)
        {
            timer -= Time.deltaTime;

            Vector3 vel = player.PlayerModel.forward * dashSpeed;
            vel.y = player.Rb.linearVelocity.y;

            player.Rb.linearVelocity = vel;

            yield return null;
        }
    }

    public IEnumerator BashShield(PlayerController player)
    {

        float timer = duration;

        while (timer > 0)
        {
            timer -= Time.deltaTime;

            Vector3 center = player.PlayerModel.transform.position + player.PlayerModel.transform.forward * hitBoxOffset.z + Vector3.up * hitBoxOffset.y;

            Collider[] hits = Physics.OverlapBox(center, hitBoxSize * 0.5f, player.PlayerModel.transform.rotation);

            player.ShowHitboxClientRpc(center, hitBoxSize, player.PlayerModel.transform.rotation);

            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                {
                    var dmg = hit.GetComponent<IDamageable>();

                    if (dmg != null)
                    {
                        dmg.TakeDamage((player.Stats.PhysicalDamage.CurrentValue * hitData.physicalScale) + (player.Stats.MagicalDamage.CurrentValue * hitData.magicalScale), hitData.throwType, player.PlayerModel.transform.forward, hitData.stunDuration, hitData.keepInAir, hitData.airLiftForce, hitData.staggerCharge);

                        yield break; 
                    }
                }
            }

            yield return null;
        }
    }

}

