using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Singleton que gestiona las notificaciones de recogida de ítems.
// Muestra un stack de toasts ascendentes: el más nuevo aparece abajo,
// los anteriores suben, y el más antiguo se desvanece.
//
// SETUP EN UNITY:
// 1. Crear un Canvas en la escena de exploración (o reusar el existente).
// 2. Dentro del Canvas, crear un GameObject vacío "ToastContainer" con
//    RectTransform anclado en la esquina inferior-izquierda (anchor: 0,0 / pivot: 0,0).
//    Posición sugerida: x=30, y=40.
// 3. Anadir este script a cualquier GameObject persistente (o al Canvas).
// 4. Asignar toastPrefab (ver punto 5) y toastContainer.
// 5. Crear el prefab "PickupToastPrefab":
//    - GameObject con RectTransform (width~220, height~30), CanvasGroup, PickupToast.
//    - Child "Label": TextMeshProUGUI con el texto de prueba "+ Ítem".
//      Fuente sugerida: negrita, tamano 18, color blanco, outline negro opcional.
public class PickupNotifier : MonoBehaviour
{
    public static PickupNotifier Instance { get; private set; }

    [Header("Prefab del toast")]
    [Tooltip("Prefab que contiene RectTransform + CanvasGroup + PickupToast + TextMeshProUGUI.")]
    public GameObject toastPrefab;

    [Header("Contenedor")]
    [Tooltip("RectTransform anclado en la esquina inferior de la pantalla. Los toasts se instancian aquí.")]
    public RectTransform toastContainer;

    [Header("Ajustes visuales")]
    [Tooltip("Separación vertical entre toasts apilados (px). Debe ser >= altura del toast.")]
    public float toastSpacing   = 50f;
    [Tooltip("Tiempo total de vida de cada toast (segundos).")]
    public float toastDuration  = 2.8f;
    [Tooltip("Velocidad de ascenso de cada toast (px/segundo).")]
    public float risePerSecond  = 22f;

    private readonly List<PickupToast> activeToasts = new List<PickupToast>();

    private void Awake()
    {
        if (Instance != null) { Destroy(this); return; }
        Instance = this;
    }

    // Llama este método desde cualquier lugar para mostrar una notificación.
    public void Show(string nombreItem)
    {
        if (toastPrefab == null || toastContainer == null)
        {
            Debug.LogWarning("PickupNotifier: falta asignar toastPrefab o toastContainer en el Inspector.");
            return;
        }

        // Limpiar referencias nulas (toasts que ya se destruyeron solos)
        activeToasts.RemoveAll(t => t == null);

        // Desplazar los toasts existentes hacia arriba para hacer sitio al nuevo
        foreach (PickupToast toast in activeToasts)
        {
            if (toast == null) continue;
            RectTransform rt = toast.GetComponent<RectTransform>();
            rt.anchoredPosition += Vector2.up * toastSpacing;
        }

        // Instanciar el nuevo toast en la posición base (Y=0 del container)
        GameObject go = Instantiate(toastPrefab, toastContainer);
        RectTransform newRt = go.GetComponent<RectTransform>();
        newRt.anchoredPosition = Vector2.zero;

        PickupToast nuevoToast = go.GetComponent<PickupToast>();
        nuevoToast.SetText($"+ {nombreItem}");

        activeToasts.Add(nuevoToast);
        StartCoroutine(EjecutarToast(nuevoToast));
    }

    private IEnumerator EjecutarToast(PickupToast toast)
    {
        yield return StartCoroutine(toast.Animar(toastDuration, risePerSecond));
        activeToasts.Remove(toast);
    }
}
