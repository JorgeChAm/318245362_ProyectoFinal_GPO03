using UnityEngine;

// Estado persistente de un personaje entre escenas.
[System.Serializable]
public class CharacterSaveData
{
    public CharacterData characterData;
    public int currentLv;
    public int currentXp;
    public int currentHealth;
    public bool hasBeenSaved;

    public CharacterSaveData(CharacterData characterData)
    {
        this.characterData = characterData;
        currentLv = 1;
        currentXp = 0;
        currentHealth = CharacterStats.StatLvUpdate(characterData.vidaBase, currentLv) * CharacterStats.StatScale;
        hasBeenSaved = false;
    }
}