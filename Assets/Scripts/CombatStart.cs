using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// Gestiona la secuencia de inicio de combate al contacto con una zona de trigger.
public class CombatStart : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject player;
    public GameObject enemy;
    public Transform enemyTarget;
    public CameraFollow cameraFollow;
    public Animator enemyAnimator;

    [Header("Configuración")]
    public float speed = 1.2f;
    public float delayAntesCambioEscena = 6f;
    public float lookAtDuration = 1.0f;

    [Header("Entrada Dramática")]
    [Tooltip("Velocidad de reproducción del walk del enemigo (0.5 = la mitad de lento)")]
    public float walkAnimSpeed = 0.5f;
    [Tooltip("Pausa en segundos al llegar a destino antes de cambiar de escena")]
    public float pausaDramatica = 1.8f;
    [Tooltip("Trigger del animator para activar la pausa dramática (ej. 'isArrived')")]
    public string arrivalTrigger = "";

    private PlayerController playerController;
    private bool enemyMove;
    private bool enemyArrived;

    private void Start()
    {
        playerController = player.GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (!enemyMove) return;

        float distancia = Vector3.Distance(enemy.transform.position, enemyTarget.position);

        if (distancia > 0.05f)
        {
            // Velocidad con ease-in suave: empieza lento y llega a la velocidad configurada
            float t = 1f - Mathf.Clamp01(distancia / 8f);
            float velocidadActual = Mathf.Lerp(speed * 0.3f, speed, t);

            enemy.transform.position = Vector3.MoveTowards(
                enemy.transform.position,
                enemyTarget.position,
                velocidadActual * Time.deltaTime);
        }
        else if (!enemyArrived)
        {
            // Llegó al destino — snap final y pausa dramática
            enemy.transform.position = enemyTarget.position;
            enemyMove = false;
            enemyArrived = true;

            if (enemyAnimator != null)
            {
                enemyAnimator.speed = 1f; // Restaurar velocidad normal del animator
                enemyAnimator.SetBool("isWalking", false);

                // Activar trigger de llegada si está configurado (ej. pose amenazante)
                if (!string.IsNullOrEmpty(arrivalTrigger))
                    enemyAnimator.SetTrigger(arrivalTrigger);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            StartCoroutine(IniciarCombate());
    }

    private IEnumerator IniciarCombate()
    {
        playerController.enabled = false;

        // Forzar idle en el player al detener el control
        if (playerController.characterAnimator != null)
            playerController.characterAnimator.SetWalking(false);

        cameraFollow.EventStart();
        enemyMove = true;
        enemyArrived = false;

        // Enemigo camina lento y dramático hacia su posición de entrada
        if (enemyAnimator != null)
        {
            enemyAnimator.speed = walkAnimSpeed; // Animación en cámara lenta
            enemyAnimator.SetBool("isWalking", true);
        }

        // Pasos del enemigo durante la entrada
        AudioManager.Instance?.PlaySFX(AudioManager.Instance?.sfxPasos);

        // El jugador clava la mirada en el enemigo mientras este se acerca
        StartCoroutine(SmoothLookAt(player.transform, enemy.transform, lookAtDuration));

        // Esperar a que el enemigo llegue a su posicion
        yield return new WaitUntil(() => enemyArrived);

        // Risa del enemigo al llegar — momento dramatico
        AudioManager.Instance?.PlayEvilLaugh();

        // Pausa dramatica una vez que llega (tiempo para lucirse)
        yield return new WaitForSeconds(pausaDramatica);

        // Detener SFX antes de cambiar escena para que no se arrastren
        if (AudioManager.Instance != null) AudioManager.Instance.sfxSource.Stop();
        SceneManager.LoadScene("Combat_01");
    }

    // Rota suavemente un Transform para mirar a un objetivo solo en el eje Y.
    private IEnumerator SmoothLookAt(Transform looker, Transform target, float duration)
    {
        float elapsed = 0f;
        Quaternion startRot = looker.rotation;

        while (elapsed < duration)
        {
            Vector3 dir = target.position - looker.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                looker.rotation = Quaternion.Slerp(startRot, targetRot, elapsed / duration);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap final para asegurar que queda mirando exactamente al objetivo
        Vector3 finalDir = target.position - looker.position;
        finalDir.y = 0f;
        if (finalDir.sqrMagnitude > 0.001f)
            looker.rotation = Quaternion.LookRotation(finalDir);
    }
}