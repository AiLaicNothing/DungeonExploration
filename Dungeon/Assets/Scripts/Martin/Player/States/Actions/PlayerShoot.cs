using UnityEngine;

public class PlayerShoot : PlayerStates
{
    public PlayerShoot(PlayerController player) : base(player) { }

    float timer;
    float fireCooldown;
    bool hasShoot;

    public override void OnEnter()
    {
        timer = 0f;
        fireCooldown = 0f;
        hasShoot = false;

        player.isPerformingAction = true;

        Debug.Log("Shoot State");
    }

    public override void OnUpdate()
    {
        timer += Time.deltaTime;
        fireCooldown -= Time.deltaTime;

        if (!hasShoot && timer >= player.ShootData.shootTime)
        {
            Shoot();
            hasShoot=true;
        }

        if(player.Input.AttackBuffered && fireCooldown <= 0f)
        {
            player.ChangeActionState(player.shoot_State);
        }

        if(timer >= player.ShootData.shootTime + 0.1f)
        {
            player.ChangeActionState(player.iddeAction_State);
        }
    }

    public override void OnExit()
    {
        player.isPerformingAction = false;
    }

    private void Shoot()
    {
        fireCooldown = player.ShootData.timeBtwShot;

        Vector3 targetPoint = player.GetViewPoint();

        Vector3 dir = (targetPoint - player.FirePoint.position).normalized;

        GameObject prefab = GameObject.Instantiate(player.ShootData.proyectilePrefab, player.FirePoint.position, Quaternion.LookRotation(dir));

        PlayerProyectile proyectile = prefab.GetComponent<PlayerProyectile>();

        if (proyectile != null)
        {
            proyectile.Initialize(10, player.ShootData.hitData, dir, player.ShootData.proyectileSpeed, player);
        }
    }
}
