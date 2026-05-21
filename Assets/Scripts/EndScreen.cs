using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Pantalla de fin de partida (victoria o derrota).
// SETUP EN UNITY — Combat_01:
// 1. Panel negro stretch total, color negro puro.
// 2. Hijo "MensajeFinal": TMP grande, centrado.
// 3. Hijo "TextoContinuar": TMP pequeno debajo, centrado.
//    Texto: "Presiona ESPACIO para continuar"
// 4. Desactivar el panel. Asignar referencias en el Inspector.
public class EndScreen : MonoBehaviour
{
    public static EndScreen Instance { get; private set; }

    [Header("Panel negro de fin de partida")]
    public GameObject panel;

    [Header("Texto principal (victoria / derrota)")]
    public TextMeshProUGUI mensaje;

    [Header("Texto secundario 'presiona para continuar'")]
    public TextMeshProUGUI textoContinuar;

    [Header("Nombre de la escena del menu principal")]
    public string escenaMenu = "Menu";

    private void Awake()
    {
        if (Instance != null) { Destroy(this); return; }
        Instance = this;
    }

    // victoria = true  → "Gracias por jugar"
    // victoria = false → "Fin del juego"
    public IEnumerator Mostrar(bool victoria)
    {
        panel.SetActive(true);
        mensaje.text = victoria ? "Gracias por jugar" : "Fin del juego";

        if (textoContinuar != null)
            textoContinuar.text = "Presiona ESPACIO para continuar";

        AudioManager.Instance?.PlayBGMFin();

        // Esperar un momento antes de aceptar input (evita pulsacion accidental)
        yield return new WaitForSeconds(1.2f);

        // Esperar a que el jugador pulse Espacio
        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

        SceneManager.LoadScene(escenaMenu);
    }
}
