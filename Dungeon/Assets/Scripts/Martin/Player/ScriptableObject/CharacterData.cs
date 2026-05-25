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
}
