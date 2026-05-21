using UnityEngine;
using UnityEngine.UI;

// Controla la barra de experiencia animada en el HUD.
public class XpBarSystem : MonoBehaviour
{
    public Image xpBar;
    public float barSpeed = 2;
    private float nextXp;

    private void Awake()
    {
        xpBar.fillAmount = 0f;
        nextXp = 0f;
    }

    private void Update()
    {
        if (xpBar.fillAmount != nextXp)
        {
            xpBar.fillAmount = Mathf.MoveTowards(
                xpBar.fillAmount,
                nextXp,
                Time.deltaTime * barSpeed);
        }
    }

    // Recibe un porcentaje entre 0 y 1 y actualiza el objetivo de la barra.
    public void SetXp(float xpPercentage)
    {
        nextXp = xpPercentage;
    }

    // Reinicia la barra visualmente al subir de nivel. Llamado desde CharacterStats.
    public void LevelUp(int level)
    {
        xpBar.fillAmount = 0f;
        nextXp = 0f;
    }
}