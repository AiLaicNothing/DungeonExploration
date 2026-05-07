using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Nova Orbita")]
public class NovaOrbita : Skill
{
    [Header("Prefab")]
    public GameObject orbPrefab;
    public HitData hitData;

    [Header("Orbit Settings")]
    public float radius = 3f;
    public float angularSpeed = 180f; // degrees per second
    public float duration = 5f;

    public override void LocalExecute(PlayerController player, Vector3 targetPoint)
    {
    }

    public override void ServerExecute(PlayerController player, Vector3 targetPoint)
    {
        SpawnOrb(player, 0f);    // front
        SpawnOrb(player, 180f);  // back
    }

    void SpawnOrb(PlayerController player, float startAngle)
    {
        // convert angle to radians
        float rad = startAngle * Mathf.Deg2Rad;

        Vector3 forward = player.PlayerModel.forward;
        Vector3 right = player.PlayerModel.right;

        // calculate orbit offset relative to player rotation
        Vector3 offset = forward * Mathf.Cos(rad) * radius +  right * Mathf.Sin(rad) * radius;

        // final spawn position
        Vector3 spawnPos = player.transform.position + offset;

        // instantiate orb in correct position
        GameObject orb = Instantiate(orbPrefab, spawnPos, Quaternion.identity);

        orb.GetComponent<NetworkObject>().Spawn();

        // initialize behavior
        NovaOrbitaOrb orbScript = orb.GetComponent<NovaOrbitaOrb>();
        orbScript.Initialize(player, radius, angularSpeed, duration, startAngle, hitData);
    }
}
