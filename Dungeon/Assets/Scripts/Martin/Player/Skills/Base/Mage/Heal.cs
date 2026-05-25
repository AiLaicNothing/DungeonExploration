using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Frenzy")]
public class Heal : Skill
{
    [Header("Buff")]
    public float HealPorcentage;

    [Header("Sfx")]
    public GameObject sfx;

    public override void LocalExecute(PlayerController player, Vector3 targetPoint)
    {
        throw new System.NotImplementedException();
    }

    public override void ServerExecute(PlayerController player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        HealPlayer(player);
    }

    private void HealPlayer(PlayerController player)
    {
        float maxHealth = player.Stats.Health.Max;

        float currentHealth = player.Stats.Health.CurrentValue;

        float healAmount = maxHealth * (HealPorcentage / 100f);

        float finalHealth = currentHealth + healAmount;

        // prevent overheal
        finalHealth = Mathf.Clamp(finalHealth, 0, maxHealth);

        // apply heal
        player.Stats.Health.Modify(finalHealth);

        Debug.Log($"Healed -> {healAmount}");


        if (sfx != null)
        {
            GameObject obj = Instantiate(sfx, player.transform.position, Quaternion.identity);
            Destroy(obj, 3f);
        }
    }
}


