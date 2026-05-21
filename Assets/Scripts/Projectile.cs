using System;
using System.Collections;
using UnityEngine;

// Proyectil genérico. Se mueve hacia un objetivo y al llegar dispara un callback.
//
// SETUP EN UNITY:
// 1. Crear un prefab simple (cubo, esfera, cápsula).
// 2. Anadir este script.
// 3. Asignar el prefab al campo "proyectilPrefab" de cada CharacterAnimator
//    o al CombatAnimator según la magia.
public class Projectile : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 12f;

    [Header("Curva de vuelo (opcional)")]
    [Tooltip("Si es mayor a 0, el proyectil hace un arco. 0 = línea recta.")]
    public float alturaArco = 0f;

    private Transform objetivo;
    private Action onImpacto;
    private Vector3 posicionInicio;
    private float distanciaTotal;
    private float progreso = 0f;
    private bool llegó = false;

    // CombatAnimator llama esto justo después de Instantiate.
    public void Inicializar(Transform objetivo, Action onImpacto)
    {
        this.objetivo    = objetivo;
        this.onImpacto   = onImpacto;
        posicionInicio   = transform.position;
        distanciaTotal   = Vector3.Distance(posicionInicio, objetivo.position);
    }

    private void Update()
    {
        if (llegó || objetivo == null)
        {
            if (!llegó) Impactar(); // objetivo destruido antes de llegar
            return;
        }

        // Avanzar progreso según velocidad y distancia total
        progreso += (velocidad / distanciaTotal) * Time.deltaTime;
        progreso  = Mathf.Clamp01(progreso);

        // Interpolación en línea recta (o con arco si alturaArco > 0)
        Vector3 posLineal = Vector3.Lerp(posicionInicio, objetivo.position, progreso);

        if (alturaArco > 0f)
        {
            // Parábola simple: seno del ángulo de progreso × altura máxima
            float arcoY = Mathf.Sin(progreso * Mathf.PI) * alturaArco;
            posLineal.y += arcoY;
        }

        transform.position = posLineal;

        // Rotar hacia la dirección de movimiento para que "apunte" al objetivo
        Vector3 direccion = objetivo.position - transform.position;
        if (direccion != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direccion);

        if (progreso >= 1f)
            Impactar();
    }

    private void Impactar()
    {
        llegó = true;
        onImpacto?.Invoke();
        Destroy(gameObject);
    }
}