using UnityEngine;

/// <summary>
/// Anima la intensidad de un Point Light con Perlin noise para simular
/// el parpadeo orgánico de una antorcha.
/// Cada instancia usa una semilla aleatoria para que las antorchas no sincronicen.
/// </summary>
[RequireComponent(typeof(Light))]
public class FlickerAnimator : MonoBehaviour
{
    [Header("Intensidad")]
    [Tooltip("Intensidad base de la luz en reposo.")]
    public float baseIntensity = 1.5f;

    [Tooltip("Cuánto puede desviarse la intensidad arriba/abajo del base.")]
    public float flickerAmount = 0.45f;

    [Header("Frecuencia")]
    [Tooltip("Velocidad del parpadeo. Valores altos = más nervioso.")]
    public float flickerSpeed = 2.8f;

    [Tooltip("Segunda octava de ruido para variaciones lentas de 'respiración'.")]
    public float breathSpeed = 0.4f;
    public float breathAmount = 0.25f;

    // ── internos ──────────────────────────────────────────────────────────────
    private Light _light;
    private float _seed;

    void Awake()
    {
        _light = GetComponent<Light>();
        // Semilla única por instancia para evitar sincronización entre antorchas
        _seed = Random.Range(0f, 1000f);
    }

    void Update()
    {
        // Perlin noise en dos frecuencias: parpadeo rápido + respiración lenta
        float flicker = Mathf.PerlinNoise(_seed + Time.time * flickerSpeed, 0f);
        float breath  = Mathf.PerlinNoise(_seed + Time.time * breathSpeed,  99f);

        // Remapear [0,1] → [-1,1] y aplicar
        float delta = (flicker - 0.5f) * 2f * flickerAmount
                    + (breath  - 0.5f) * 2f * breathAmount;

        _light.intensity = Mathf.Max(0f, baseIntensity + delta);
    }

#if UNITY_EDITOR
    // Permite previsualizar la luz en la Scene view sin entrar en Play Mode
    void OnValidate()
    {
        if (_light == null) _light = GetComponent<Light>();
        if (_light != null) _light.intensity = baseIntensity;
    }
#endif
}
