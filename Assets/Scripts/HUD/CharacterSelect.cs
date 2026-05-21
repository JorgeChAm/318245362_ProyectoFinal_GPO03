using UnityEngine;
using UnityEngine.EventSystems;

// Gestiona la selección de un personaje aliado como objetivo durante el combate.
public class CharacterSelect : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referencias")]
    public CharacterStats misEstadisticas;
    public CombatManager combatManager;

    [Header("Visuales")]
    public GameObject marcoVisual;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (combatManager.esperandoObjetivo && !misEstadisticas.IsDefeated)
            marcoVisual.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        marcoVisual.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (combatManager.esperandoObjetivo && !misEstadisticas.IsDefeated)
        {
            marcoVisual.SetActive(false);
            combatManager.ElegirObjetivoAliado(misEstadisticas);
        }
    }
}