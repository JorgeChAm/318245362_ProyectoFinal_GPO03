using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Gestiona el submenú de selección de magias e items durante el combate.
public class SubMenu : MonoBehaviour
{
    public CombatManager combatManager;

    [Header("Menú Principal")]
    public GameObject actionMenu;
    public GameObject characterContainer;

    [Header("Scroll")]
    public Button buttonPrefab;
    public Button buttonCancelPrefab;
    public Transform contentPanel;

    public void OpenSubMenu(SubMenuType subMenuType)
    {
        gameObject.SetActive(true);
        actionMenu.SetActive(false);
        characterContainer.SetActive(false);

        foreach (Transform hijo in contentPanel)
            Destroy(hijo.gameObject);

        if (subMenuType == SubMenuType.Magic)
        {
            Magic[] magics = combatManager.GetCurrentCharacter().magics;
            if (magics != null)
            {
                for (int i = 0; i < magics.Length; i++)
                {
                    Button newButton = Instantiate(buttonPrefab, contentPanel);
                    TextMeshProUGUI text = newButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null)
                        text.text = magics[i].nombre;

                    Magic magiaSeleccionada = magics[i];
                    newButton.onClick.AddListener(() => OnMagicClicked(magiaSeleccionada));
                }
            }
        }
        else if (subMenuType == SubMenuType.Object)
        {
            List<Item> objectList = Inventory.inventory.ItemsList;
            if (objectList != null)
            {
                for (int i = 0; i < objectList.Count; i++)
                {
                    Button newButton = Instantiate(buttonPrefab, contentPanel);
                    TextMeshProUGUI text = newButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null)
                        text.text = objectList[i].nombre + " x" + objectList[i].ItemQuantity;

                    Item itemSeleccionado = objectList[i];
                    newButton.onClick.AddListener(() => OnObjectClicked(itemSeleccionado));
                }
            }
        }

        Button buttonCancel = Instantiate(buttonCancelPrefab, contentPanel);
        buttonCancel.onClick.AddListener(ReturnMainMenu);
    }

    private void ReturnMainMenu()
    {
        gameObject.SetActive(false);
        actionMenu.SetActive(true);
        characterContainer.SetActive(true);
    }

    // Cierra el submenú al final de la selección del último personaje.
    // NO muestra el actionMenu (EjecutarTurnos lo oculta), pero SÍ
    // restaura el characterContainer para que las barras de vida sigan visibles.
    private void CerrarSinBotones()
    {
        gameObject.SetActive(false);
        characterContainer.SetActive(true);
        // actionMenu permanece oculto — lo gestiona EjecutarTurnos
    }

    private void OnMagicClicked(Magic magiaSeleccionada)
    {
        combatManager.SelectAction(magiaSeleccionada);
        // Si era el último personaje, SelectAction ya disparó EjecutarTurnos.
        // En ese caso no volvemos al menú principal — EjecutarTurnos oculta el UI.
        // Si seguimos en selección (target pendiente o quedan personajes), sí volvemos.
        if (combatManager.EstaEnSeleccion)
            ReturnMainMenu();
        else
            CerrarSinBotones(); // ocultar submenú y restaurar barras, pero NO los botones
    }

    private void OnObjectClicked(Item itemSeleccionado)
    {
        combatManager.SelectAction(itemSeleccionado);
        if (combatManager.EstaEnSeleccion)
            ReturnMainMenu();
        else
            CerrarSinBotones();
    }

    public enum SubMenuType
    {
        Magic,
        Object
    }
}