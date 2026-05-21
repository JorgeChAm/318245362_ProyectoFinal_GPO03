using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Coordinador central de animaciones de combate.
// CombatManager le cede el control durante un turno y espera a que termine.
//
// SETUP EN UNITY:
// 1. Crear un GameObject vacío en la escena de combate llamado "CombatAnimator".
// 2. Anadir este script.
// 3. Asignar los CharacterAnimator de cada aliado en el array partyAnimators
//    (mismo orden que partyMembers en CombatManager).
// 4. Asignar el CharacterAnimator del enemigo en enemyAnimator.
// 5. Asignar los prefabs de proyectil por personaje (o dejar uno genérico).
public class CombatAnimator : MonoBehaviour
{
    [Header("Animadores — mismo orden que partyMembers en CombatManager")]
    public CharacterAnimator[] partyAnimators;
    public CharacterAnimator enemyAnimator;

    [Header("Proyectiles — uno por personaje (o un genérico si todos usan el mismo)")]
    [Tooltip("Índice igual que partyAnimators. El enemigo usa el último slot.")]
    public GameObject[] proyectilesPorPersonaje;
    public GameObject proyectilEnemigo;

    // Referencias internas para buscar animadores por CharacterStats
    private CharacterStats[] partyMembers;
    private CharacterStats enemy;

    // CombatManager llama esto en InicializarCombate para dar contexto
    public void Init(CharacterStats[] party, CharacterStats enemy)
    {
        this.partyMembers = party;
        this.enemy = enemy;
    }

    // CombatManager lo llama al inicio de cada ronda de ejecución para sacar
    // a los que estaban defendiendo de la pose de guardia.
    public void ClearAllDefending()
    {
        if (partyAnimators != null)
            foreach (var anim in partyAnimators)
                if (anim != null) anim.ExitDefend();
        if (enemyAnimator != null)
            enemyAnimator.ExitDefend();
    }

    // ──────────────────────────────────────────
    // PUNTO DE ENTRADA: CombatManager hace yield aquí
    // ──────────────────────────────────────────

    public IEnumerator PlayAction(CharacterStats actor, CharacterStats target, TurnData turno,
                                   System.Action aplicarConsecuencias = null)
    {
        CharacterAnimator actorAnim  = GetAnimator(actor);
        CharacterAnimator targetAnim = GetAnimator(target);

        switch (turno.actionType)
        {
            case CombatAction.Attack:
                yield return StartCoroutine(PlayAtaque(actor, actorAnim, target, targetAnim, turno, aplicarConsecuencias));
                break;

            case CombatAction.Magic:
                yield return StartCoroutine(PlayMagia(actor, actorAnim, target, targetAnim, turno, aplicarConsecuencias));
                break;

            case CombatAction.Defense:
                // Defense no tiene golpe que sincronizar — la pose de guardia
                // YA es el efecto. Aplicamos al inicio (en la práctica solo loguea).
                aplicarConsecuencias?.Invoke();
                yield return StartCoroutine(PlayDefensa(actorAnim));
                break;

            case CombatAction.Item:
                // El item se aplica al final del clip de UseItem (cuando el actor
                // "consume" el objeto). El hook de aplicarConsecuencias queda listo
                // para el momento en que el personaje completa el gesto de uso.
                yield return StartCoroutine(PlayItem(actorAnim, targetAnim, target));
                aplicarConsecuencias?.Invoke();
                break;
        }
    }

    // ──────────────────────────────────────────
    // SECUENCIAS DE ANIMACIÓN POR ACCIÓN
    // ──────────────────────────────────────────

    // Mismo flujo que PlayMagia: el actor anima hasta el frame de impacto,
    // ahí se instancia el proyectil del personaje, el actor termina su clip
    // mientras el proyectil viaja, y al impactar el target reacciona.
    private IEnumerator PlayAtaque(CharacterStats actor, CharacterAnimator actorAnim,
                                    CharacterStats target, CharacterAnimator targetAnim,
                                    TurnData turno, System.Action aplicarConsecuencias)
    {
        if (actorAnim == null)
        {
            // Sin animador: espera fija para no romper el flujo
            aplicarConsecuencias?.Invoke();
            yield return new WaitForSeconds(1.0f);
            yield break;
        }

        // 1. Animar hasta el momento de lanzamiento
        yield return StartCoroutine(actorAnim.PlayAttackUntilLaunch());
        AudioManager.Instance?.PlayAtaqueBasico();

        // Si el ataque falló: spawneamos el proyectil igual, pero lo mandamos
        // a un dummy MÁS ALLÁ del target para que pase de largo y se desvanezca
        // en el aire. El target NO reacciona. Visualmente parece que el golpe
        // se les pasó por un costado. Y como falló, aplicarConsecuencias es null
        // (ProcessAction lo dejó así), por eso no hace falta condicionar nada.
        if (!turno.hit)
        {
            if (targetAnim != null)
                SpawnProyectilFallido(actorAnim, targetAnim, ObtenerProyectil(actor, turno));
            yield return StartCoroutine(actorAnim.PlayAttackRemainder());
            yield break;
        }

        // 2. Instanciar y lanzar el proyectil
        if (targetAnim != null)
        {
            bool impactó = false;
            GameObject prefab = ObtenerProyectil(actor, turno);

            if (prefab != null && actorAnim.puntoLanzamiento != null)
            {
                GameObject go = Instantiate(prefab,
                    actorAnim.puntoLanzamiento.position,
                    actorAnim.puntoLanzamiento.rotation);

                Projectile proyectil = go.GetComponent<Projectile>();
                if (proyectil != null)
                    proyectil.Inicializar(GetPuntoImpacto(targetAnim), () => impactó = true);
                else
                    impactó = true; // prefab sin script Projectile → continuar sin esperar
            }
            else
            {
                // Sin prefab configurado: simular delay corto y seguir
                yield return new WaitForSeconds(0.3f);
                impactó = true;
            }

            // 3. Animar el resto del clip del actor mientras el proyectil vuela
            StartCoroutine(actorAnim.PlayAttackRemainder());

            // 4. Esperar a que el proyectil llegue
            yield return new WaitUntil(() => impactó);

            // 5. AL IMPACTO: aplicamos dano/cura. Esto hace que la barra de vida
            //    baje en sincronía con el golpe visual, no al inicio del turno.
            aplicarConsecuencias?.Invoke();

            // 6. El objetivo reacciona
            if (target.IsDefeated)
                yield return StartCoroutine(targetAnim.PlayDie());
            else
                yield return StartCoroutine(targetAnim.PlayTakeHit());
        }
        else
        {
            // No hay target válido: aplicamos al final del clip del actor
            // para no perder el efecto.
            yield return StartCoroutine(actorAnim.PlayAttackRemainder());
            aplicarConsecuencias?.Invoke();
        }
    }

    private IEnumerator PlayMagia(CharacterStats actor, CharacterAnimator actorAnim,
                                   CharacterStats target, CharacterAnimator targetAnim,
                                   TurnData turno, System.Action aplicarConsecuencias)
    {
        if (actorAnim == null)
        {
            // Sin animador: espera fija para no romper el flujo
            aplicarConsecuencias?.Invoke();
            yield return new WaitForSeconds(1.5f);
            yield break;
        }

        // Inyectar el clip de animación de la magia activa en el override del actor.
        // Así dos personajes que lancen la misma magia usan el mismo clip.
        if (turno.selectedMagic != null && turno.selectedMagic.castClip != null)
            actorAnim.SetMagicClip(turno.selectedMagic.castClip);

        // 1. Animar hasta el momento de lanzamiento (comun a Tipo A y Tipo B)
        yield return StartCoroutine(actorAnim.PlayMagicUntilLaunch(turno.selectedMagic));
        AudioManager.Instance?.PlaySFX(turno.selectedMagic?.audioClip);

        // Si la magia falló: el visual aparece igual para que se vea el hechizo,
        // pero NO hay reacción ni dano/cura aplicado.
        //  - Tipo A (proyectil): vuela hacia un dummy más allá del target y se
        //    desvanece pasando de largo.
        //  - Tipo B (efecto sobre target): el efecto sale sobre el/los target(s),
        //    pero nadie hace TakeHit/Die.
        if (!turno.hit)
        {
            if (turno.selectedMagic.targetEffectPrefab != null)
                SpawnTargetEffectsParaMagia(actor, turno.selectedMagic, target, targetAnim);
            else if (targetAnim != null)
                SpawnProyectilFallido(actorAnim, targetAnim, ObtenerProyectil(actor, turno));

            yield return StartCoroutine(actorAnim.PlayMagicRemainder(turno.selectedMagic));
            yield break;
        }

        // ── Tipo B: efecto sobre target(s), no viaja ──
        // Detección por presencia de targetEffectPrefab. esArea decide si es
        // single-target (sobre el target seleccionado) o AOE (sobre todo el bando).
        if (turno.selectedMagic.targetEffectPrefab != null)
        {
            SpawnTargetEffectsParaMagia(actor, turno.selectedMagic, target, targetAnim);

            // Capturar quién estaba vivo ANTES de aplicar el dano AOE para
            // saber después quién acaba de morir (y no re-animar ya muertos).
            System.Collections.Generic.HashSet<CharacterStats> vivoAntesDano = null;
            if (turno.selectedMagic.esArea && turno.selectedMagic.magicType == MagicType.Damage)
            {
                bool actorEsEnemigo = actor == enemy;
                if (actorEsEnemigo && partyMembers != null)
                {
                    vivoAntesDano = new System.Collections.Generic.HashSet<CharacterStats>();
                    foreach (var m in partyMembers)
                        if (m != null && !m.IsDefeated) vivoAntesDano.Add(m);
                }
                else if (!actorEsEnemigo && enemy != null && !enemy.IsDefeated)
                {
                    vivoAntesDano = new System.Collections.Generic.HashSet<CharacterStats> { enemy };
                }
            }

            // AL APARECER el efecto sobre el/los target(s) se aplica el dano/cura.
            // Para Heal este es el momento natural: el aliado ve el efecto sobre él
            // y la barra sube. Para Damage la barra baja a la vez que aparece el
            // efecto y el TakeHit viene encima.
            aplicarConsecuencias?.Invoke();

            yield return StartCoroutine(actorAnim.PlayMagicRemainder(turno.selectedMagic));

            // Reacción de los target(s):
            //  - Heal: nadie reacciona (no hay golpe).
            //  - Damage single: el target reacciona (TakeHit/Die).
            //  - Damage AOE: cada miembro del bando opuesto reacciona en paralelo
            //    para que no se encadenen los TakeHits y todos respondan a la vez.
            if (turno.selectedMagic.magicType == MagicType.Damage)
            {
                if (!turno.selectedMagic.esArea)
                {
                    if (target != null && targetAnim != null)
                    {
                        if (target.IsDefeated)
                            yield return StartCoroutine(targetAnim.PlayDie());
                        else
                            yield return StartCoroutine(targetAnim.PlayTakeHit());
                    }
                }
                else
                {
                    yield return StartCoroutine(ReaccionAOEDamage(actor, vivoAntesDano));
                }
            }

            yield break;
        }

        // 2. Instanciar y lanzar el proyectil
        if (targetAnim != null)
        {
            bool impactó = false;
            GameObject prefab = ObtenerProyectil(actor, turno);

            if (prefab != null && actorAnim.puntoLanzamiento != null)
            {
                GameObject go = Instantiate(prefab,
                    actorAnim.puntoLanzamiento.position,
                    actorAnim.puntoLanzamiento.rotation);

                Projectile proyectil = go.GetComponent<Projectile>();
                if (proyectil != null)
                    proyectil.Inicializar(GetPuntoImpacto(targetAnim), () => impactó = true);
                else
                    impactó = true; // prefab sin script Projectile → continuar sin esperar
            }
            else
            {
                // Sin prefab configurado: simular delay de vuelo
                yield return new WaitForSeconds(0.5f);
                impactó = true;
            }

            // 3. Animar el resto del clip del actor mientras el proyectil vuela
            StartCoroutine(actorAnim.PlayMagicRemainder(turno.selectedMagic));

            // 4. Esperar a que el proyectil llegue
            yield return new WaitUntil(() => impactó);

            // 5. AL IMPACTO: aplicamos dano/cura. La barra baja en sincronía con
            //    el golpe del proyectil sobre el target.
            aplicarConsecuencias?.Invoke();

            // 6. El objetivo reacciona
            if (target.IsDefeated)
                yield return StartCoroutine(targetAnim.PlayDie());
            else
                yield return StartCoroutine(targetAnim.PlayTakeHit());
        }
        else
        {
            // No hay target válido: aplicamos al final del clip del actor.
            yield return StartCoroutine(actorAnim.PlayMagicRemainder(turno.selectedMagic));
            aplicarConsecuencias?.Invoke();
        }
    }

    // Lanza TakeHit/Die en paralelo sobre todo el bando opuesto al actor y
    // espera a que terminen. Como todos los TakeHit duran lo mismo, en la
    // práctica esperar al primero alcanza, pero recorremos la lista por si
    // algunos animadores tienen duraciones distintas.
    private IEnumerator ReaccionAOEDamage(CharacterStats actor,
        System.Collections.Generic.HashSet<CharacterStats> vivoAntes = null)
    {
        bool actorEsEnemigo = actor == enemy;
        List<Coroutine> reacciones = new List<Coroutine>();

        if (actorEsEnemigo)
        {
            if (partyMembers != null)
            {
                foreach (CharacterStats miembro in partyMembers)
                {
                    if (miembro == null) continue;
                    CharacterAnimator anim = GetAnimator(miembro);
                    if (anim == null) continue;

                    if (miembro.IsDefeated)
                    {
                        // Solo animar muerte si el miembro estaba vivo ANTES
                        // del AOE (acaba de morir ahora). Si ya estaba muerto,
                        // ignorarlo para no reiniciar su animación de muerte.
                        bool acabaDeMorir = vivoAntes == null || vivoAntes.Contains(miembro);
                        if (acabaDeMorir)
                            reacciones.Add(StartCoroutine(anim.PlayDie()));
                    }
                    else
                    {
                        reacciones.Add(StartCoroutine(anim.PlayTakeHit()));
                    }
                }
            }
        }
        else
        {
            if (enemy != null && enemyAnimator != null)
            {
                if (enemy.IsDefeated)
                    reacciones.Add(StartCoroutine(enemyAnimator.PlayDie()));
                else
                    reacciones.Add(StartCoroutine(enemyAnimator.PlayTakeHit()));
            }
        }

        foreach (Coroutine c in reacciones)
            yield return c;
    }

    private IEnumerator PlayDefensa(CharacterAnimator actorAnim)
    {
        if (actorAnim != null)
            yield return StartCoroutine(actorAnim.PlayDefend());
        else
            yield return new WaitForSeconds(0.8f);
    }

    private IEnumerator PlayItem(CharacterAnimator actorAnim, CharacterAnimator targetAnim, CharacterStats target)
    {
        AudioManager.Instance?.PlayPocion();
        // El que bebe la pocion es el que se va a curar, no el que la da
        CharacterAnimator animadorQueBebe = targetAnim != null ? targetAnim : actorAnim;
        if (animadorQueBebe != null)
            yield return StartCoroutine(animadorQueBebe.PlayUseItem());

        yield return new WaitForSeconds(0.3f);
    }

    // ──────────────────────────────────────────
    // HELPERS
    // ──────────────────────────────────────────

    // Devuelve el puntoImpacto del receptor si está configurado; si no,
    // cae al transform raíz (los pies). Esto evita que los proyectiles bajen
    // hacia los pies por interpolar entre alturas distintas.
    private Transform GetPuntoImpacto(CharacterAnimator targetAnim)
    {
        if (targetAnim == null) return null;
        return targetAnim.puntoImpacto != null ? targetAnim.puntoImpacto : targetAnim.transform;
    }

    // Instancia un efecto Tipo B parentado al ancla del receptor (para que
    // siga al personaje si se mueve) y lo destruye tras la duración indicada.
    // Spawnea un efecto Tipo B sobre el target. Si el prefab tiene un
    // BeamConnector, lo usa para conectar origenBeam→anchor (rayo que sale
    // de la mano del caster y llega al target). Si no, comportamiento clásico
    // (parentea el efecto al anchor con su rotación, lo que sirve para
    // efectos esféricos/envolventes tipo Pulso Sombrío o auras).
    private void SpawnTargetEffect(GameObject prefab, Transform anchor, float duration, Transform origenBeam = null)
    {
        if (prefab == null || anchor == null) return;

        BeamConnector beamProto = prefab.GetComponent<BeamConnector>();
        if (beamProto != null && origenBeam != null)
        {
            // Rayo direccional: spawn SIN parent (BeamConnector va a setear
            // pos/rot/scale absolutos en mundo, no debe heredar transform).
            GameObject go = Instantiate(prefab);
            BeamConnector beam = go.GetComponent<BeamConnector>();
            beam.Configurar(origenBeam.position, anchor.position);
            Destroy(go, duration);
        }
        else
        {
            GameObject go = Instantiate(prefab, anchor.position, anchor.rotation, anchor);
            Destroy(go, duration);
        }
    }

    // Spawnea un proyectil que, en lugar de impactar al target, vuela hacia un
    // punto MÁS ALLÁ del target en línea recta. Sirve para visualizar ataques
    // fallados: el proyectil aparece, "atraviesa" la posición del target y se
    // desvanece al llegar al dummy. No registra callback de impacto.
    private void SpawnProyectilFallido(CharacterAnimator actorAnim,
                                        CharacterAnimator targetAnim,
                                        GameObject prefab)
    {
        if (prefab == null || actorAnim == null || actorAnim.puntoLanzamiento == null) return;
        Transform impacto = GetPuntoImpacto(targetAnim);
        if (impacto == null) return;

        GameObject go = Instantiate(prefab,
            actorAnim.puntoLanzamiento.position,
            actorAnim.puntoLanzamiento.rotation);

        Projectile proyectil = go.GetComponent<Projectile>();
        if (proyectil == null)
        {
            // Prefab sin script Projectile: que no se quede en la mano del actor.
            // Lo destruimos solo tras un par de segundos.
            Destroy(go, 2f);
            return;
        }

        Transform dummy = CrearDummyFlyThrough(actorAnim.puntoLanzamiento, impacto);
        proyectil.Inicializar(dummy, null);
    }

    // Crea un GameObject vacío en línea recta desde origen → impacto, pero
    // extendido distanciaExtra metros más allá. El Projectile lo usa como
    // "objetivo" para volar de largo y autodestruirse al llegar. El dummy
    // se autodestruye tras unos segundos por si el proyectil se queda colgado.
    private Transform CrearDummyFlyThrough(Transform origen, Transform impacto, float distanciaExtra = 8f)
    {
        GameObject dummy = new GameObject("ProyectilFalladoTarget");
        Vector3 direccion = (impacto.position - origen.position).normalized;
        if (direccion == Vector3.zero) direccion = origen.forward;
        dummy.transform.position = impacto.position + direccion * distanciaExtra;
        Destroy(dummy, 5f);
        return dummy.transform;
    }

    // Decide a quién(es) afecta una magia Tipo B según esArea + tipo + bando del actor:
    //  - !esArea         → sobre el target seleccionado (lo que decida CombatManager)
    //  - esArea + Heal   → sobre el MISMO bando del actor (sus aliados vivos)
    //  - esArea + Damage → sobre el bando OPUESTO del actor (enemigos vivos)
    private void SpawnTargetEffectsParaMagia(CharacterStats actor,
                                             Magic magia,
                                             CharacterStats target,
                                             CharacterAnimator targetAnim)
    {
        if (magia == null || magia.targetEffectPrefab == null) return;

        // Origen del rayo (si la magia tiene BeamConnector en su prefab):
        // la mano/puntoLanzamiento del caster. Si la magia es Tipo B normal
        // (sin BeamConnector), este transform es ignorado por SpawnTargetEffect.
        CharacterAnimator actorAnim = GetAnimator(actor);
        Transform origenBeam = actorAnim != null ? actorAnim.puntoLanzamiento : null;

        if (!magia.esArea)
        {
            if (targetAnim != null)
                SpawnTargetEffect(magia.targetEffectPrefab,
                                  GetPuntoImpacto(targetAnim),
                                  magia.effectDuration,
                                  origenBeam);
            return;
        }

        bool actorEsEnemigo = actor == enemy;
        bool esCura = magia.magicType == MagicType.Heal;

        // El bando objetivo del AOE depende del tipo y del bando del actor.
        bool aplicarAParty = esCura ? !actorEsEnemigo : actorEsEnemigo;

        if (aplicarAParty)
        {
            if (partyMembers == null) return;
            foreach (CharacterStats miembro in partyMembers)
            {
                if (miembro == null || miembro.IsDefeated) continue;
                SpawnTargetEffect(magia.targetEffectPrefab,
                                  GetPuntoImpacto(GetAnimator(miembro)),
                                  magia.effectDuration,
                                  origenBeam);
            }
        }
        else
        {
            // Con un único enemigo, se opera directamente sobre él.
            // Si se añaden más enemigos, este bloque debería iterar sobre la lista de vivos.
            if (enemy != null && !enemy.IsDefeated)
                SpawnTargetEffect(magia.targetEffectPrefab,
                                  GetPuntoImpacto(enemyAnimator),
                                  magia.effectDuration,
                                  origenBeam);
        }
    }

    private CharacterAnimator GetAnimator(CharacterStats stats)
    {
        if (stats == null) return null;
        if (stats == enemy) return enemyAnimator;

        if (partyMembers != null)
        {
            int idx = System.Array.IndexOf(partyMembers, stats);
            if (idx >= 0 && idx < partyAnimators.Length)
                return partyAnimators[idx];
        }

        return null;
    }

    private GameObject ObtenerProyectil(CharacterStats actor, TurnData turno)
    {
        // Para magias: el prefab vive en el SO de la magia (mismo prefab para
        // cualquier personaje que lance ese hechizo). Si el SO no tiene prefab
        // (típicamente Tipo B — efecto sobre target sin proyectil que viaje),
        // devolvemos null y PlayMagia sigue sin instanciar nada.
        if (turno.actionType == CombatAction.Magic)
            return turno.selectedMagic != null ? turno.selectedMagic.projectilePrefab : null;

        // Para ataques básicos: el proyectil vive en el array por personaje
        // (cubo/forma característica de cada uno, asignado en el Inspector).
        if (actor == enemy) return proyectilEnemigo;

        if (partyMembers != null)
        {
            int idx = System.Array.IndexOf(partyMembers, actor);
            if (idx >= 0 && proyectilesPorPersonaje != null && idx < proyectilesPorPersonaje.Length)
                return proyectilesPorPersonaje[idx];
        }

        return null;
    }
}