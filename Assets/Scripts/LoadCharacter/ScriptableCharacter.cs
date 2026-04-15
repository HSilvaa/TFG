using UnityEngine;

[CreateAssetMenu(fileName = "CharacterInfo", menuName = "Character/Character Info")]
public class ScriptableCharacter : ScriptableObject
{
    public int characterId;
    public string characterName;
    public string characterAge;
    public string characterDescription;
    public string characterEpoca;
}
