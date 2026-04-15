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

    [Header("Animtion")]
    public string castAnimation;
    public string actionAnimation;

    public abstract void Execute(PlayerController player);
}
