using UnityEngine;

// Sigue al jugador con interpolación suave y transiciona entre cámara de exploración y combate.
public class CameraFollow : MonoBehaviour
{
    [Header("Posición")]
    public float offsetX;
    public float offsetY = 6;
    public float offsetZ = 10;
    public float velocidadMov = 6;

    [Header("Rotación y FOV")]
    // Valores bajos producen transiciones lentas; aumentar para respuesta más rápida.
    public float velocidadRot = 1;
    public float combatFOV = 80f;
    public Vector3 exploreRotation = new Vector3(30f, -0.688f, 0f);
    public Vector3 combatRotation = new Vector3(17.4f, 21.7f, -0.02f);

    [Header("Referencias")]
    public Transform playerTransform;

    private Camera cam;
    private float newFOV, exploreFOV;
    private Vector3 targetRotation;

    private void Start()
    {
        cam = GetComponent<Camera>();
        exploreFOV = cam.fieldOfView;
        newFOV = exploreFOV;
        targetRotation = exploreRotation;
    }

    private void LateUpdate()
    {
        Vector3 newPos = new Vector3(
            playerTransform.position.x + offsetX,
            playerTransform.position.y + offsetY,
            playerTransform.position.z - offsetZ);

        transform.position = Vector3.Lerp(transform.position, newPos, velocidadMov * Time.deltaTime);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, newFOV, velocidadRot * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(targetRotation), velocidadRot * Time.deltaTime);
    }

    public void EventStart()
    {
        offsetX = 2f;
        offsetY = 5f;
        offsetZ = 15f;
        newFOV = combatFOV;
        targetRotation = combatRotation;
    }

    public void EventEnd()
    {
        offsetX = 0f;
        offsetY = 6f;
        offsetZ = 10f;
        newFOV = exploreFOV;
        targetRotation = exploreRotation;
    }
}