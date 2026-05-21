using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Gestiona el menú principal de acciones durante la fase de selección del combate.
public class ActionMenu : MonoBehaviour
{
    public Button atackBtn;
    public Button defenseBtn;
    public Button magicBtn;
    public Button objectBtn;
    public GameObject characterContainer;
    public SubMenu subMenu;
    public CombatManager combatManager;

    private TextMeshProUGUI magicBtnText;
    private const string LABEL_MAGIA    = "Magia";
    private const string LABEL_CANCELAR = "Cancelar";

    private void Start()
    {
        atackBtn.onClick.AddListener(AtackBtn_Click);
        defenseBtn.onClick.AddListener(DefenseBtn_Click);
        magicBtn.onClick.AddListener(MagicBtn_Click);
        objectBtn.onClick.AddListener(ObjectBtn_Click);

        // Obtener referencia al texto del botón de magia para cambiarlo dinámicamente
        magicBtnText = magicBtn.GetComponentInChildren<TextMeshProUGUI>();
    }

    // Bloquea botones mientras se espera la selección de un objetivo.
    // El botón de magia cambia su label a "Cancelar" para revertir la elección.
    private void Update()
    {
        bool waiting = combatManager.esperandoObjetivo;

        atackBtn.interactable   = !waiting;
        defenseBtn.interactable = !waiting;
        objectBtn.interactable  = !waiting;

        if (magicBtnText != null)
            magicBtnText.text = waiting ? LABEL_CANCELAR : LABEL_MAGIA;
    }

    private void AtackBtn_Click()
    {
        combatManager.SelectAction(CombatAction.Attack);
    }

    private void DefenseBtn_Click()
    {
        combatManager.SelectAction(CombatAction.Defense);
    }

    private void MagicBtn_Click()
    {
        if (combatManager.esperandoObjetivo)
            combatManager.CancelarSeleccion();
        else
            subMenu.OpenSubMenu(SubMenu.SubMenuType.Magic);
    }

    private void ObjectBtn_Click()
    {
        subMenu.OpenSubMenu(SubMenu.SubMenuType.Object);
    }
}