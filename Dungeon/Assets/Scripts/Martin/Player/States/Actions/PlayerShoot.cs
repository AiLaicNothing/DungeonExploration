using Unity.Netcode;
using UnityEngine;

public class PlayerShoot : PlayerStates
{
    public PlayerShoot(PlayerController player) : base(player) { }

    float timer;
    float fireCooldown;
    bool hasShoot;

    private Vector3 saveTargetPoint;

    public override void OnEnter()
    {
        timer = 0f;
        fireCooldown = 0f;
        hasShoot = false;

        //player.isPerformingAction = true;

        if (player.LockTarget != null &&
            player.LockTarget.isTargeting &&
            player.LockTarget.CurrentTarget != null)
        {
            saveTargetPoint = player.LockTarget.CurrentTarget.position;
        }
        else
        {
            saveTargetPoint = player.GetViewPoint();
        }

        Debug.Log("Shoot State");
    }

    public override void OnUpdate()
    {
        timer += Time.deltaTime;
        fireCooldown -= Time.deltaTime;

        if (!hasShoot && timer >= player.ShootData.shootTime)
        {
            Shoot();
            hasShoot = true;
        }

        if (player.Input.AttackBuffered && fireCooldown <= 0f)
        {
            player.ChangeActionState(player.shoot_State);
        }

        if (timer >= player.ShootData.shootTime + 0.1f)
        {
            player.ChangeActionState(player.iddeAction_State);
        }
    }

    public override void OnExit()
    {
        //player.isPerformingAction = false;
    }

    private void Shoot()
    {
        fireCooldown = player.ShootData.timeBtwShot;

        player.RequestShoot(saveTargetPoint);
    }
}