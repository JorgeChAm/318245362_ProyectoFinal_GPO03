using System.Collections;
using UnityEngine;
using TMPro;

// Un solo "toast" de recogida. Se instancia desde PickupNotifier,
// sube flotando y se desvanece, luego se autodestruye.
[RequireComponent(typeof(CanvasGroup))]
public class PickupToast : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    private void Awake()
    {
        canvasGroup  = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void SetText(string texto)
    {
        TextMeshProUGUI tmp = GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = texto;
    }

    // Sube a razón de `risePerSecond` px/s durante `duration` segundos.
    // Usa velocidad incremental para que desplazamientos externos (nuevo toast
    // que empuja este hacia arriba) no sean pisados por el lerp.
    public IEnumerator Animar(float duration, float risePerSecond)
    {
        canvasGroup.alpha = 1f;
        float tiempoFade = duration * 0.4f;
        float elapsed    = 0f;

        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            // Movimiento incremental — respeta desplazamientos externos
            rectTransform.anchoredPosition += Vector2.up * risePerSecond * dt;

            // Fade progresivo en el último 60 % del tiempo
            if (elapsed > tiempoFade)
                canvasGroup.alpha = 1f - ((elapsed - tiempoFade) / (duration - tiempoFade));

            yield return null;
        }

        Destroy(gameObject);
    }
}
