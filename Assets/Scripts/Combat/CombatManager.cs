using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatManager : MonoBehaviour
{
    [Header("Participantes")]
    public CharacterStats[] partyMembers;
    public CharacterStats enemy;

    [Header("Configuración")]
    public string escenaExploracion = "Exploracion";

    [Header("UI")]
    public GameObject actionMenu;

    [Header("Sistemas")]
    public CombatLogger logger;
    public CombatAnimator combatAnimator;

    public bool esperandoObjetivo = false;

    private CombatState currentState;
    private TurnData[] turnData;
    private int currentPartyIndex;

    private void Start()
    {
        currentPartyIndex = 0;
        currentState = CombatState.Selection;
        turnData = new TurnData[partyMembers.Length];
        StartCoroutine(InicializarCombate());
    }

    private IEnumerator InicializarCombate()
    {
        yield return null;

        if (PartyManager.PartyMana != null)
            PartyManager.PartyMana.LoadIntoStats(partyMembers);

        if (combatAnimator != null)
            combatAnimator.Init(partyMembers, enemy);

        logger.LogPhase("INICIO DE COMBATE");
        string log = "";
        foreach (CharacterStats m in partyMembers)
            log += $"  {m.characterData.nombre,-12} LV:{m.currentLv,2}  HP:{m.currentMaxHealth,3}  ATK:{m.currentPhysicAtk,3}  DEF:{m.currentDefense,3}  SPD:{m.currentSpeed,3}\n";
        log += $"  {enemy.characterData.nombre,-12} LV:{enemy.currentLv,2}  HP:{enemy.currentMaxHealth,3}  ATK:{enemy.currentPhysicAtk,3}  DEF:{enemy.currentDefense,3}  SPD:{enemy.currentSpeed,3}";
        Debug.Log(log);

        logger.LogSeleccion(partyMembers[currentPartyIndex].characterData.nombre);
    }

    private void NextSelection()
    {
        if (currentState != CombatState.Selection) return;

        currentPartyIndex++;

        while (currentPartyIndex < partyMembers.Length && partyMembers[currentPartyIndex].IsDefeated)
        {
            Debug.Log($"  [{partyMembers[currentPartyIndex].characterData.nombre}] está derrotado, se salta.");
            currentPartyIndex++;
        }

        if (currentPartyIndex >= partyMembers.Length)
        {
            currentState = CombatState.Execution;
            StartCoroutine(EjecutarTurnos());
        }
        else
        {
            logger.LogSeleccion(partyMembers[currentPartyIndex].characterData.nombre);
        }
    }

    public void SelectAction(CombatAction action)
    {
        if (currentState != CombatState.Selection) return;

        TurnData nuevoTurno = new TurnData();
        nuevoTurno.actionType = action;

        if (action == CombatAction.Attack)  nuevoTurno.target = enemy;
        if (action == CombatAction.Defense) nuevoTurno.target = GetCurrentCharacter();

        turnData[currentPartyIndex] = nuevoTurno;
        Debug.Log($"  [{GetCurrentCharacter().characterData.nombre}] seleccionó: {action}");
        NextSelection();
    }

    public void SelectAction(Magic magia)
    {
        if (currentState != CombatState.Selection) return;

        TurnData nuevoTurno = new TurnData();
        nuevoTurno.actionType = CombatAction.Magic;
        nuevoTurno.selectedMagic = magia;
        turnData[currentPartyIndex] = nuevoTurno;

        if (magia.magicType == MagicType.Damage)
        {
            turnData[currentPartyIndex].target = enemy;
            Debug.Log($"  [{GetCurrentCharacter().characterData.nombre}] seleccionó: {magia.nombre} (Dano)");
            NextSelection();
        }
        else if (magia.magicType == MagicType.Heal && magia.esArea)
        {
            turnData[currentPartyIndex].target = null;
            Debug.Log($"  [{GetCurrentCharacter().characterData.nombre}] seleccionó: {magia.nombre} (Cura área)");
            NextSelection();
        }
        else
        {
            Debug.Log($"  [{GetCurrentCharacter().characterData.nombre}] seleccionó: {magia.nombre} → esperando objetivo...");
            esperandoObjetivo = true;
        }
    }

    public void SelectAction(Item objeto)
    {
        if (currentState != CombatState.Selection) return;

        // El item se descuenta al seleccionarlo, no al ejecutar el turno.
        // Si el jugador cancela, CancelarSeleccion lo devuelve.
        Inventory.inventory.RemoveItem(objeto);

        TurnData nuevoTurno = new TurnData();
        nuevoTurno.actionType = CombatAction.Item;
        nuevoTurno.selectedItem = objeto;
        turnData[currentPartyIndex] = nuevoTurno;
        Debug.Log($"  [{GetCurrentCharacter().characterData.nombre}] selecciono: {objeto.nombre} -> esperando objetivo...");
        esperandoObjetivo = true;
    }

    public void ElegirObjetivoAliado(CharacterStats aliadoSeleccionado)
    {
        TurnData ticketActual = turnData[currentPartyIndex];
        ticketActual.target = aliadoSeleccionado;
        turnData[currentPartyIndex] = ticketActual;
        esperandoObjetivo = false;
        Debug.Log($"    → Objetivo: {aliadoSeleccionado.characterData.nombre}");
        NextSelection();
    }

    public void CancelarSeleccion()
    {
        if (!esperandoObjetivo) return;
        esperandoObjetivo = false;

        // Si habia un item seleccionado, devolverlo al inventario
        TurnData turnoActual = turnData[currentPartyIndex];
        if (turnoActual.actionType == CombatAction.Item && turnoActual.selectedItem != null)
            Inventory.inventory.AddItem(turnoActual.selectedItem);

        turnData[currentPartyIndex] = new TurnData();
        Debug.Log($"  [{GetCurrentCharacter().characterData.nombre}] cancelo.");
        logger.LogSeleccion(partyMembers[currentPartyIndex].characterData.nombre);
    }

    private IEnumerator EjecutarTurnos()
    {
        actionMenu.SetActive(false);
        logger.ClearQueue();

        // Los que defendieron la ronda pasada salen de la pose de guardia antes
        // de ejecutar sus nuevas acciones. Si vuelven a defender, PlayDefend
        // re-activa el bool.
        if (combatAnimator != null)
            combatAnimator.ClearAllDefending();

        List<CharacterStats> combatQueue = new List<CharacterStats>(partyMembers) { enemy };
        combatQueue = combatQueue.OrderByDescending(c =>
        {
            if (c == enemy) return c.currentSpeed;
            int idx = System.Array.IndexOf(partyMembers, c);
            if (idx >= 0 && turnData[idx].actionType == CombatAction.Defense)
                return int.MaxValue;
            return c.currentSpeed;
        }).ToList();

        logger.LogPhase("EJECUCIÓN DE TURNO");

        foreach (CharacterStats personajeActual in combatQueue)
        {
            if (currentState == CombatState.Victory || currentState == CombatState.Defeat)
                yield break;

            if (personajeActual == enemy)
            {
                // Bosses pueden ejecutar varias acciones por ronda (ataquesPorTurno
                // en CharacterData). El bucle se rompe si el enemigo muere a mitad
                // del combo o si el party entero cae.
                int ataques = Mathf.Max(1, enemy.characterData != null
                                              ? enemy.characterData.ataquesPorTurno
                                              : 1);

                for (int i = 0; i < ataques; i++)
                {
                    if (currentState == CombatState.Victory || currentState == CombatState.Defeat)
                        yield break;
                    if (enemy.IsDefeated) break;
                    if (partyMembers.All(m => m.IsDefeated)) break;

                    TurnData turnoEnemigo = CalcularIAEnemigo();
                    System.Action aplicarConsecuenciasEnemigo;
                    turnoEnemigo.hit = ProcessAction(enemy, turnoEnemigo.target, turnoEnemigo, out aplicarConsecuenciasEnemigo);

                    if (combatAnimator != null)
                        yield return StartCoroutine(combatAnimator.PlayAction(enemy, turnoEnemigo.target, turnoEnemigo, aplicarConsecuenciasEnemigo));
                    else
                    {
                        aplicarConsecuenciasEnemigo?.Invoke();
                        yield return new WaitForSeconds(2f);
                    }

                    // Pequena pausa entre acciones del combo para que se sienta
                    // como ataques separados y no como una secuencia continua.
                    if (i < ataques - 1)
                        yield return new WaitForSeconds(0.4f);

                    VerificarEstadoCombate();
                }
            }
            else
            {
                if (personajeActual.IsDefeated)
                {
                    Debug.Log($"  [{personajeActual.characterData.nombre}] derrotado, pierde turno.");
                    continue;
                }

                int idx = System.Array.IndexOf(partyMembers, personajeActual);
                TurnData turno = turnData[idx];

                System.Action aplicarConsecuencias;
                turno.hit = ProcessAction(personajeActual, turno.target, turno, out aplicarConsecuencias);
                turnData[idx] = turno;

                if (combatAnimator != null)
                    yield return StartCoroutine(combatAnimator.PlayAction(personajeActual, turno.target, turno, aplicarConsecuencias));
                else
                {
                    aplicarConsecuencias?.Invoke();
                    yield return new WaitForSeconds(2f);
                }
            }

            VerificarEstadoCombate();
        }

        Debug.Log($"  Estado: {currentState}");

        if (currentState != CombatState.Victory && currentState != CombatState.Defeat)
        {
            logger.LogRoundSummary(partyMembers, enemy);
            yield return new WaitUntil(() => logger.IsIdle);
            FinalizarRonda();
        }
    }

    // Devuelve true si la acción acertó (precisión exitosa). CombatAnimator
    // usa este flag para no spawnear proyectil/efecto ni triggerear TakeHit
    // cuando el ataque falló.
    //
    // Importante: el aplicar dano/cura NO se hace acá. ProcessAction calcula
    // y loguea, pero deja la aplicación capturada en `aplicarConsecuencias`,
    // un callback que CombatAnimator invoca AL MOMENTO DEL IMPACTO. Esto hace
    // que las barras de vida bajen/suban en sincronía con el golpe visual,
    // no al inicio del turno.
    public bool ProcessAction(CharacterStats emitter, CharacterStats target, TurnData turno,
                              out System.Action aplicarConsecuencias)
    {
        bool esHeal = turno.actionType == CombatAction.Magic &&
                      turno.selectedMagic != null &&
                      turno.selectedMagic.magicType == MagicType.Heal;

        bool success = CombatCalculator.Precision(
            emitter.currentAccuracy,
            target != null ? target.currentEvasion : 0,
            turno);

        int actionValue = success ? CombatCalculator.ActionValue(emitter, target, turno) : 0;

        string emitterName = emitter.characterData.nombre;
        string targetName  = target != null ? target.characterData.nombre : "—";
        string actionName  = turno.actionType == CombatAction.Magic
            ? turno.selectedMagic.nombre
            : turno.actionType == CombatAction.Item
                ? turno.selectedItem.nombre
                : turno.actionType.ToString();

        // El log de la acción SÍ se hace ya (es info para el jugador, va al
        // tope del turno). Los LogHPState que reflejan el cambio de barras
        // van DENTRO del callback, junto con el TakeDamage/ReceiveHeal.
        logger.LogAction(emitterName, actionName, targetName, success, actionValue, turno.actionType, esHeal);

        if (!success)
        {
            aplicarConsecuencias = null;
            return false;
        }

        // Capturamos toda la lógica de aplicación en una closure. CombatAnimator
        // la dispara cuando el proyectil impacta o el efecto aparece sobre el
        // target, según el tipo de acción.
        aplicarConsecuencias = () =>
        {
            switch (turno.actionType)
            {
                case CombatAction.Attack:
                    target.TakeDamage(actionValue);
                    logger.LogHPState(targetName, target.currentHealth, target.currentMaxHealth);
                    break;

                case CombatAction.Magic:
                    if (turno.selectedMagic.magicType == MagicType.Damage)
                    {
                        if (turno.selectedMagic.esArea)
                        {
                            // AOE Damage: golpea a todo el bando opuesto al emisor.
                            bool emisorEsEnemigo = emitter == enemy;
                            if (emisorEsEnemigo)
                            {
                                foreach (CharacterStats miembro in partyMembers)
                                {
                                    if (miembro == null || miembro.IsDefeated) continue;
                                    int dmg = CombatCalculator.ActionValue(emitter, miembro, turno);
                                    miembro.TakeDamage(dmg);
                                    logger.LogHPState(miembro.characterData.nombre, miembro.currentHealth, miembro.currentMaxHealth);
                                }
                            }
                            else
                            {
                                if (enemy != null && !enemy.IsDefeated)
                                {
                                    int dmg = CombatCalculator.ActionValue(emitter, enemy, turno);
                                    enemy.TakeDamage(dmg);
                                    logger.LogHPState(enemy.characterData.nombre, enemy.currentHealth, enemy.currentMaxHealth);
                                }
                            }
                        }
                        else
                        {
                            target.TakeDamage(actionValue);
                            logger.LogHPState(targetName, target.currentHealth, target.currentMaxHealth);
                        }
                    }
                    else if (turno.selectedMagic.magicType == MagicType.Heal)
                    {
                        if (turno.selectedMagic.esArea)
                        {
                            foreach (CharacterStats miembro in partyMembers)
                            {
                                if (!miembro.IsDefeated)
                                {
                                    miembro.ReceiveHeal(actionValue);
                                    logger.LogHPState(miembro.characterData.nombre, miembro.currentHealth, miembro.currentMaxHealth);
                                }
                            }
                        }
                        else
                        {
                            if (target.IsDefeated)
                                logger.Log($"{targetName} está fuera de combate y no puede ser curado.");
                            else
                            {
                                target.ReceiveHeal(actionValue);
                                logger.LogHPState(targetName, target.currentHealth, target.currentMaxHealth);
                            }
                        }
                    }
                    break;

                case CombatAction.Defense:
                    Debug.Log($"  [{emitterName}] en guardia.");
                    break;

                case CombatAction.Item:
                    if (turno.selectedItem != null && turno.selectedItem.ItemData != null)
                    {
                        if (target != null && target.IsDefeated)
                        {
                            // El objetivo cayó antes de que se ejecutara la acción — devolver al inventario
                            Inventory.inventory.AddItem(turno.selectedItem);
                            logger.Log($"{targetName} ya no puede recibir la pocion.");
                        }
                        else
                        {
                            int cura = turno.selectedItem.ItemData.healAmount;
                            if (cura > 0 && target != null)
                            {
                                target.ReceiveHeal(cura);
                                logger.LogHPState(targetName, target.currentHealth, target.currentMaxHealth);
                            }
                        }
                    }
                    break;
            }
        };

        return true;
    }

    private TurnData CalcularIAEnemigo()
    {
        List<CharacterStats> vivos = partyMembers.Where(m => !m.IsDefeated).ToList();
        if (vivos.Count == 0) return new TurnData { actionType = CombatAction.Defense };

        // Objetivo aleatorio entre los aliados vivos (idéntico para las 3 acciones).
        CharacterStats objetivo = vivos[Random.Range(0, vivos.Count)];

        // Rhaegal alterna con igual probabilidad entre ataque físico normal
        // y sus dos magias (índices 0 y 1 de su lista de magics).
        int decision = Random.Range(0, 3);

        if (decision == 0)
        {
            return new TurnData { actionType = CombatAction.Attack, target = objetivo };
        }

        Magic magia = enemy.magics[decision - 1]; // 1 → magics[0], 2 → magics[1]
        return new TurnData
        {
            actionType    = CombatAction.Magic,
            selectedMagic = magia,
            target        = objetivo
        };
    }

    private void VerificarEstadoCombate()
    {
        if (enemy.IsDefeated)
        {
            currentState = CombatState.Victory;
            Debug.Log("  ¡VICTORIA!");
            StartCoroutine(FinalizarCombate(true));
            return;
        }
        if (partyMembers.All(m => m.IsDefeated))
        {
            currentState = CombatState.Defeat;
            Debug.Log("  DERROTA.");
            StartCoroutine(FinalizarCombate(false));
        }
    }

    private IEnumerator FinalizarCombate(bool victoria)
    {
        logger.LogPhase(victoria ? "VICTORIA" : "DERROTA");
        yield return new WaitForSeconds(2f);

        if (victoria && PartyManager.PartyMana != null)
            PartyManager.PartyMana.SaveFromStats(partyMembers);

        if (EndScreen.Instance != null)
            yield return StartCoroutine(EndScreen.Instance.Mostrar(victoria));
        else
            SceneManager.LoadScene(escenaExploracion); // fallback si no hay EndScreen
    }

    private void FinalizarRonda()
    {
        currentPartyIndex = 0;
        while (currentPartyIndex < partyMembers.Length && partyMembers[currentPartyIndex].IsDefeated)
            currentPartyIndex++;

        currentState = CombatState.Selection;
        turnData = new TurnData[partyMembers.Length];
        actionMenu.SetActive(true);
        logger.LogPhase("NUEVA RONDA");
        logger.LogSeleccion(partyMembers[currentPartyIndex].characterData.nombre);
    }

    public CharacterStats GetCurrentCharacter() => partyMembers[currentPartyIndex];

    public bool EstaEnSeleccion => currentState == CombatState.Selection;
}

[System.Serializable]
public struct TurnData
{
    public CombatAction actionType;
    public Magic selectedMagic;
    public Item selectedItem;
    public CharacterStats target;
    // Resultado del check de precisión. CombatManager lo setea tras ProcessAction
    // y CombatAnimator lo respeta para no spawnear proyectil/efecto ni triggerear
    // TakeHit cuando el ataque falló.
    public bool hit;
}

public enum CombatState { Selection, Execution, Victory, Defeat }
public enum CombatAction { Attack, Magic, Item, Defense }