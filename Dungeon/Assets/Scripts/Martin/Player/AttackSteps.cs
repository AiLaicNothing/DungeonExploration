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

    [Header("HitBox")]
    public float hitTime;
    public Vector3 hitBoxSize = new Vector3(1, 1, 2);
    public Vector3 hitBoxOffSet = new Vector3(0, 0, 1);

}
