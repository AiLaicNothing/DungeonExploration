using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Magic Beam Skill")]
public class MagicBeam : Skill
{
    [Header("Beam Size")]
    [SerializeField] private float maxRange = 15f;
    [SerializeField] private float width = 2f;
    [SerializeField] private float height = 2f;

    [Header("Time")]
    [SerializeField] private float duration = 2f;
    [SerializeField] private float tickRate;

    [Header("Offset")]
    [SerializeField] private Vector3 startOffset;

    [Header("Damage")]
    [SerializeField] private HitData hitData;

    [Header("Layer")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask enemyLayer;
    private GameObject debugBox;

    public override void LocalExecute(PlayerController player, Vector3 targetPoint)
    {
        throw new System.NotImplementedException();
    }
    public override void ServerExecute(PlayerController player, Vector3 targetPoint)
    {
        player.StartCoroutine(BeamRoutine(player));
    }

    private IEnumerator BeamRoutine(PlayerController player)
    {
        player.blockVelocity = true;

        float timer = 0f;

        while (timer < duration)
        {
            FireBeam(player);

            yield return new WaitForSeconds(tickRate);

            timer += tickRate;
        }

        if (debugBox != null)
        {
            GameObject.Destroy(debugBox);
            debugBox = null;
        }
    }

    private void FireBeam(PlayerController player)
    {
        Vector3 startPos = player.transform.position + player.PlayerModel.right * startOffset.x + player.PlayerModel.up * startOffset.y + player.PlayerModel.forward * startOffset.z;

        Vector3 dir = player.PlayerModel.forward;

        float finalDistance = maxRange;

        if (Physics.Raycast(startPos, dir, out RaycastHit hit, maxRange, obstacleLayer))
        {
            finalDistance = hit.distance;
        }

        Vector3 center = startPos + dir * (finalDistance / 2f);

        Vector3 halfExtents = new Vector3(width / 2f, height / 2f, finalDistance / 2f);

        Quaternion rot = Quaternion.LookRotation(dir);

        debugBox = player.ShowHitboxPersistent(center, halfExtents * 2, rot, debugBox);

        Collider[] hits = Physics.OverlapBox(center, halfExtents, rot, enemyLayer);

        foreach (var target in hits)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(10, hitData.throwType, dir, hitData.stunDuration, hitData.keepInAir, hitData.airLiftForce, hitData.staggerCharge);
            }
        }

    }
}
