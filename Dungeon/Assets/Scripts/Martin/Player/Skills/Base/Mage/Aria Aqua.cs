using System.Collections;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Aria Aqua")]
public class AriaAqua : Skill
{
    [Header("Prefabs")]
    public GameObject projectilePrefab;
    public GameObject centerVFX;
    public GameObject explosionVFX;

    [Header("Targeting")]
    public float maxRange = 25f;
    public LayerMask groundLayer;

    [Header("Spawn")]
    public float spawnDistance = 10f;
    public float airHeight = 1f;

    [Header("Timing")]
    public float travelTime = 1.5f;
    public float explosionDelay = 0.2f;

    [Header("Explosion")]
    public float explosionRadius = 5f;
    public LayerMask enemyLayer;
    public HitData hitData;
    public float damage = 20f;


    public override void LocalExecute(PlayerController player, Vector3 targetPoint)
    {
        throw new System.NotImplementedException();
    }

    public override void ServerExecute(PlayerController player, Vector3 targetPoint)
    {
        player.StartCoroutine(ExecuteRoutine(player));
    }

    private IEnumerator ExecuteRoutine(PlayerController player)
    {
        player.blockVelocity = true;

        Vector3 groundCenter = player.GetAimPoint(maxRange, groundLayer);

        // ensure ground
        if (Physics.Raycast(groundCenter + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
        {
            groundCenter = hit.point;
        }

        Vector3 airCenter = groundCenter + Vector3.up * airHeight;

        if (centerVFX != null)
        {
            GameObject.Instantiate(centerVFX, groundCenter, Quaternion.identity);
        }

        int arrivedCount = 0;

        void OnNodeArrive()
        {
            arrivedCount++;
        }

        SpawnNodes(player, airCenter, OnNodeArrive);

        // wait until all 3 arrive
        yield return new WaitUntil(() => arrivedCount >= 3);

        // small delay
        yield return new WaitForSeconds(explosionDelay);

        if (explosionVFX != null)
        {
            GameObject.Instantiate(explosionVFX, airCenter, Quaternion.identity);
        }

        Explode(groundCenter);
    }

    void SpawnNodes(PlayerController player, Vector3 center, System.Action onArrive)
    {
        Vector3 forward = player.PlayerModel.forward;
        Vector3 right = player.PlayerModel.right;

        Vector3 p1 = center + forward * spawnDistance;
        Vector3 p2 = center - forward * spawnDistance + right * spawnDistance;
        Vector3 p3 = center - forward * spawnDistance - right * spawnDistance;

        CreateNode(p1, center, onArrive);
        CreateNode(p2, center, onArrive);
        CreateNode(p3, center, onArrive);
    }

    void CreateNode(Vector3 pos, Vector3 center, System.Action onArrive)
    {
        GameObject obj = Instantiate(projectilePrefab, pos, Quaternion.identity);

        obj.GetComponent<NetworkObject>().Spawn();

        var node = obj.GetComponent<AriaAquaproj>();
        node.Initialize(center, travelTime, onArrive);
    }

    void Explode(Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(center, explosionRadius, enemyLayer);

        foreach (var hit in hits)
        {
            IDamageable dmg = hit.GetComponent<IDamageable>();

            if (dmg != null)
            {
                Vector3 dir = (hit.transform.position - center).normalized;

                dmg.TakeDamage(damage,  hitData.throwType,  dir, hitData.stunDuration,  hitData.keepInAir, hitData.airLiftForce, hitData.staggerCharge);
            }
        }
    }
}