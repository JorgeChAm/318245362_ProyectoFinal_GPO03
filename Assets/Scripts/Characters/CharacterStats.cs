using TMPro;
using UnityEngine;

// Gestiona las estadísticas dinámicas de un personaje durante el juego.
public class CharacterStats : MonoBehaviour
{
    [Header("UI")]
    public HealthBarSystem healthBar;
    public XpBarSystem xpBar;
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI healthCount;

    [Header("Datos")]
    public CharacterData characterData;
    public Magic[] magics;

    [Header("Experiencia")]
    public int maxXp;
    public int currentXp;

    [Header("Nivel y Salud")]
    public int initialLv = 1;
    public int currentLv = 1;
    public int currentHealth;

    [Header("Estadísticas Actuales")]
    public int currentMaxHealth;
    public int currentPhysicAtk;
    public int currentMagicPower;
    public int currentDefense;
    public int currentSpeed;
    public int currentEvasion;
    public int currentAccuracy;
    
    // Multiplicador aplicado a HP/ATK/DEF/MagicPower/Speed para que las cifras
    // se sientan más impactantes en pantalla (HP en cientos, dano en decenas/cientos).
    // Precisión y evasión NO se multiplican porque las fórmulas de hit% usan
    // diferencias absolutas con un offset fijo de 50 que no escala bien.
    public const int StatScale = 10;

    public bool IsDefeated => currentHealth <= 0;
        
    void Start()
    {
        maxXp = 100;
        currentXp = 0;
        currentLv = initialLv; 
        StatsUpdater();
        if (currentHealth <= 0) currentHealth = currentMaxHealth;
        magics = characterData.magias;
        characterName.text = characterData.nombre;
        HealthTextUpdate();
    }

    // Recalcula stats derivadas tras un cambio de nivel.
    public void RecalcularStats()
    {
        StatsUpdater();
        magics = characterData.magias;
        characterName.text = characterData.nombre;
        HealthTextUpdate();
    }

    // Refresca texto y barra de vida sin recalcular stats.
    public void RefreshHealthUI()
    {
        HealthTextUpdate();
        if (healthBar != null && currentMaxHealth > 0)
            healthBar.SetHealth((float)currentHealth / currentMaxHealth);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, currentMaxHealth);
        float healthPercent = (float)currentHealth / currentMaxHealth;
        healthBar.SetHealth(healthPercent);
        HealthTextUpdate();
    }

    public void ReceiveHeal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, currentMaxHealth);
        float healthPercent = (float)currentHealth / currentMaxHealth;
        healthBar.SetHealth(healthPercent);
        HealthTextUpdate();
    }

    private void XpGain(int xp)
    {
        currentXp += xp;
        float xpPercent = (float)currentXp / maxXp;

        if (xpBar != null) xpBar.SetXp(xpPercent);

        if (xpPercent >= 1f)
        {
            currentXp = 0;
            currentLv++;
            if (xpBar != null) xpBar.LevelUp(currentLv);
            StatsUpdater();
            HealthTextUpdate();
        }
    }

    // Fórmula de escalado de estadísticas por nivel, inspirada en la fórmula base de Pokémon.
    public static int StatLvUpdate(int baseStat, int nivel)
    {
        return Mathf.FloorToInt((2 * baseStat * nivel) / 100) + nivel + 10;
    }

    private void StatsUpdater()
    {
        // Preserva la proporción de HP actual al recalcular el máximo tras subir de nivel.
        // Se protege la división por cero en la primera llamada cuando currentMaxHealth es 0.
        float healthPercent = currentMaxHealth > 0 ? (float)currentHealth / currentMaxHealth : 1f;

        currentMaxHealth    = StatLvUpdate(characterData.vidaBase,          currentLv) * StatScale;
        currentPhysicAtk    = StatLvUpdate(characterData.ataqueFisicoBase,  currentLv) * StatScale;
        currentMagicPower   = StatLvUpdate(characterData.poderMagico,       currentLv) * StatScale;
        currentDefense      = StatLvUpdate(characterData.defensaBase,       currentLv) * StatScale;
        currentSpeed        = StatLvUpdate(characterData.velocidadBase,     currentLv) * StatScale;
        currentEvasion      = StatLvUpdate(characterData.evasionBase,       currentLv);
        currentAccuracy     = StatLvUpdate(characterData.precisionBase,     currentLv);

        currentHealth = Mathf.FloorToInt(currentMaxHealth * healthPercent);
    }

    private void HealthTextUpdate()
    {
        healthCount.text = currentHealth + "/" + currentMaxHealth + "HP";
    }
}