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
        player.StartCoroutine(BashMove(player));
    }
    public override void ServerExecute(PlayerController player, Vector3 targetPoint)
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
                        dmg.TakeDamage(10 * hitData.damageMultiplier, hitData.throwType, player.PlayerModel.transform.forward, hitData.stunDuration, hitData.keepInAir, hitData.airLiftForce, hitData.staggerCharge);

                        yield break; 
                    }
                }
            }

            yield return null;
        }
    }

}

