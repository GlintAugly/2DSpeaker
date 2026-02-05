using UnityEngine;
using System;

[Serializable]
public class CharacterData : JsonReader<CharacterData>
{
    public CharacterDataEntry[] characters;

    public CharacterDataEntry GetCharacterDataItem(string characterName)
    {
        return Array.Find(characters, item => item.name == characterName);
    }
}
