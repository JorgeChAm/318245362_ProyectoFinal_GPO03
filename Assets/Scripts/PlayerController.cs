using System.Collections;
using UnityEngine;

// Controla el movimiento del jugador y la interaccion con objetos recogibles.
public class PlayerController : MonoBehaviour
{
    public float speed;
    public float rotationSpeed;

    [Header("Animacion")]
    public CharacterAnimator characterAnimator;

    [Header("Audio — Pasos")]
    [Tooltip("Intervalo en segundos entre cada sonido de paso.")]
    public float intervaloPasos = 0.45f;

    private float moveX, moveZ;
    private Quaternion targetRotation;
    private Rigidbody _playerRB;
    private CollectableItem nearbyItem;
    private bool caminando = false;
    private Coroutine coroutinePasos;

    private void Start()
    {
        _playerRB = GetComponent<Rigidbody>();
        if (characterAnimator == null)
            characterAnimator = GetComponentInChildren<CharacterAnimator>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && nearbyItem != null)
            RecogerItem(nearbyItem);
    }

    private void FixedUpdate()
    {
        moveX = Input.GetAxis("Horizontal") * speed;
        moveZ = Input.GetAxis("Vertical") * speed;

        _playerRB.linearVelocity = new Vector3(moveX, _playerRB.linearVelocity.y, moveZ);

        bool isMoving = Mathf.Abs(moveX) > 0.01f || Mathf.Abs(moveZ) > 0.01f;

        if (characterAnimator != null)
            characterAnimator.SetWalking(isMoving);

        // Arrancar o detener sonido de pasos
        if (isMoving && !caminando)
        {
            caminando = true;
            coroutinePasos = StartCoroutine(LoopPasos());
        }
        else if (!isMoving && caminando)
        {
            caminando = false;
            if (coroutinePasos != null) StopCoroutine(coroutinePasos);
        }

        if (isMoving)
        {
            targetRotation = Quaternion.LookRotation(new Vector3(moveX, 0, moveZ));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private IEnumerator LoopPasos()
    {
        while (caminando)
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance?.sfxPasosMatarael);
            yield return new WaitForSeconds(intervaloPasos);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Object")) return;
        CollectableItem ci = other.GetComponent<CollectableItem>();
        if (ci != null) nearbyItem = ci;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Object")) return;
        CollectableItem ci = other.GetComponent<CollectableItem>();
        if (ci != null && ci == nearbyItem) nearbyItem = null;
    }

    private void RecogerItem(CollectableItem collectable)
    {
        Item item = new Item(collectable.itemData, 1);
        Inventory.inventory.AddItem(item);

        AudioManager.Instance?.PlayRecogerItem();

        if (PickupNotifier.Instance != null)
            PickupNotifier.Instance.Show(collectable.itemData.nombre);

        nearbyItem = null;
        Destroy(collectable.gameObject);
    }
}
