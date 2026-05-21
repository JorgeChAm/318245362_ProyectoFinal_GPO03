using UnityEngine;
using UnityEngine.UI;

// Controla la barra de vida animada en el HUD.
public class HealthBarSystem : MonoBehaviour
{
    public Image healthBar;
    public float barSpeed = 2;
    private float nextHealth;

    private void Awake()
    {
        healthBar.fillAmount = 1f;
        nextHealth = 1f;
    }

    // Recibe un porcentaje entre 0 y 1 y actualiza el objetivo de la barra.
    public void SetHealth(float healthPercentage)
    {
        nextHealth = healthPercentage;
    }

    private void Update()
    {
        if (healthBar.fillAmount != nextHealth)
        {
            healthBar.fillAmount = Mathf.MoveTowards(
                healthBar.fillAmount,
                nextHealth,
                barSpeed * Time.deltaTime);
        }
    }
}