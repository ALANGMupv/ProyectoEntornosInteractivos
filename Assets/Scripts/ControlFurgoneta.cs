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

    [Header("Carretera")]
    [Tooltip("Si está activo, la furgoneta solo puede moverse por encima de los colliders de la capa de carretera.")]
    public bool limitarACarretera;
    [Tooltip("Capa de los colliders de la carretera. Si se deja en Nothing usa la capa \"Carretera\".")]
    public LayerMask capaCarretera;
    [Tooltip("Distancia por delante del centro que también tiene que estar sobre la carretera (el morro), en unidades del mapa.")]
    public float margenFrontal = 0.01f;
    [Tooltip("Altura desde la que se lanza el rayo hacia abajo para buscar la carretera, en unidades del mapa.")]
    public float alturaRayo = 0.05f;

    [Header("Sprint 02 (opcional, solo escena iluminada)")]
    public bool mejorarApoyo;
    public float semiancho = 0.006f;
    public float margenTrasero = 0.01f;

    InputAction m_Mover;
    InputAction m_Reiniciar;
    float m_Velocidad;
    // Parada en el borde de la carretera: deja girar sin avanzar para poder salir
    bool m_Bloqueada;
    bool m_EsperarSoltarControl;

    // Locales al mapa: en RA el mapa se mueve con el Image Target
    Vector3 m_PosicionInicial;
    Quaternion m_RotacionInicial;

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

        // Tecla R para reiniciar en el editor (en el móvil se usa el botón, que llama a Reiniciar())
        m_Reiniciar = new InputAction("Reiniciar", InputActionType.Button, "<Keyboard>/r");

        m_PosicionInicial = transform.localPosition;
        m_RotacionInicial = transform.localRotation;

        if (capaCarretera == 0)
            capaCarretera = LayerMask.GetMask("Carretera");
    }

    void OnEnable()
    {
        m_Mover.Enable();
        m_Reiniciar.Enable();
    }

    void OnDisable()
    {
        m_Mover.Disable();
        m_Reiniciar.Disable();
    }

    void OnDestroy()
    {
        m_Mover.Dispose();
        m_Reiniciar.Dispose();
    }

    /// <summary>Devuelve la furgoneta a su posición y rotación iniciales, parada.</summary>
    public void Reiniciar()
    {
        transform.localPosition = m_PosicionInicial;
        transform.localRotation = m_RotacionInicial;
        m_Velocidad = 0f;
        m_Bloqueada = false;
        m_EsperarSoltarControl = mejorarApoyo;
    }

    void Update()
    {
        if (m_Reiniciar.WasPressedThisFrame())
        {
            Reiniciar();
            return;
        }

        MoverConEntrada(m_Mover.ReadValue<Vector2>(), Time.deltaTime);
    }

    void MoverConEntrada(Vector2 entrada, float deltaTime)
    {
        if (camara == null && Camera.main != null)
            camara = Camera.main.transform;
        if (camara == null)
            return;

        Transform referenciaSuelo = suelo != null ? suelo : transform.parent;
        Vector3 arriba = referenciaSuelo != null ? referenciaSuelo.up : Vector3.up;
        // En RA el mapa se puede escalar: las velocidades se expresan en unidades del mapa
        float escala = referenciaSuelo != null ? Mathf.Abs(referenciaSuelo.lossyScale.x) : 1f;

        entrada = Vector2.ClampMagnitude(entrada, 1f);
        float intensidad = entrada.magnitude;
        // Tras reiniciar, una pulsación todavía mantenida no debe sacar el coche de la salida.
        if (m_EsperarSoltarControl)
        {
            if (intensidad > zonaMuerta) return;
            m_EsperarSoltarControl = false;
        }

        if (intensidad > zonaMuerta)
        {
            Vector3 direccion = DireccionRelativaACamara(entrada, arriba);
            Quaternion objetivo = Quaternion.LookRotation(direccion, arriba);

            // Cuanto más de espaldas a la dirección pedida, más lento va, para que trace la curva cerrada
            float alineacion = Vector3.Dot(transform.forward, direccion);
            float velocidadObjetivo = velocidadMaxima * intensidad * Mathf.Lerp(0.35f, 1f, (alineacion + 1f) * 0.5f);
            m_Velocidad = Mathf.MoveTowards(m_Velocidad, velocidadObjetivo, aceleracion * deltaTime);

            // Como un coche: solo gira si se está moviendo (o si está parada en el borde, para poder salir)
            float factorGiro = m_Bloqueada ? 1f : Mathf.Clamp01(m_Velocidad / (velocidadMaxima * 0.3f));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, objetivo,
                velocidadGiro * factorGiro * deltaTime);
        }
        else
        {
            m_Velocidad = Mathf.MoveTowards(m_Velocidad, 0f, frenado * deltaTime);
        }

        Vector3 desplazamiento = transform.forward * (m_Velocidad * escala * deltaTime);
        if (!limitarACarretera)
        {
            transform.position += desplazamiento;
            return;
        }

        // En RA el mapa se mueve con el target: actualizar los colliders antes de lanzar rayos
        Physics.SyncTransforms();
        if (mejorarApoyo)
        {
            // Subpasos: evita atravesar un borde cuando baja la tasa de fotogramas.
            int pasos = Mathf.Max(1, Mathf.CeilToInt(desplazamiento.magnitude / Mathf.Max(0.001f * escala, 0.00001f)));
            Vector3 paso = desplazamiento / pasos;
            m_Bloqueada = false;
            for (int i = 0; i < pasos; i++)
            {
                Vector3 siguiente = transform.position + paso;
                if (!ApoyoCompleto(siguiente, arriba, escala) &&
                    !PuedeRecuperarApoyo(transform.position, siguiente, arriba, escala))
                {
                    m_Bloqueada = true;
                    m_Velocidad = 0f;
                    // Conservar el giro permite recuperar la dirección hacia la carretera.
                    // Restaurarlo aquí bloqueaba el vehículo al pedir un giro junto al borde.
                    break;
                }
                transform.position = siguiente;
            }
            if (BuscarCarretera(transform.position, arriba, escala, out var contacto))
                transform.position = contacto.point;
            return;
        }
        Vector3 morro = transform.position + desplazamiento + transform.forward * (margenFrontal * escala);
        m_Bloqueada = m_Velocidad > 0f && !BuscarCarretera(morro, arriba, escala, out _);
        if (m_Bloqueada)
            m_Velocidad = 0f;
        else
            transform.position += desplazamiento;

        // Seguir la altura de la carretera (cuestas, baches del modelo)
        if (BuscarCarretera(transform.position, arriba, escala, out var apoyo))
            transform.position = apoyo.point;
    }

    bool ApoyoCompleto(Vector3 centro, Vector3 arriba, float escala)
    {
        // Restringir el centro y el ancho de apoyo a la calzada. Exigir también
        // las esquinas del morro/trasera impedía tomar curvas y recuperar el giro.
        if (!BuscarCarretera(centro, arriba, escala, out _)) return false;
        Vector3 lateral = transform.right * (semiancho * escala);
        return BuscarCarretera(centro - lateral, arriba, escala, out _) &&
               BuscarCarretera(centro + lateral, arriba, escala, out _);
    }

    bool PuedeRecuperarApoyo(Vector3 actual, Vector3 siguiente, Vector3 arriba, float escala)
    {
        // Si un giro deja un lateral sobre el margen, permitir volver hacia dentro.
        // No autorizar salir desde una posición apoyada ni atravesar zonas sin calzada.
        if (ApoyoCompleto(actual, arriba, escala)) return false;
        Vector3 avance = siguiente - actual;
        if (avance.sqrMagnitude < 1e-14f) return false;
        Vector3 direccion = avance.normalized;
        float paso = Mathf.Max(.001f * escala, .00001f);
        float alcance = Mathf.Max(semiancho * 2f, .002f) * escala;
        for (float distancia = 0f; distancia <= alcance + paso * .5f; distancia += paso)
        {
            Vector3 punto = siguiente + direccion * distancia;
            if (!BuscarCarretera(punto, arriba, escala, out _)) return false;
            if (ApoyoCompleto(punto, arriba, escala)) return true;
        }
        return false;
    }

    bool BuscarCarretera(Vector3 punto, Vector3 arriba, float escala, out RaycastHit impacto)
    {
        float altura = alturaRayo * escala;
        return Physics.Raycast(punto + arriba * altura, -arriba, out impacto, altura * 2f,
            capaCarretera, QueryTriggerInteraction.Ignore);
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
