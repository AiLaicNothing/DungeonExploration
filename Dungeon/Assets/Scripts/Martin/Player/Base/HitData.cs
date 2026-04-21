using UnityEngine;

[System.Serializable]
public class HitData 
{
    public float damageMultiplier = 1f;

    public ThrowType throwType;
    public float stunDuration;

    public bool keepInAir;
    public float airLiftForce;
}
