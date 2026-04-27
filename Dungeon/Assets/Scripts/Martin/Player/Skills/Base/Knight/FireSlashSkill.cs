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

    public override void Execute(PlayerController player)
    {
        Vector3 spawnPos = player.PlayerModel.transform.position + player.PlayerModel.transform.forward * spawnOffset.z + Vector3.up * spawnOffset.y;

        Quaternion rot = Quaternion.LookRotation(player.PlayerModel.transform.forward);

        GameObject prefab = Instantiate(proyectilePrefab, spawnPos, rot);

        PlayerProyectile proyectile = prefab.GetComponent<PlayerProyectile>();

        if (proyectile != null )
        {
            proyectile.Initialize(damage, hitData, player.PlayerModel.forward, proyectileSpeed, player);
        }
    }

}
