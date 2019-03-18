using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSettings", menuName = "Settings/CharacterSettings")]
public class CharacterSettings : ScriptableObject
{
    public int maxHealth = 1;
    public int attackDamage = 1;
}
