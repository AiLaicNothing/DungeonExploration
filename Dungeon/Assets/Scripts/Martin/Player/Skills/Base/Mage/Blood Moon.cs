using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Blood Moon Skill")]
public class BloodMoon : Skill
{
    public GameObject moonPrefab;

    [Header("Spawn")]
    public float height;
    public Vector3 offset;
    public HitData hitData;

    public override void Execute(PlayerController player)
    {
        player.blockVelocity = true;

        Vector3 spawnPos = player.transform.position + player.PlayerModel.forward * offset.z + player.PlayerModel.right * offset.x + Vector3.up * height;

        GameObject moon = Instantiate(moonPrefab, spawnPos, Quaternion.identity);

        BloodMoonProj moonProj = moon.GetComponent<BloodMoonProj>();

        if (moonProj != null)
        {
            moonProj.Initialize(player, hitData);
        } 
    }
}
