using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mueve la furgoneta con un joystick relativo a la cámara (pensado para RA):
/// joystick arriba = alejarse de la cámara, derecha = derecha de la pantalla,
/// sin importar hacia dónde mire la furgoneta. La furgoneta gira hacia esa
/// dirección como un coche (no gira sobre sí misma) y se para al soltar el joystick.
/// </summary>
public class ControlFurgoneta : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Cámara del jugador (en RA, la AR Camera). Si se deja vacío usa Camera.main.")]
    public Transform camara;

    [Tooltip("Objeto que hace de suelo (el mapa / Image Target). Define qué es 'arriba' y la escala. " +
             "Si se deja vacío usa el padre de la furgoneta.")]
    public Transform suelo;

    [Header("Movimiento (en unidades del mapa sin escalar)")]
    public float velocidadMaxima = 1.5f;
    public float aceleracion = 3f;
    [Tooltip("Frenado al soltar el joystick. Alto = se para casi en seco.")]
    public float frenado = 8f;

    [Header("Giro")]
    [Tooltip("Grados por segundo que gira la furgoneta yendo a máxima velocidad.")]
    public float velocidadGiro = 180f;
    [Tooltip("Por debajo de este valor el joystick se considera suelto.")]
    public float zonaMuerta = 0.15f;

    InputAction m_Mover;
    float m_Velocidad;

    void Awake()
    {
        // Joystick en pantalla (On-Screen Stick → <Gamepad>/leftStick), mando real o WASD/flechas para probar en el editor
        m_Mover = new InputAction("Mover", InputActionType.Value);
        m_Mover.AddBinding("<Gamepad>/leftStick");
        m_Mover.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        m_Mover.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
    }

    void OnEnable() => m_Mover.Enable();
    void OnDisable() => m_Mover.Disable();
    void OnDestroy() => m_Mover.Dispose();

    void Update()
    {
        if (camara == null && Camera.main != null)
            camara = Camera.main.transform;
        if (camara == null)
            return;

        Transform referenciaSuelo = suelo != null ? suelo : transform.parent;
        Vector3 arriba = referenciaSuelo != null ? referenciaSuelo.up : Vector3.up;
        // En RA el mapa se puede escalar: las velocidades se expresan en unidades del mapa
        float escala = referenciaSuelo != null ? referenciaSuelo.lossyScale.x : 1f;

        Vector2 entrada = Vector2.ClampMagnitude(m_Mover.ReadValue<Vector2>(), 1f);
        float intensidad = entrada.magnitude;

        if (intensidad > zonaMuerta)
        {
            Vector3 direccion = DireccionRelativaACamara(entrada, arriba);
            Quaternion objetivo = Quaternion.LookRotation(direccion, arriba);

            // Cuanto más de espaldas a la dirección pedida, más lento va, para que trace la curva cerrada
            float alineacion = Vector3.Dot(transform.forward, direccion);
            float velocidadObjetivo = velocidadMaxima * intensidad * Mathf.Lerp(0.35f, 1f, (alineacion + 1f) * 0.5f);
            m_Velocidad = Mathf.MoveTowards(m_Velocidad, velocidadObjetivo, aceleracion * Time.deltaTime);

            // Como un coche: solo gira si se está moviendo
            float factorGiro = Mathf.Clamp01(m_Velocidad / (velocidadMaxima * 0.3f));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, objetivo,
                velocidadGiro * factorGiro * Time.deltaTime);
        }
        else
        {
            m_Velocidad = Mathf.MoveTowards(m_Velocidad, 0f, frenado * Time.deltaTime);
        }

        transform.position += transform.forward * (m_Velocidad * escala * Time.deltaTime);
    }

    /// <summary>
    /// Convierte el joystick en una dirección sobre el plano del suelo según hacia dónde mira la cámara.
    /// Usa el eje derecho de la cámara aplanado y saca el "adelante" con un producto vectorial,
    /// así funciona aunque el móvil apunte casi en vertical hacia el mapa.
    /// </summary>
    Vector3 DireccionRelativaACamara(Vector2 entrada, Vector3 arriba)
    {
        Vector3 derecha = Vector3.ProjectOnPlane(camara.right, arriba).normalized;
        Vector3 adelante = Vector3.Cross(derecha, arriba);
        return (derecha * entrada.x + adelante * entrada.y).normalized;
    }
}
