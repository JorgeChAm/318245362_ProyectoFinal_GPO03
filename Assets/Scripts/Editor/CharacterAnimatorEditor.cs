#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEditor;

// Custom Editor del CharacterAnimator. Anade un panel con botones para
// disparar cada animación de combate por separado, sin tener que entrar a
// un combate real. Útil para revisar timings, transiciones y poses.
//
// IMPORTANTE: los botones solo funcionan en Play Mode porque el Animator
// necesita estar inicializado para responder a triggers/bools.
[CustomEditor(typeof(CharacterAnimator))]
public class CharacterAnimatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Preview de animaciones", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Entra a Play Mode para usar los botones de preview.",
                MessageType.Info);
            return;
        }

        CharacterAnimator anim = (CharacterAnimator)target;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Attack"))   anim.StartCoroutine(SecuenciaAtaqueBasico(anim));
        if (GUILayout.Button("Use Item")) anim.StartCoroutine(anim.PlayUseItem());
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Take Hit")) anim.StartCoroutine(anim.PlayTakeHit());
        if (GUILayout.Button("Die"))      anim.StartCoroutine(anim.PlayDie());
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Defend (entrar)")) anim.StartCoroutine(anim.PlayDefend());
        if (GUILayout.Button("Defend (salir)"))  anim.ExitDefend();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Magias del personaje", EditorStyles.miniBoldLabel);

        // Busca un CharacterStats en el mismo GameObject, en el padre o en hijos.
        // Esto cubre los tres patrones de jerarquía habituales (script en raíz,
        // o script en el modelo hijo).
        CharacterStats stats = anim.GetComponent<CharacterStats>();
        if (stats == null) stats = anim.GetComponentInParent<CharacterStats>();
        if (stats == null) stats = anim.GetComponentInChildren<CharacterStats>();

        if (stats == null || stats.magics == null || stats.magics.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "No se encontraron magias en el CharacterStats asociado.",
                MessageType.Info);
            return;
        }

        foreach (Magic magia in stats.magics)
        {
            if (magia == null) continue;

            string label = string.IsNullOrEmpty(magia.nombre) ? magia.name : magia.nombre;
            string tooltip = magia.castClip == null
                ? "Esta magia no tiene castClip asignado."
                : $"Lanza {label} ({magia.castClip.name})";

            using (new EditorGUI.DisabledScope(magia.castClip == null))
            {
                if (GUILayout.Button(new GUIContent(label, tooltip)))
                {
                    anim.SetMagicClip(magia.castClip);
                    anim.StartCoroutine(SecuenciaMagia(anim, magia));
                }
            }
        }
    }

    private IEnumerator SecuenciaAtaqueBasico(CharacterAnimator anim)
    {
        yield return anim.StartCoroutine(anim.PlayAttackUntilLaunch());
        SpawnPreviewProjectile(anim, anim.previewBasicAttackProjectile);
        yield return anim.StartCoroutine(anim.PlayAttackRemainder());
    }

    private IEnumerator SecuenciaMagia(CharacterAnimator anim, Magic magia)
    {
        yield return anim.StartCoroutine(anim.PlayMagicUntilLaunch(magia));

        // Si la magia es Tipo B (efecto sobre target sin viaje), spawnea el
        // efecto sobre previewTarget (o sobre el propio actor si no hay target).
        // Si no, spawnea el proyectil Tipo A como antes.
        if (magia.targetEffectPrefab != null)
            SpawnPreviewTargetEffect(anim, magia.targetEffectPrefab, magia.effectDuration);
        else
            SpawnPreviewProjectile(anim, magia.projectilePrefab);

        yield return anim.StartCoroutine(anim.PlayMagicRemainder(magia));
    }

    // Instancia el prefab Tipo B sobre el previewTarget (o sobre el propio
    // personaje si no hay target asignado). Lo destruye tras effectDuration.
    //
    // Si el prefab tiene BeamConnector, lo trata como rayo: lo conecta desde
    // el puntoLanzamiento del actor hasta el previewTarget en lugar de
    // simplemente parentarlo al ancla.
    private void SpawnPreviewTargetEffect(CharacterAnimator anim, GameObject prefab, float duration)
    {
        if (prefab == null) return;

        Transform anchor = anim.previewTarget != null ? anim.previewTarget : anim.transform;

        BeamConnector beamProto = prefab.GetComponent<BeamConnector>();
        if (beamProto != null)
        {
            // Rayo: spawn sin parent y dejar que BeamConnector setee el transform.
            GameObject go = Object.Instantiate(prefab);
            BeamConnector beam = go.GetComponent<BeamConnector>();
            Vector3 origen = anim.puntoLanzamiento != null
                ? anim.puntoLanzamiento.position
                : anim.transform.position;
            beam.Configurar(origen, anchor.position);
            Object.Destroy(go, duration);
        }
        else
        {
            GameObject go = Object.Instantiate(prefab, anchor.position, anchor.rotation, anchor);
            Object.Destroy(go, duration);
        }
    }

    // Instancia el proyectil desde puntoLanzamiento y lo inicializa hacia
    // previewTarget (o un dummy 5 unidades enfrente si no hay target asignado).
    // Si no hay prefab o no hay puntoLanzamiento, no hace nada — la animación
    // sigue corriendo igual.
    private void SpawnPreviewProjectile(CharacterAnimator anim, GameObject prefab)
    {
        if (prefab == null || anim.puntoLanzamiento == null) return;

        GameObject go = Object.Instantiate(prefab,
            anim.puntoLanzamiento.position,
            anim.puntoLanzamiento.rotation);

        Projectile proj = go.GetComponent<Projectile>();
        if (proj == null) return; // prefab sin script Projectile: solo aparece, no vuela

        Transform target = anim.previewTarget;
        if (target == null)
        {
            // Crea un transform dummy 5 metros adelante para que el proyectil tenga
            // a dónde volar. Se autodestruye después; el Projectile se destruye solo
            // al impactar o al ver el target null.
            GameObject dummy = new GameObject("PreviewTargetTemp");
            dummy.transform.position = anim.puntoLanzamiento.position
                                     + anim.transform.forward * 5f;
            target = dummy.transform;
            Object.Destroy(dummy, 5f);
        }

        proj.Inicializar(target, null);
    }
}
#endif
