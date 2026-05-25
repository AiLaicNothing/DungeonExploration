using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Frenzy")]
public class Frenzy : Skill
{
    [Header("Buff")]
    public float increasePorcentage;
    public float duration;
    public float timer;

    [Header("Sfx")]
    public GameObject sfx;

    private float originalValue;
    private Coroutine frenzyRoutine;
    public override void LocalExecute(PlayerController player, Vector3 targetPoint)
    {
        throw new System.NotImplementedException();
    }

    public override void ServerExecute(PlayerController player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        if (frenzyRoutine != null)
        {
            player.StartCoroutine(FrenzyState(player));
        }

        frenzyRoutine = player.StartCoroutine(FrenzyState(player));
    }

    private IEnumerator FrenzyState(PlayerController player)
    {
        if (sfx != null)
        {
            GameObject obj = Instantiate(sfx, player.transform.position, Quaternion.identity);
            Destroy(obj, duration);
        }

        // store original value
        float originalValue = player.Stats.PhysicalDamage.CurrentValue;

        // calculate bonus
        float bonus = originalValue * (increasePorcentage / 100f);

        // apply buff
        player.Stats.PhysicalDamage.Modify(originalValue + bonus);

        Debug.Log($"Frenzy ON -> Damage: {player.Stats.PhysicalDamage.CurrentValue}");

        // wait duration
        yield return new WaitForSeconds(duration);

        // restore original value
        player.Stats.PhysicalDamage.Modify(originalValue);

        Debug.Log($"Frenzy OFF -> Damage: {player.Stats.PhysicalDamage.CurrentValue}");

        frenzyRoutine = null;
    }
}
