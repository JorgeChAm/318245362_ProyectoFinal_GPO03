using UnityEngine;

// Anima _DissolveAmount del material a lo largo de la vida del efecto.
// Pega un fade-in al spawn, mantiene el efecto visible, y pega un fade-out
// antes de que el GO sea destruido por CombatAnimator.
//
// SETUP:
// 1. Pegar este componente al GO con el Renderer (el cilindro hijo del prefab).
// 2. lifetime debe coincidir con el effectDuration del Magic SO.
// 3. fadeIn + fadeOut deben ser MENORES que lifetime, idealmente la suma < lifetime/2.
// 4. El material asignado al Renderer debe usar un Shader Graph con una property
//    Float llamada (Reference) "_DissolveAmount" conectada al Alpha del Master.
[RequireComponent(typeof(Renderer))]
public class DissolveAnimator : MonoBehaviour
{
    [Tooltip("Duración total del efecto. Debe coincidir con effectDuration del Magic SO.")]
    public float lifetime = 1.5f;

    [Tooltip("Tiempo que tarda en materializarse (dissolve in).")]
    public float fadeIn = 0.3f;

    [Tooltip("Tiempo que tarda en desvanecerse (dissolve out).")]
    public float fadeOut = 0.4f;

    [Tooltip("Reference name de la propiedad en el Shader Graph. Cambiar solo si renombraste la property en el Blackboard.")]
    public string dissolveProperty = "_DissolveAmount";

    [Tooltip("(Opcional) Reference name de una segunda propiedad para shaders con dissolve direccional. Si está poblada, este animator opera en MODO BIDIRECCIONAL: la propiedad principal solo controla la aparición y esta segunda solo la desaparición. Si está vacía, comportamiento clásico (la propiedad principal cicla 1→0→1).")]
    public string disappearProperty = "";

    private Material instanciaMaterial;
    private float tiempoSpawn;
    private int dissolveID;
    private int disappearID;
    private bool tieneDisappear;

    private void Start()
    {
        // .material crea automáticamente una INSTANCIA del material para que
        // animar este efecto no afecte a otros que usen el mismo material.
        instanciaMaterial = GetComponent<Renderer>().material;

        // Cachear el ID es más rápido que pasar el string en cada SetFloat.
        dissolveID = Shader.PropertyToID(dissolveProperty);

        tieneDisappear = !string.IsNullOrEmpty(disappearProperty);
        if (tieneDisappear)
            disappearID = Shader.PropertyToID(disappearProperty);

        tiempoSpawn = Time.time;

        // Arranca completamente disuelto para hacer el fade-in desde 1 hacia 0.
        instanciaMaterial.SetFloat(dissolveID, 1f);
        if (tieneDisappear)
            instanciaMaterial.SetFloat(disappearID, 1f);
    }

    private void Update()
    {
        float transcurrido = Time.time - tiempoSpawn;
        float dissolve;
        float disappear = 1f;

        if (transcurrido < fadeIn)
        {
            // Aparece: dissolve 1 → 0. En modo bidireccional, disappear se queda en 1.
            dissolve = 1f - (transcurrido / fadeIn);
        }
        else if (transcurrido > lifetime - fadeOut)
        {
            // Desaparece.
            float progreso = Mathf.Clamp01((transcurrido - (lifetime - fadeOut)) / fadeOut);

            if (tieneDisappear)
            {
                // Modo bidireccional: dissolve queda en 0 (ya plenamente
                // aparecido), y disappear baja 1 → 0 para encoger desde el
                // mismo lado en que apareció.
                dissolve = 0f;
                disappear = 1f - progreso;
            }
            else
            {
                // Modo clásico: la misma propiedad sube 0 → 1 (dissolve por
                // noise, sin direccionalidad).
                dissolve = progreso;
            }
        }
        else
        {
            // Período visible
            dissolve = 0f;
        }

        instanciaMaterial.SetFloat(dissolveID, dissolve);
        if (tieneDisappear)
            instanciaMaterial.SetFloat(disappearID, disappear);
    }
}
