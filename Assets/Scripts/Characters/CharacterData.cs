using UnityEngine;
[CreateAssetMenu(fileName = "NuevoPersonaje", menuName = "Characters/PartyMember")]
public class CharacterData : ScriptableObject
{
    public string nombre;
    public string descripcion;
    public int  ataqueFisicoBase,
        poderMagico,
        defensaBase,
        vidaBase,
        velocidadBase,
        evasionBase,
        precisionBase;

    [Tooltip("Cuántas acciones ejecuta este personaje por ronda. 1 = normal. 2-3 = boss difícil.")]
    public int ataquesPorTurno = 1;

    public Magic[] magias;
}