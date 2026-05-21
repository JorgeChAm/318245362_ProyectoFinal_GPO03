using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Estructuras serializables del guardado.

[System.Serializable]
public class SavedItem
{
    public string itemDataName; // nombre del asset ItemData (sin extension)
    public int quantity;
}

[System.Serializable]
public class SaveData
{
    public float playerX, playerY, playerZ;
    public List<SavedItem> inventory = new List<SavedItem>();
}

// Sistema de guardado, clase estatica.
// Guarda en: Application.persistentDataPath/save.json
// Los ItemData deben estar en Resources/Items/ para poder cargarse por nombre.
// Ejemplo: Assets/Resources/Items/Small Potion.asset
public static class SaveSystem
{
    private static string SavePath => Application.persistentDataPath + "/save.json";

    public static bool HasSave() => File.Exists(SavePath);

    // Llama este metodo desde el menu de pausa o al volver del combate.
    public static void Save(Vector3 playerPos)
    {
        SaveData data = new SaveData
        {
            playerX = playerPos.x,
            playerY = playerPos.y,
            playerZ = playerPos.z
        };

        if (Inventory.inventory != null)
        {
            foreach (Item item in Inventory.inventory.ItemsList)
            {
                if (item.ItemData == null) continue;
                data.inventory.Add(new SavedItem
                {
                    itemDataName = item.ItemData.name,
                    quantity     = item.ItemQuantity
                });
            }
        }

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        Debug.Log($"[SaveSystem] Partida guardada en: {SavePath}");
    }

    public static SaveData Load()
    {
        if (!HasSave())
        {
            Debug.LogWarning("[SaveSystem] No existe archivo de guardado.");
            return null;
        }
        return JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("[SaveSystem] Guardado eliminado.");
        }
    }
}
