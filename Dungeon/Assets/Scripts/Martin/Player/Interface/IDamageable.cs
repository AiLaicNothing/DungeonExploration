using UnityEngine;

public interface  IDamageable 
{
    void TakeDamage(float damage, ThrowType throwType, Vector3 hitDir, float stunDuration, bool keepOnAir, float airLift);
}
