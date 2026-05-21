using UnityEngine;
using System.Collections.Generic;

// Singleton persistente entre escenas. Gestiona el inventario global del jugador.
public class Inventory : MonoBehaviour
{
    public static Inventory inventory;
    public List<Item> ItemsList = new List<Item>();

    private void Awake()
    {
        if (inventory != null)
        {
            // Destruir solo este componente, no el GameObject completo.
            // Así no se lleva consigo otros scripts del mismo objeto (ej. CombatManager).
            Destroy(this);
        }
        else
        {
            inventory = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void AddItem(Item item)
    {
        Item itemExist = SearchItem(item);
        if (itemExist != null)
        {
            itemExist.ItemQuantity += item.ItemQuantity;
        }
        else
        {
            ItemsList.Add(item);
        }
    }

    public void RemoveItem(Item item)
    {
        Item itemExist = SearchItem(item);
        if (itemExist == null) return;

        if (itemExist.ItemQuantity <= 1)
        {
            ItemsList.Remove(itemExist);
        }
        else
        {
            itemExist.ItemQuantity -= 1;
        }
    }

    // Busca un item en la lista por referencia al mismo ScriptableObject.
    // Comparar por ItemData evita que dos SO distintos con el mismo string se fusionen.
    public Item SearchItem(Item item)
    {
        return ItemsList.Find(i => i.ItemData == item.ItemData);
    }
}