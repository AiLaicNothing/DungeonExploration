using UnityEngine;

[CreateAssetMenu(menuName = "Skills/First Judgment")]
public class FirstJudgment : Skill
{
    [Header("Prefab")]
    public GameObject swordPrefab;

    [Header("Targeting")]
    public float maxRange = 20f;
    public float spawnHeight = 15f;

    [Header("SFX")]
    public GameObject spawnSFX;

    public override void Execute(PlayerController player)
    {
        Vector3 aimPoint = player.GetViewPoint();

        // clamp range
        Vector3 dir = (aimPoint - player.transform.position);
        float dist = dir.magnitude;

        if (dist > maxRange)
        {
            aimPoint = player.transform.position + dir.normalized * maxRange;
        }

        // spawn above target
        Vector3 spawnPos = aimPoint + Vector3.up * spawnHeight;

        GameObject sword = Instantiate(swordPrefab, spawnPos, Quaternion.identity);

        FirstJudgementSword proj = sword.GetComponent<FirstJudgementSword>();
        proj.Initialize(aimPoint);

        if (spawnSFX != null)
        {
        }
    }
}
