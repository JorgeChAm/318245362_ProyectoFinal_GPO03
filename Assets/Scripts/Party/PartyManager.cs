using System.Collections.Generic;
using UnityEngine;

// Singleton persistente entre escenas. Almacena y sincroniza el estado de la party.
public class PartyManager : MonoBehaviour
{
    public static PartyManager PartyMana;
    public CharacterData[] characterDatas;
    public List<CharacterSaveData> characterSaveDatas = new List<CharacterSaveData>();

    private void Awake()
    {
        if (PartyMana != null)
        {
            // Destruir solo este componente, no el GameObject completo.
            // Así no se lleva consigo otros scripts del mismo objeto (ej. CombatManager).
            Destroy(this);
        }
        else
        {
            PartyMana = this;
            DontDestroyOnLoad(gameObject);
            foreach (CharacterData data in characterDatas)
                characterSaveDatas.Add(new CharacterSaveData(data));
        }
    }

    public void LoadIntoStats(CharacterStats[] partyStats)
    {
        for (int i = 0; i < partyStats.Length && i < characterSaveDatas.Count; i++)
        {
            if (partyStats[i] == null) continue;

            CharacterStats stats = partyStats[i];
            CharacterSaveData save = characterSaveDatas[i];

            stats.characterData = save.characterData;
            stats.currentXp = save.currentXp;

            if (!save.hasBeenSaved)
            {
                // Primer arranque: respetamos initialLv y HP completa que ya puso Start().
                stats.RecalcularStats();
                stats.currentHealth = stats.currentMaxHealth;
                stats.RefreshHealthUI();
                continue;
            }

            stats.currentLv = save.currentLv;

            // Preservamos el ratio de HP al pasar de un nivel a otro.
            int maxHealthAtSavedLv = CharacterStats.StatLvUpdate(save.characterData.vidaBase, save.currentLv) * CharacterStats.StatScale;
            float healthRatio = maxHealthAtSavedLv > 0
                ? Mathf.Clamp01((float)save.currentHealth / maxHealthAtSavedLv)
                : 1f;

            stats.RecalcularStats();
            stats.currentHealth = Mathf.Max(1, Mathf.RoundToInt(stats.currentMaxHealth * healthRatio));
            stats.RefreshHealthUI();
        }
    }

    public void SaveFromStats(CharacterStats[] partyStats)
    {
        for (int i = 0; i < partyStats.Length && i < characterSaveDatas.Count; i++)
        {
            if (partyStats[i] == null) continue;

            CharacterStats stats = partyStats[i];
            CharacterSaveData save = characterSaveDatas[i];

            save.currentLv = stats.currentLv;
            save.currentXp = stats.currentXp;
            save.currentHealth = stats.currentHealth;
            save.hasBeenSaved = true;
        }
    }
}