using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Characters/Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterId;

    public string characterName;

    [TextArea]
    public string description;

    public Sprite portrait;

    public GameObject playerPrefab;


    [Header("Starting Stats")]
    public int startingHealth = 100;
    public int startingMana = 50;
    public int startingStamina = 100;
}
