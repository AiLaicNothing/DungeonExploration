using UnityEngine;

[CreateAssetMenu(menuName = "EnemyStats")]
public class EnemyStats : ScriptableObject
{
    [Header("Stats")]
    public float maxHp;
    public float damage;
    public float moveSpeed;

    [Header("Stagger")]
    public bool hasStagger;
    public float staggerTreshold;
    public float staggerDuration;
    public float timeResetStagger;
    public bool canStaggerMultiplesTimes;

    [Header("Forces")]
    public float pushForce = 5f;
    public float airForce = 7f;

    [Header("Air Control")]
    public float airHangTime = 0.4f;
    public float fallGravityMultiplier = 2f;
}
