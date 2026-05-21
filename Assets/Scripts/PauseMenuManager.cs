using UnityEngine;
using UnityEngine.SceneManagement;

// Anadir este script a cualquier GameObject persistente en DungeonExploration.
// Asignar las referencias en el Inspector.
public class PauseMenuManager : MonoBehaviour
{
    [Header("Panel de pausa")]
    public GameObject pauseMenu;

    [Header("Jugador (para guardar su posicion)")]
    public Transform playerTransform;

    [Header("Nombre de la escena del menu principal")]
    public string escenaMenu = "Menu";

    private bool pausado = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pausado) Reanudar();
            else Pausar();
        }
    }

    private void Pausar()
    {
        pauseMenu.SetActive(true);
        Time.timeScale = 0f;
        pausado = true;
    }

    public void Reanudar()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
        pausado = false;
    }

    // ── Boton GUARDAR ──
    public void Guardar()
    {
        if (playerTransform != null)
            SaveSystem.Save(playerTransform.position);
        else
            Debug.LogWarning("[PauseMenu] playerTransform no asignado, no se guarda la posicion.");

        Reanudar();
    }

    // ── Boton CARGAR ──
    public void Cargar()
    {
        Time.timeScale = 1f;
        pausado = false;
        // ExplorationLoader se encarga de restaurar posicion e inventario al arrancar la escena
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ── Boton SALIR AL MENU ──
    public void SalirAlMenu()
    {
        Time.timeScale = 1f;
        pausado = false;
        SceneManager.LoadScene(escenaMenu);
    }
}
