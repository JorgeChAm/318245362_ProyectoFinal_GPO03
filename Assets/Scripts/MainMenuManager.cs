using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Controla los botones del menu principal.
// Anadir este script a un GameObject en la escena MainMenu.
public class MainMenuManager : MonoBehaviour
{
    [Header("Nombre de la escena de exploracion")]
    public string escenaExploracion = "DungeonExploration";

    [Header("Boton Continuar (para desactivarlo si no hay guardado)")]
    public Button botonContinuar;

    private void Start()
    {
        // Desactiva "Continuar" si no existe archivo de guardado
        if (botonContinuar != null)
            botonContinuar.interactable = SaveSystem.HasSave();
    }

    // ── Asignar al boton "Nuevo Juego" en el Inspector ──
    public void NuevoJuego()
    {
        // Borrar guardado anterior e inventario en memoria
        SaveSystem.DeleteSave();

        if (Inventory.inventory != null)
            Inventory.inventory.ItemsList.Clear();

        // Resetear party al estado inicial
        if (PartyManager.PartyMana != null)
        {
            foreach (var save in PartyManager.PartyMana.characterSaveDatas)
                save.hasBeenSaved = false;
        }

        SceneManager.LoadScene(escenaExploracion);
    }

    // ── Asignar al boton "Continuar" en el Inspector ──
    public void Continuar()
    {
        if (!SaveSystem.HasSave()) return;
        // ExplorationLoader en la escena de exploracion se encarga de restaurar
        // la posicion del jugador y el inventario al arrancar.
        SceneManager.LoadScene(escenaExploracion);
    }

    // ── Asignar al boton "Salir" en el Inspector ──
    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
