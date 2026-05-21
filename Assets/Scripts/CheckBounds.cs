using UnityEngine;

// Restringe la posición del jugador dentro de los límites del suelo usando su BoxCollider.
public class CheckBounds : MonoBehaviour
{
    public GameObject floor;
    public float offset = 5;

    private BoxCollider _floorCollider;
    private float xMax, xMin, zMax, zMin;

    private void Start()
    {
        _floorCollider = floor.GetComponent<BoxCollider>();
        xMax = _floorCollider.bounds.max.x - offset;
        xMin = _floorCollider.bounds.min.x + offset;
        zMax = _floorCollider.bounds.max.z - offset;
        zMin = _floorCollider.bounds.min.z + offset;
    }

    private void Update()
    {
        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, xMin, xMax),
            transform.position.y,
            Mathf.Clamp(transform.position.z, zMin, zMax));
    }
}