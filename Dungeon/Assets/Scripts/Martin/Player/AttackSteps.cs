using UnityEngine;

[System.Serializable]
public class AttackSteps 
{
    [Header("Animation")]
    public string name;
    public float duration;

    [Header("Combo")]
    public float comboWindowStart;
    public float comboWindowEnd;
}
