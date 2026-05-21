using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Sistema centralizado de logs de combate.
public class CombatLogger : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI combatDialogue;

    [Header("Typewriter")]
    [Tooltip("Letras por segundo.")]
    public float letrasporSegundo = 50f;
    [Tooltip("Segundos que permanece el mensaje antes del siguiente.")]
    public float tiempoEntresMensajes = 1.2f;

    private Queue<string> cola = new Queue<string>();
    private bool escribiendo = false;
    private Coroutine tipeoActivo;

    // CombatManager puede esperar a esto antes de pasar a la siguiente fase.
    public bool IsIdle => !escribiendo && cola.Count == 0;

    // ──────────────────────────────────────────
    // API PÚBLICA
    // ──────────────────────────────────────────

    public void Log(string message)
    {
        Debug.Log(message);
        if (combatDialogue != null)
            cola.Enqueue(message);
    }

    // Aparece durante la SELECCIÓN: "¿Qué debería hacer Hana?"
    // Descarta cualquier mensaje pendiente: si el jugador avanza rápido entre
    // personajes, solo queremos ver la pregunta del personaje actual.
    public void LogSeleccion(string nombrePersonaje)
    {
        ClearQueue();
        Log($"Que deberia hacer {nombrePersonaje}?");
    }

    // Vacía la cola de mensajes e interrumpe el typewriter. Lo llama CombatManager
    // al pasar de selección a ejecución para que no se arrastren preguntas viejas.
    public void ClearQueue()
    {
        cola.Clear();
        if (tipeoActivo != null)
        {
            StopCoroutine(tipeoActivo);
            tipeoActivo = null;
        }
        escribiendo = false;
        if (combatDialogue != null)
            combatDialogue.text = "";
    }

    public void LogAction(string actorName, string actionName, string targetName,
                          bool hit, int value, CombatAction actionType, bool esHeal = false)
    {
        string result;

        if (actionType == CombatAction.Defense)
        {
            result = $"{actorName} se pone en guardia.";
        }
        else if (!hit)
        {
            result = actionType == CombatAction.Attack
                ? $"{actorName} ataca a {targetName}!\n{targetName} lo esquiva."
                : $"{actorName} usa {actionName}!\n{targetName} lo esquiva.";
        }
        else if (actionType == CombatAction.Attack)
        {
            result = value <= 0
                ? $"{actorName} ataca a {targetName}!\nNo tuvo efecto."
                : $"{actorName} ataca a {targetName}!\nCausa {value} de dano.";
        }
        else if (actionType == CombatAction.Item)
        {
            result = value <= 0
                ? $"{actorName} usa {actionName}.\nNo tuvo efecto."
                : $"{actorName} usa {actionName}.\n{targetName} recupera {value} HP.";
        }
        else // Magic
        {
            if (esHeal)
            {
                result = value <= 0
                    ? $"{actorName} usa {actionName}.\nNo tuvo efecto."
                    : $"{actorName} usa {actionName}.\n{targetName} recupera {value} HP.";
            }
            else
            {
                result = value <= 0
                    ? $"{actorName} usa {actionName} sobre {targetName}!\nNo tuvo efecto."
                    : $"{actorName} usa {actionName} sobre {targetName}!\nCausa {value} de dano.";
            }
        }

        Log(result);
    }

    // Separador de fase — solo consola.
    public void LogPhase(string phase)
    {
        Debug.Log($"\n── {phase.ToUpper()} ──");
    }

    // Estado de HP — solo consola.
    public void LogHPState(string name, int current, int max)
    {
        Debug.Log($"  [{name}] HP: {current}/{max}  ({Mathf.RoundToInt((float)current / max * 100)}%)");
    }

    // Resumen al final de la ronda — solo consola.
    public void LogRoundSummary(CharacterStats[] party, CharacterStats enemy)
    {
        string summary = "\n── ESTADO AL FINAL DE LA RONDA ──\n";
        foreach (CharacterStats m in party)
        {
            string estado = m.IsDefeated ? "DERROTADO" : $"{m.currentHealth}/{m.currentMaxHealth} HP";
            summary += $"  {m.characterData.nombre} (LV {m.currentLv}): {estado}\n";
        }
        string estadoEnemigo = enemy.IsDefeated ? "DERROTADO" : $"{enemy.currentHealth}/{enemy.currentMaxHealth} HP";
        summary += $"  {enemy.characterData.nombre} (LV {enemy.currentLv}): {estadoEnemigo}";
        Debug.Log(summary);
    }

    // ──────────────────────────────────────────
    // LÓGICA INTERNA
    // ──────────────────────────────────────────

    private void Update()
    {
        if (!escribiendo && cola.Count > 0)
            tipeoActivo = StartCoroutine(MostrarSiguiente());
    }

    private IEnumerator MostrarSiguiente()
    {
        escribiendo = true;
        string mensaje = cola.Dequeue();

        combatDialogue.text = "";
        float delay = 1f / letrasporSegundo;

        foreach (char letra in mensaje)
        {
            combatDialogue.text += letra;
            yield return new WaitForSeconds(delay);
        }

        yield return new WaitForSeconds(tiempoEntresMensajes);
        escribiendo = false;
        tipeoActivo = null;
    }
}