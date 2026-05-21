using UnityEngine;
using UnityEngine.SceneManagement;

// Singleton persistente que gestiona BGM y SFX de todo el juego.
//
// SETUP EN UNITY:
// 1. Crear un GameObject "AudioManager" en la escena Menu (o cualquier primera escena).
// 2. Anadir este script y dos AudioSources hijos:
//    - "BGM" con Loop=true, Play On Awake=false
//    - "SFX" con Loop=false, Play On Awake=false
// 3. Asignar ambos AudioSources en el Inspector.
// 4. Asignar todos los clips de BGM y SFX.
// 5. En Escena Menu asignar el nombre exacto de cada escena.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("BGM — Musica de fondo")]
    public AudioClip bgmMenu;
    public AudioClip bgmExploracion;
    public AudioClip bgmCombate;
    public AudioClip bgmFin;

    [Header("Nombres de escena (deben coincidir con Build Settings)")]
    public string nombreEscenaMenu        = "Menu";
    public string nombreEscenaExploracion = "DungeonExploration";
    public string nombreEscenaCombate     = "Combat_01";

    [Header("SFX — Efectos de exploracion")]
    public AudioClip sfxBoton;
    public AudioClip sfxRecogerItem;
    public AudioClip sfxCofre;
    public AudioClip sfxPasos;          // Pasos lentos — entrada de Rhaegal
    public AudioClip sfxPasosMatarael;  // Pasos del jugador
    public AudioClip sfxEvilLaugh;      // Risa de Rhaegal al llegar
    [Range(0f, 1f)]
    public float volumenEvilLaugh = 0.4f;

    [Header("SFX — Combate general")]
    public AudioClip sfxAtaqueBasico;
    public AudioClip sfxPocion;
    public AudioClip sfxRecibirDano;
    public AudioClip sfxMuerte;

    // ── Magias: sus clips se asignan en cada Magic SO directamente ──

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneCargada;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneCargada;
    }

    private void OnSceneCargada(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == nombreEscenaMenu)
            PlayBGM(bgmMenu);
        else if (scene.name == nombreEscenaExploracion)
            PlayBGM(bgmExploracion);
        else if (scene.name == nombreEscenaCombate)
            PlayBGM(bgmCombate);
    }

    // ──────────────────────────────────────────
    // API PUBLICA
    // ──────────────────────────────────────────

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return; // ya suena
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    public void PlayBGMFin()
    {
        PlayBGM(bgmFin);
    }

    // Reproduce un SFX sin interrumpir otros sonidos
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }

    // Atajos para los SFX mas usados
    public void PlayBoton()       => PlaySFX(sfxBoton);
    public void PlayRecogerItem() => PlaySFX(sfxRecogerItem);
    public void PlayCofre()       => PlaySFX(sfxCofre);
    public void PlayAtaqueBasico()=> PlaySFX(sfxAtaqueBasico);
    public void PlayPocion()      => PlaySFX(sfxPocion);
    public void PlayRecibirDano() => PlaySFX(sfxRecibirDano);
    public void PlayMuerte()      => PlaySFX(sfxMuerte);

    public void PlayEvilLaugh()
    {
        if (sfxEvilLaugh == null || sfxSource == null) return;
        sfxSource.PlayOneShot(sfxEvilLaugh, volumenEvilLaugh);
    }
}
