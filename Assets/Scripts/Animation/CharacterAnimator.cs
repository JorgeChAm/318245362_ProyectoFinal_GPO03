using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Componente por personaje. Envuelve su Animator y expone coroutines
// para que CombatAnimator pueda esperar a que cada clip termine.
//
// SETUP EN UNITY:
// 1. Anadir este script al GameObject raíz del personaje (o al hijo con el Animator).
// 2. Ajustar las duraciones en el Inspector para que coincidan con los clips de Mixamo.
// 3. Crear un child vacío en la mano del personaje y asignarlo a "puntoLanzamiento".
// 4. El Animator del modelo debe usar un AnimatorOverrideController con un clip
//    placeholder (por defecto "Magic_Base") en el estado Magic, que se swapea
//    en runtime desde SetMagicClip.
public class CharacterAnimator : MonoBehaviour
{
    private Animator animator;

    [Header("Duraciones de clips (segundos) — ajustar según Mixamo)")]
    public float duracionAtaque  = 1.0f;
    [Tooltip("FALLBACK. Solo se usa cuando una magia no tiene castClip asignado. Si la magia tiene clip, su duración se lee automáticamente de castClip.length.")]
    public float duracionMagia   = 1.2f;
    public float duracionDefensa = 0.8f;
    public float duracionUsoItem = 1.0f;
    public float duracionTakeHit = 0.6f;
    public float duracionMorir   = 1.8f;

    [Tooltip("Velocidad de reproducción del clip UseItem. 1 = normal, 1.5 = 50% más rápido. Ajustar hasta que la animación acabe antes del siguiente mensaje.")]
    public float velocidadUsoItem = 1.5f;

    [Header("Punto de lanzamiento de proyectil")]
    [Tooltip("Child vacío posicionado en la mano o punto de lanzamiento del personaje.")]
    public Transform puntoLanzamiento;

    [Header("Punto de impacto (a dónde apuntan los proyectiles que reciben)")]
    [Tooltip("Child vacío a la altura del torso del personaje. CombatAnimator lo usa como destino del proyectil para que no se vea cayendo a los pies. Si está vacío, usa el transform del personaje (los pies).")]
    public Transform puntoImpacto;

    [Header("Tiempo dentro del clip de magia en que se lanza el proyectil (0-1)")]
    [Tooltip("FALLBACK. Solo se usa cuando una magia no tiene tiempoLanzamiento configurado en su SO. Cada Magic SO ya define su propio tiempoLanzamiento porque cada animación de magia tiene su frame de impacto en distinto lugar.")]
    [Range(0f, 1f)]
    public float tiempoLanzamiento = 0.5f;

    [Header("Tiempo dentro del clip de ataque básico en que se lanza el proyectil (0-1)")]
    [Tooltip("Mismo concepto que tiempoLanzamiento pero para el clip de Attack. Ajustar al frame del impacto/swing del puno.")]
    [Range(0f, 1f)]
    public float tiempoLanzamientoAtaque = 0.5f;

    [Header("Audio — SFX propios del personaje")]
    [Tooltip("Sonido al recibir dano. Deja vacio para usar el del AudioManager.")]
    public AudioClip sfxTakeHit;
    [Tooltip("Sonido al morir.")]
    public AudioClip sfxDie;

    [Header("Override de magia")]
    [Tooltip("Nombre del clip placeholder en el Animator Controller base; SetMagicClip lo usa como clave para inyectar el clip de la magia activa.")]
    public string magicPlaceholderClipName = "Magic_Base";

#if UNITY_EDITOR
    [Header("Preview en Editor — solo para los botones del Inspector")]
    [Tooltip("Transform al que vuelan los proyectiles cuando pruebas con los botones del Inspector. Si está vacío, el proyectil sale recto hacia adelante (5 unidades).")]
    public Transform previewTarget;
    [Tooltip("Prefab del proyectil del ataque básico SOLO para los botones del Editor. En runtime se usa el del CombatAnimator.")]
    public GameObject previewBasicAttackProjectile;
#endif

    // ──────────────────────────────────────────
    // PARÁMETROS DEL ANIMATOR (string → hash para performance)
    // ──────────────────────────────────────────
    private static readonly int HashAtaque   = Animator.StringToHash("Attack");
    private static readonly int HashMagia    = Animator.StringToHash("Magic");
    private static readonly int HashDefensa  = Animator.StringToHash("Defend");
    private static readonly int HashUsoItem  = Animator.StringToHash("UseItem");
    private static readonly int HashTakeHit  = Animator.StringToHash("TakeHit");
    private static readonly int HashMorir    = Animator.StringToHash("Die");
    private static readonly int HashCaminar  = Animator.StringToHash("IsWalking"); // solo Matarael

    private void Awake()
    {
        // Permite que el script viva en el GO raíz (con la lógica) y el Animator
        // en un hijo (el modelo Mixamo). Si está en el mismo GO también funciona.
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    // ──────────────────────────────────────────
    // COROUTINES DE COMBATE
    // ──────────────────────────────────────────

    // Ataque básico partido en dos mitades, igual que las magias: la primera
    // mitad anima hasta el frame de "impacto/lanzamiento" y devuelve el control
    // a CombatAnimator para que instancie el proyectil; la segunda mitad anima
    // el resto del clip mientras el proyectil viaja.
    public IEnumerator PlayAttackUntilLaunch()
    {
        animator.SetTrigger(HashAtaque);
        yield return new WaitForSeconds(duracionAtaque * tiempoLanzamientoAtaque);
    }

    public IEnumerator PlayAttackRemainder()
    {
        yield return new WaitForSeconds(duracionAtaque * (1f - tiempoLanzamientoAtaque));
    }

    // Conveniencia: reproduce el clip completo de ataque sin instanciar proyectil.
    // CombatAnimator NO lo usa (parte el clip manualmente), pero el Editor de
    // preview sí, para validar la animación sin necesidad de proyectil.
    public IEnumerator PlayAttack()
    {
        yield return StartCoroutine(PlayAttackUntilLaunch());
        yield return StartCoroutine(PlayAttackRemainder());
    }

    // Devuelve el control en el momento de lanzamiento (tiempoLanzamiento × duración)
    // para que CombatAnimator pueda instanciar el proyectil en ese momento.
    // Luego espera el resto del clip.
    //
    // La duración sale automáticamente de magia.castClip.length, así no hay que
    // configurarla a mano en cada personaje. El tiempoLanzamiento sale del
    // Magic SO porque cada animación tiene su frame de impacto en distinto lugar.
    // Si la magia es null o no tiene clip asignado, se usa el fallback del personaje.
    public IEnumerator PlayMagicUntilLaunch(Magic magia)
    {
        animator.SetTrigger(HashMagia);
        float duracion = GetDuracionMagia(magia);
        float t        = GetTiempoLanzamientoMagia(magia);
        yield return new WaitForSeconds(duracion * t);
    }

    public IEnumerator PlayMagicRemainder(Magic magia)
    {
        float duracion = GetDuracionMagia(magia);
        float t        = GetTiempoLanzamientoMagia(magia);
        yield return new WaitForSeconds(duracion * (1f - t));
    }

    // Lee la duración del clip de la magia. Si la magia no tiene clip (o la
    // magia es null), cae al fallback duracionMagia configurado por personaje.
    private float GetDuracionMagia(Magic magia)
    {
        if (magia != null && magia.castClip != null && magia.castClip.length > 0f)
            return magia.castClip.length;
        return duracionMagia;
    }

    // Lee el tiempo de lanzamiento de la magia. Si la magia es null, cae al
    // fallback del personaje.
    private float GetTiempoLanzamientoMagia(Magic magia)
    {
        if (magia != null) return magia.tiempoLanzamiento;
        return tiempoLanzamiento;
    }

    // Swapea el clip del estado Magic en el AnimatorOverrideController del personaje.
    // CombatAnimator lo llama justo antes de disparar el trigger, para que la magia
    // que esté lanzando el personaje use su propio clip compartido.
    public void SetMagicClip(AnimationClip clip)
    {
        if (clip == null || animator == null) return;

        AnimatorOverrideController aoc = animator.runtimeAnimatorController as AnimatorOverrideController;
        if (aoc == null)
        {
            Debug.LogWarning($"{name}: el Animator no usa un AnimatorOverrideController; no se puede swapear el clip de magia.");
            return;
        }

        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(aoc.overridesCount);
        aoc.GetOverrides(overrides);

        for (int i = 0; i < overrides.Count; i++)
        {
            if (overrides[i].Key != null && overrides[i].Key.name == magicPlaceholderClipName)
            {
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, clip);
                aoc.ApplyOverrides(overrides);
                return;
            }
        }

        Debug.LogWarning($"{name}: no se encontró el clip placeholder '{magicPlaceholderClipName}' en el override.");
    }

    // Defend es un Bool (no Trigger). Se mantiene en true hasta que
    // CombatManager lo limpie al inicio de la siguiente ronda de ejecución.
    public IEnumerator PlayDefend()
    {
        animator.SetBool(HashDefensa, true);
        yield return new WaitForSeconds(duracionDefensa);
    }

    public void ExitDefend()
    {
        if (animator != null)
            animator.SetBool(HashDefensa, false);
    }

    public bool IsDefending => animator != null && animator.GetBool(HashDefensa);

    public IEnumerator PlayUseItem()
    {
        animator.speed = velocidadUsoItem;
        animator.SetTrigger(HashUsoItem);
        // La duración real se divide por la velocidad: si el clip dura 1s y velocidad=1.5, acaba en 0.67s
        yield return new WaitForSeconds(duracionUsoItem / velocidadUsoItem);
        animator.speed = 1f;
    }

    public IEnumerator PlayTakeHit()
    {
        // Sonido de recibir dano: primero el propio del personaje, si no el global
        AudioClip clip = sfxTakeHit != null ? sfxTakeHit
                       : AudioManager.Instance != null ? AudioManager.Instance.sfxRecibirDano : null;
        AudioManager.Instance?.PlaySFX(clip);

        if (IsDefending)
        {
            yield return new WaitForSeconds(duracionTakeHit * 0.5f);
            yield break;
        }

        animator.SetTrigger(HashTakeHit);
        yield return new WaitForSeconds(duracionTakeHit);
    }

    public IEnumerator PlayDie()
    {
        AudioClip clip = sfxDie != null ? sfxDie
                       : AudioManager.Instance != null ? AudioManager.Instance.sfxMuerte : null;
        AudioManager.Instance?.PlaySFX(clip);

        if (animator != null)
        {
            // Habilitamos root motion solo para morir: las demás animaciones se
            // reproducen en su sitio sin que el personaje se desplace, pero la
            // de muerte necesita que el modelo se desplome al piso de verdad.
            animator.applyRootMotion = true;

            // La muerte tiene prioridad absoluta. Si el personaje estaba en
            // guardia, limpiamos el bool Defend antes de disparar el trigger
            // Die. Sin esto, el Animator queda atrapado en el estado Defend
            // (porque el bool sigue en true) y la transición a Die nunca
            // sucede → el modelo flota en pose de defensa en lugar de
            // desplomarse. Defender no protege de morir.
            animator.SetBool(HashDefensa, false);

            animator.SetTrigger(HashMorir);
        }

        yield return new WaitForSeconds(duracionMorir);
        // El personaje queda en el último frame de Die (estado final en el Animator)
    }


    // ──────────────────────────────────────────
    // EXPLORACION — solo Matarael
    // ──────────────────────────────────────────

    public void SetWalking(bool value)
    {
        animator.SetBool(HashCaminar, value);
    }
}
