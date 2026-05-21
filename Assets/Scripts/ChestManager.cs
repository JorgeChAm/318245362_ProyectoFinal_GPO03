using UnityEngine;

// Gestiona la apertura del cofre y la expulsión de su contenido.
public class ChestManager : MonoBehaviour
{
    public GameObject chestTop;
    public Transform itemSpawnPoint;
    public GameObject[] chestItem;
    public float speed = 120f;
    public float fuerza = 10f;

    private bool chestOpen, itemsDropped, sonidoReproducido;
    private float rotacion;
    private const float LimiteRotacion = 60f;

    private void Update()
    {
        if (chestOpen && rotacion <= LimiteRotacion)
        {
            OpenChest();
        }
        else if (rotacion >= LimiteRotacion && !itemsDropped)
        {
            itemsDropped = true;
            ItemDrop();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E))
        {
            chestOpen = true;
            if (!sonidoReproducido)
            {
                sonidoReproducido = true;
                AudioManager.Instance?.PlayCofre();
            }
        }
    }

    private void OpenChest()
    {
        float rotacionFrame = speed * Time.deltaTime;
        rotacion += rotacionFrame;
        chestTop.transform.Rotate(Vector3.right, -rotacionFrame);
    }

    private void ItemDrop()
    {
        foreach (GameObject item in chestItem)
        {
            GameObject itemInstanciado = Instantiate(item, itemSpawnPoint.position, itemSpawnPoint.rotation);
            if (itemInstanciado.TryGetComponent(out Rigidbody itemRb))
            {
                // Y forzado a positivo para que los items salten hacia arriba.
                Vector3 direccion = new Vector3(Random.Range(-1f, 1f), 1f, Random.Range(-1f, 1f));
                itemRb.AddForce(direccion.normalized * fuerza, ForceMode.Impulse);
            }
        }
    }
}