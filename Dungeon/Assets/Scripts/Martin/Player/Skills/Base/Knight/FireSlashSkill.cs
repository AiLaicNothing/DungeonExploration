using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Fire Slash ")]
public class FireSlashSkill : Skill
{
    [Header("Proyectile")]
    public GameObject proyectilePrefab;
    public float proyectileSpeed;

    [Header("Spawn")]
    public Vector3 spawnOffset = new Vector3(0, 1f, 1.5f);

    [Header("Damage")]
    public float damage;
    public HitData hitData;

    public override void LocalExecute(PlayerController player, Vector3 targetPoint)
    {
    }


    public override void ServerExecute(PlayerController player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        Vector3 finalTarget = lockTargetPos != Vector3.zero? lockTargetPos : targetPoint;

        Vector3 spawnPos = player.PlayerModel.position + player.PlayerModel.forward * spawnOffset.z + Vector3.up * spawnOffset.y;

        Vector3 dir = (finalTarget - spawnPos).normalized;

        if (dir.sqrMagnitude < 0.0001f) dir = player.PlayerModel.forward;

        Quaternion rot = Quaternion.LookRotation(dir);

        GameObject prefab = Object.Instantiate(proyectilePrefab, spawnPos, rot);
        prefab.GetComponent<NetworkObject>().Spawn();

        PlayerProyectile proyectile = prefab.GetComponent<PlayerProyectile>();

        if (proyectile != null)
        {
            proyectile.Initialize((player.Stats.PhysicalDamage.CurrentValue * hitData.physicalScale) + (player.Stats.MagicalDamage.CurrentValue * hitData.magicalScale), hitData, dir, proyectileSpeed, Vector3.zero);
        }
    }
}


