using System.Collections;
using Unity.Services.Lobbies.Models;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.ProBuilder;
using static UnityEditor.PlayerSettings;

[CreateAssetMenu(menuName = "Skills/Magic bullet Skill")]
public class MagicBullet : Skill
{
    [Header("Prefabs")]
    public GameObject vfxPrefab;
    public GameObject proyectilePrefab;

    [Header("Offsets")]
    public float backOffset = 1.5f;
    public float heightOffset = 2f;
    public float sideOffset = 2f;

    [Header("Shooting")]
    public float speed;
    public int proyectilePerNode = 2;
    public float timeBtwShoot;

    public HitData hitData;

    public override void Execute(PlayerController player)
    {
        Vector3 basePos = player.transform.position - player.PlayerModel.forward * backOffset + player.PlayerModel.up * heightOffset;

        Vector3 left = basePos - player.PlayerModel.right * sideOffset;
        Vector3 middle = basePos;
        Vector3 right = basePos + player.PlayerModel.right * sideOffset;

        player.StartCoroutine(CastSkill(player, left));
        player.StartCoroutine(CastSkill(player, middle));
        player.StartCoroutine(CastSkill(player, right));
    }

    IEnumerator CastSkill(PlayerController player, Vector3 pos)
    {
        if (vfxPrefab != null)
        {
            GameObject vfx = Instantiate(vfxPrefab, pos, player.PlayerModel.rotation);
        }

        yield return new WaitForSeconds(0.2f);

        Vector3 target = player.GetViewPoint();

        Vector3 baseDir = (target - pos).normalized;

        for (int i = 0; i < proyectilePerNode; i++)
        {
            GameObject proj = Instantiate(proyectilePrefab, pos, Quaternion.identity);

            var proyectile = proj.GetComponent<PlayerProyectile>();

            if (proyectile != null)
            {
                proyectile.Initialize(10, hitData, baseDir, speed, player);
            }

            yield return new WaitForSeconds(timeBtwShoot);
        }
    }
}
