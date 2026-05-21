using UnityEngine;

// Anadir este script al GameManager (o cualquier GO) en la escena de exploracion.
// Al arrancar, detecta si existe un guardado y restaura posicion del jugador
// e inventario. Si no hay guardado, no hace nada (el jugador empieza desde el inicio).
//
// REQUISITO para que el inventario se restaure correctamente:
//   Los assets ItemData deben estar dentro de una carpeta Resources/Items/
//   Ejemplo: Assets/Resources/Items/Small Potion.asset
public class ExplorationLoader : MonoBehaviour
{
    [Header("Transform del jugador en la escena")]
    public Transform playerTransform;

    private void Start()
    {
        SaveData data = SaveSystem.Load();
        if (data == null) return; // Nuevo juego — no restaurar nada

        // ── Restaurar posicion del jugador ──
        if (playerTransform != null)
            playerTransform.position = new Vector3(data.playerX, data.playerY, data.playerZ);

        // ── Restaurar inventario ──
        if (Inventory.inventory != null && data.inventory != null)
        {
            Inventory.inventory.ItemsList.Clear();

            foreach (SavedItem savedItem in data.inventory)
            {
                // Busca el ItemData en Resources/Items/<nombre>
                ItemData itemData = Resources.Load<ItemData>($"Items/{savedItem.itemDataName}");

                if (itemData != null)
                {
                    Inventory.inventory.AddItem(new Item(itemData, savedItem.quantity));
                }
                else
                {
                    Debug.LogWarning($"[ExplorationLoader] No se encontro ItemData '{savedItem.itemDataName}' " +
                                     $"en Resources/Items/. Mueve el asset a esa carpeta.");
                }
            }
        }

        Debug.Log("[ExplorationLoader] Partida restaurada.");
    }
}
