using UnityEngine;

[System.Serializable]
public class Item
{
    public ItemData ItemData { get; private set; }
    public int ItemQuantity;

    // Campos de visualización en el Inspector, datos espejo de ItemData
    public string nombre, descripcion;

    public Item(ItemData itemData, int quantity)
    {
        ItemData = itemData;
        ItemQuantity = quantity;
        nombre = ItemData.nombre;
        descripcion = ItemData.descripcion;
    }
}