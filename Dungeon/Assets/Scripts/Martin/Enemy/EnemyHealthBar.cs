using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthBar;

    public void UpdateHealthBar(float currentHp, float maxHp)
    {
        healthBar.value = currentHp / maxHp;
    }

}
