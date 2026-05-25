using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Skills/NightVeil")]
public class NightVeil : Skill
{
    [Header("Buff")]
    public float increasePorcentage;
    public float duration;
    public float timer;

    [Header("Sfx")]
    public GameObject sfx;
    [SerializeField] private int sfxCount = 3;
    [SerializeField] private float orbitRadius = 1.5f;
    [SerializeField] private float orbitSpeed = 120f;
    [SerializeField] private float heightOffset = 1.2f;

    private float originalValue;
    private Coroutine nightVeilRoutine;
    public override void LocalExecute(PlayerController player, Vector3 targetPoint)
    {
        throw new System.NotImplementedException();
    }

    public override void ServerExecute(PlayerController player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        if (nightVeilRoutine != null)
        {
            player.StartCoroutine(NightVeilState(player));
        }

        nightVeilRoutine = player.StartCoroutine(NightVeilState(player));
    }

    private IEnumerator NightVeilState(PlayerController player)
    {
        float originalValue = player.Stats.ManaRegen.CurrentValue;

        float bonus = originalValue * (increasePorcentage / 100f);

        player.Stats.ManaRegen.Modify(originalValue + bonus);

        List<GameObject> spawnedSfx = null;

        // ONLY create and update SFX if prefab exists
        if (sfx != null)
        {
            spawnedSfx = new List<GameObject>();

            for (int i = 0; i < sfxCount; i++)
            {
                float angle = (360f / sfxCount) * i;

                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * orbitRadius;

                Vector3 spawnPos = player.transform.position + offset + Vector3.up * heightOffset;

                GameObject obj = Instantiate(sfx, spawnPos, Quaternion.identity);

                spawnedSfx.Add(obj);
            }
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            // ONLY update orbit if SFX exists
            if (spawnedSfx != null)
            {
                for (int i = 0; i < spawnedSfx.Count; i++)
                {
                    if (spawnedSfx[i] == null) continue;

                    float angle = ((360f / sfxCount) * i) + (timer * orbitSpeed);

                    Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * orbitRadius;

                    Vector3 targetPos = player.transform.position +  offset + Vector3.up * heightOffset;

                    spawnedSfx[i].transform.position = targetPos;

                    spawnedSfx[i].transform.Rotate( Vector3.up * 360f * Time.deltaTime);
                }
            }

            yield return null;
        }

        // restore stat
        player.Stats.ManaRegen.Modify(originalValue);

        // ONLY destroy if list exists
        if (spawnedSfx != null)
        {
            foreach (GameObject obj in spawnedSfx)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }

        nightVeilRoutine = null;
    }
}

    