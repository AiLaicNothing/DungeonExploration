using UnityEngine;
using UnityEngine.UI;

public class EnemyStaggerBar : MonoBehaviour
{
    [SerializeField] private Slider staggerBar;
    public void UpdateStaggerhBar(float currentHp, float maxHp)
    {
        staggerBar.value = currentHp / maxHp;
    }
}
