using UnityEngine;

[CreateAssetMenu(fileName = "NuevoItem", menuName = "Inventario/ItemData")]
public class ItemData : ScriptableObject
{
    public string nombre;
    public string descripcion;

    [Tooltip("HP que restaura este ítem al usarse en combate. 0 = sin efecto de cura.")]
    public int healAmount;
}
