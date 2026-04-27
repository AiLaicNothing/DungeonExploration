using UnityEngine;

[CreateAssetMenu(menuName = ("Shoot Data"))]
public class ShootData : ScriptableObject
{
    [Header("Info")]
    public string shootName;
    public string shootAnimation;

    [Header("Shoot")]
    //the time it take to shoot so it synch with the anim
    public float shootTime;
    public float timeBtwShot;
    public GameObject proyectilePrefab;
    public float proyectileSpeed;
    public HitData hitData;
}
