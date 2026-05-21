using UnityEngine;

public enum MagicType
{
    Damage,
    Heal
}

// ScriptableObject que define los datos base de una magia.
[CreateAssetMenu(fileName = "NuevaMagia", menuName = "Characters/Magic")]
public class Magic : ScriptableObject
{
    [Header("Datos")]
    public string nombre;
    public string descripcion;
    public MagicType magicType;
    public bool esArea; // Si es true, afecta a todos los objetivos del bando
    public int valor;
    public int precision;
    public int usosMaximos;

    [Header("Animación y VFX")]
    [Tooltip("Clip de animación que se reproduce al lanzar la magia. El mismo clip se usa para todos los personajes que lancen esta magia. Su duración (clip.length) determina automáticamente cuánto tarda la animación.")]
    public AnimationClip castClip;
    [Tooltip("Porcentaje del clip donde se considera 'lanzado' el proyectil/efecto. 0 = inicio del clip, 0.5 = mitad, 1 = final. Ajustar al frame de impacto/swing/cast de la animación de esta magia en concreto.")]
    [Range(0f, 1f)]
    public float tiempoLanzamiento = 0.5f;
    [Tooltip("Tipo A — Prefab del proyectil que viaja del actor al receptor. Null si la magia es Tipo B (efecto sobre target sin viaje).")]
    public GameObject projectilePrefab;
    [Tooltip("Tipo B — Prefab del efecto que aparece directamente sobre el receptor (sin viaje). Para AOE, se spawnea uno por cada aliado/enemigo afectado. Null si la magia es Tipo A.")]
    public GameObject targetEffectPrefab;
    [Tooltip("Duración (segundos) que dura el efecto Tipo B antes de autodestruirse.")]
    public float effectDuration = 1.5f;

    [Header("Audio")]
    [Tooltip("Sonido que se reproduce al lanzar esta magia.")]
    public AudioClip audioClip;
}