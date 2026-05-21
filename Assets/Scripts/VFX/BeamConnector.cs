using UnityEngine;

// Componente para que un mesh tipo "rayo" (cilindro alargado, viga, beam)
// conecte dos puntos en el espacio del mundo: posiciona, rota y estira el
// mesh en su eje longitudinal de origen a destino para que vaya en línea
// recta entre los dos.
//
// USO:
//   1. Anadir este componente al GameObject raíz del prefab del rayo.
//   2. Configurar lengthAxis y longitudOriginalMesh según el mesh:
//        - Cilindro primitivo de Unity: Y, longitudOriginalMesh = 2.
//        - Quad de Unity orientado en X: X, longitudOriginalMesh = 1.
//        - Modelo custom: depende del FBX.
//   3. Quien spawnee el prefab debe llamar Configurar(origen, destino)
//      inmediatamente después de Instantiate, ANTES de que pase un frame.
//
// REUTILIZABLE PARA: cualquier hechizo o efecto que conecte dos puntos del
// mundo — rayos, cadenas mágicas, lazos, vigas de luz, lazos de canalización,
// haces direccionales, etc.
public class BeamConnector : MonoBehaviour
{
    public enum LengthAxis { X, Y, Z }

    [Header("Orientación del mesh")]
    [Tooltip("Eje LOCAL del mesh donde está la longitud. Cilindro primitivo de Unity = Y.")]
    public LengthAxis lengthAxis = LengthAxis.Y;

    [Tooltip("Longitud natural del mesh sin escalar (cuando localScale = 1). Cilindro primitivo de Unity = 2 unidades. Quad = 1 unidad. Modelo custom = lo que mida el FBX.")]
    public float longitudOriginalMesh = 2f;

    [Header("Dimensiones")]
    [Tooltip("Escala aplicada a los dos ejes perpendiculares al longitudinal. Para un cilindro primitivo, equivale aproximadamente al diámetro visible en unidades de mundo.")]
    public float grosor = 0.3f;

    [Tooltip("Multiplicador de la longitud calculada. 1.0 = exactamente origen→destino. <1 = más corto que la distancia (no atraviesa al target). >1 = se extiende un poco más allá del target.")]
    public float factorLongitud = 1f;

    // Posiciona, rota y escala este GameObject para conectar origen y destino
    // en el espacio mundial. Llamar inmediatamente después de Instantiate,
    // antes de que se renderice el primer frame del efecto.
    public void Configurar(Vector3 origen, Vector3 destino)
    {
        Vector3 delta = destino - origen;
        float distancia = delta.magnitude;

        // Protección contra origen == destino (evita división por cero al normalizar).
        if (distancia < 0.001f)
        {
            Debug.LogWarning($"[BeamConnector] Distancia ~0, abortando. Origen y destino están en el mismo punto.");
            return;
        }

        // 1) POSICIÓN: punto medio entre origen y destino.
        // El mesh se estira simétricamente desde su pivot (asumimos centrado),
        // así que el centro debe quedar a la mitad del recorrido.
        transform.position = origen + delta * 0.5f;

        // 2) ROTACIÓN: girar el GameObject para que el eje longitudinal del
        // mesh apunte hacia destino. FromToRotation calcula el quaternion
        // mínimo que rota el primer vector hasta alinearse con el segundo.
        Vector3 dirLocal = ObtenerDireccionLocal();
        Vector3 dirMundo = delta.normalized;
        transform.rotation = Quaternion.FromToRotation(dirLocal, dirMundo);

        // 3) ESCALA: estirar el eje longitudinal para cubrir la distancia,
        // dejando los otros dos ejes con el grosor configurado.
        float longitudObjetivo = distancia * factorLongitud;
        float escalaLongitudinal = longitudObjetivo / Mathf.Max(longitudOriginalMesh, 0.0001f);
        transform.localScale = ConstruirEscala(escalaLongitudinal);
    }

    // Devuelve el vector unitario del eje longitudinal en espacio local del mesh.
    private Vector3 ObtenerDireccionLocal()
    {
        return lengthAxis switch
        {
            LengthAxis.X => Vector3.right,
            LengthAxis.Y => Vector3.up,
            LengthAxis.Z => Vector3.forward,
            _            => Vector3.up
        };
    }

    // Construye el Vector3 de localScale: la longitudinal va en el eje del mesh,
    // el grosor en los otros dos.
    private Vector3 ConstruirEscala(float escalaLongitudinal)
    {
        return lengthAxis switch
        {
            LengthAxis.X => new Vector3(escalaLongitudinal, grosor, grosor),
            LengthAxis.Y => new Vector3(grosor, escalaLongitudinal, grosor),
            LengthAxis.Z => new Vector3(grosor, grosor, escalaLongitudinal),
            _            => Vector3.one
        };
    }
}
