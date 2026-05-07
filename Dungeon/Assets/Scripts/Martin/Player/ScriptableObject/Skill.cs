using UnityEngine;

public abstract class Skill : ScriptableObject
{
    [Header("Info")]
    public string skillName;

    [Header("Cost")]
    public ResourceType resourceType;
    public float cost;

    [Header("Casting")]
    public float castTime;
    public float cooldown;
    public float actionTime;

    [Header("Animtion")]
    public string castAnimation;
    public string actionAnimation;

    public abstract void LocalExecute(PlayerController player, Vector3 targetPoint);
    public abstract void ServerExecute(PlayerController player, Vector3 targetPoint);
}
