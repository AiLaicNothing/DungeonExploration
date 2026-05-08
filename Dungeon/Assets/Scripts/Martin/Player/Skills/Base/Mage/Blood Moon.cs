using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Blood Moon Skill")]
public class BloodMoon : Skill
{
    public GameObject moonPrefab;

    [Header("Spawn")]
    public float height;
    public Vector3 offset;
    public HitData hitData;

    public override void LocalExecute(PlayerController player, Vector3 targetPoint)
    {
    }

    public override void ServerExecute(PlayerController player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        player.blockVelocity = true;

        Vector3 spawnPos = player.transform.position + player.PlayerModel.forward * offset.z + player.PlayerModel.right * offset.x + Vector3.up * height;

        GameObject moon = Instantiate(moonPrefab, spawnPos, Quaternion.identity);

        moon.GetComponent<NetworkObject>().Spawn();


        BloodMoonProj moonProj = moon.GetComponent<BloodMoonProj>();

        if (moonProj != null)
        {
            moonProj.Initialize(player, hitData, targetPoint);
        } 
    }
}
