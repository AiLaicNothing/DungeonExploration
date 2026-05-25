using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Freeze")]
public class Freeze : Skill
{
    [Header("Spin Size")]
    [SerializeField] private Vector3 hitBoxSize;

    [Header("Offset")]
    [SerializeField] private Vector3 startOffset;

    [Header("Damage")]
    [SerializeField] private HitData hitData;

    [Header("Layer")]
    [SerializeField] private LayerMask enemyLayer;
    private GameObject debugBox;

    [Header("Sfx")]
    [SerializeField] private GameObject sfx;

    public override void LocalExecute(PlayerController player, Vector3 targetPoint)
    {
    }

    public override void ServerExecute(PlayerController player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        player.blockVelocity = true;
        DealDamage(player);
    }

    private void DealDamage(PlayerController player)
    {
        Vector3 startPos = player.transform.position + player.PlayerModel.right * startOffset.x + player.PlayerModel.up * startOffset.y + player.PlayerModel.forward * startOffset.z;

        debugBox = player.ShowHitboxPersistent(startPos, hitBoxSize * 0.5f * 2, player.transform.rotation, debugBox);

        Collider[] hits = Physics.OverlapBox(startPos, hitBoxSize * 0.5f, player.transform.rotation, enemyLayer);

        foreach (var target in hits)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();

            if (damageable != null)
            {
                Vector3 dir = (target.transform.position - player.transform.position).normalized;

                damageable.TakeDamage(10, hitData.throwType, dir, hitData.stunDuration, hitData.keepInAir, hitData.airLiftForce, hitData.staggerCharge);
            }
        }
    }
}
