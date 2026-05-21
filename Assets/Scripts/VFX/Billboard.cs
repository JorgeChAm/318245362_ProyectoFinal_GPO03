using UnityEngine;

/// <summary>
/// Rota el Transform para que siempre mire hacia la cámara principal.
/// Usar en el quad de la llama de la antorcha.
/// </summary>
[ExecuteAlways]
public class Billboard : MonoBehaviour
{
    [Tooltip("Si true, solo copia la rotación de la cámara (más rápido). " +
             "Si false, hace LookAt completo (útil si la cámara puede orbitar).")]
    public bool copyRotation = true;

    private Transform _camTransform;

    void OnEnable()
    {
        RefreshCamera();
    }

    void LateUpdate()
    {
        if (_camTransform == null) RefreshCamera();
        if (_camTransform == null) return;

        if (copyRotation)
        {
            // Hereda la rotación de la cámara — correcto para side-scroller
            // donde la cámara no orbita.
            transform.rotation = _camTransform.rotation;
        }
        else
        {
            // LookAt completo: funciona aunque la cámara cambie de ángulo.
            transform.LookAt(
                transform.position + _camTransform.rotation * Vector3.forward,
                _camTransform.rotation * Vector3.up
            );
        }
    }

    void RefreshCamera()
    {
        if (Camera.main != null)
            _camTransform = Camera.main.transform;
    }
}
