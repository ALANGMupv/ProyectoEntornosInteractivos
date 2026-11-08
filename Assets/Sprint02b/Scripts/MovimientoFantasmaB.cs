
using UnityEngine;

/// <summary>
/// Movimiento del fantasma jugable: vuela libremente por la habitación de un punto
/// a otro, trazando curvas suaves. Cada punto de destino es una posición fija del
/// mundo: girar el móvil no le afecta y, si caminas hacia él, puedes acercarte.
/// </summary>
public class MovimientoFantasmaB : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Distancia mínima al jugador a la que elige sus destinos.")]
    public float distanciaMinima = 1.5f;
    [Tooltip("Distancia máxima al jugador a la que elige sus destinos.")]
    public float distanciaMaxima = 3f;
    [Tooltip("Grados por segundo que puede girar al cambiar de rumbo.")]
    public float velocidadGiro = 40f;
    [Tooltip("Velocidad de vuelo en metros por segundo.")]
    public float velocidadMovimiento = 0.5f;
    [Tooltip("Segundos para acelerar y frenar: evita los cambios bruscos.")]
    public float suavizado = 0.8f;

    [Header("Ajuste global")]
    [Tooltip("Multiplica la velocidad de vuelo de todas las variantes.")]
    public float escalaVelocidad = 1.9f;
    [Tooltip("Multiplica la velocidad de giro de todas las variantes.")]
    public float escalaGiro = 2.3f;

    [Header("Distancia a la cámara")]
    [Tooltip("Distancia mínima garantizada entre la cámara y cualquier parte visible del modelo.")]
    public float margenVisible = 0.6f;
    [Tooltip("Metros extra, por fuera del margen, en los que empieza a apartarse con suavidad.")]
    public float margenSuave = 0.5f;

    private const float RadioLlegada = 0.4f;

    // Duración máxima de cada tramo: a veces cambia de rumbo antes de llegar al destino
    private float tiempoMaximoTramo = 5f;

    // Esfera que envuelve todas las partes visibles del modelo: desde el pivote (en los
    // pies) hasta su centro, y su radio. Se mide al aparecer, así vale para cualquier modelo.
    private Vector3 desfaseCentro;
    private float radioModelo;

    // Distancia mínima entre la cámara y el centro del modelo, y la de inicio del apartado
    private float distanciaSegura;
    private float espacioPersonal;

    private Transform camara;
    private Quaternion rotacionBase;

    private float alturaBase;
    private Vector3 destino;
    private Vector3 velocidad;
    private float factorTramo = 1f;
    private float tiempoTramo;

    public void Inicializar(Transform camaraJugador)
    {
        camara = camaraJugador;

        // Moverse en el mundo y no dentro del ancla, cuya posición corrige el seguimiento
        transform.SetParent(null, true);

        // Solo este script lo mueve: el Rigidbody dinámico hacía que la física lo
        // desplazara y lo girara por su cuenta al chocar con otros colliders
        Rigidbody cuerpo = GetComponent<Rigidbody>();
        if (cuerpo != null)
        {
            cuerpo.linearVelocity = Vector3.zero;
            cuerpo.angularVelocity = Vector3.zero;
            cuerpo.isKinematic = true;
        }

        // Guardar la orientacion original del prefab
        rotacionBase = transform.rotation;

        // Medir el tamaño real del modelo con sus partes visibles (no con el collider,
        // que es más pequeño que el modelo y no tiene en cuenta cabeza, sombrero, etc.)
        MedirModelo();
        distanciaSegura = radioModelo + margenVisible;
        espacioPersonal = distanciaSegura + margenSuave;

        alturaBase = camara.position.y - 0.1f;

        ElegirDestino();
        velocidad = (destino - transform.position).normalized * 0.1f;
    }

    void LateUpdate()
    {
        if (camara == null)
            return;

        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        Vector3 posicion = transform.position;

        // Cambiar de destino al llegar (o si tarda demasiado en alcanzarlo)
        tiempoTramo += dt;
        Vector3 haciaDestino = destino - posicion;
        if (haciaDestino.magnitude < RadioLlegada || tiempoTramo > tiempoMaximoTramo)
        {
            ElegirDestino();
            haciaDestino = destino - posicion;
        }

        float velocidadMaxima = velocidadMovimiento * escalaVelocidad * factorTramo;
        Vector3 direccionDeseada = haciaDestino.normalized;

        // No atravesar la cámara: solo actúa si pasa muy cerca, no huye del jugador
        Vector3 desdeJugador = posicion + desfaseCentro - camara.position;
        float distanciaJugador = desdeJugador.magnitude;
        if (distanciaJugador < espacioPersonal && distanciaJugador > 0.001f)
        {
            float empuje = 1f - distanciaJugador / espacioPersonal;
            direccionDeseada = (direccionDeseada + desdeJugador / distanciaJugador * empuje * 2f).normalized;
        }

        // Girar la dirección de vuelo poco a poco: curvas suaves, sin giros en seco
        Vector3 direccionActual = velocidad.sqrMagnitude > 0.0001f ? velocidad.normalized : direccionDeseada;
        float giroMaximo = velocidadGiro * escalaGiro * Mathf.Deg2Rad * dt;
        Vector3 direccion = Vector3.RotateTowards(direccionActual, direccionDeseada, giroMaximo, 0f);

        // Frenar al tomar curvas cerradas para no quedarse dando vueltas al destino
        float alineacion = (Vector3.Dot(direccionActual, direccionDeseada) + 1f) * 0.5f;
        float rapidezDeseada = velocidadMaxima * Mathf.Lerp(0.35f, 1f, alineacion);

        float aceleracion = velocidadMaxima / Mathf.Max(0.05f, suavizado);
        float rapidez = Mathf.MoveTowards(velocidad.magnitude, rapidezDeseada, aceleracion * dt);

        velocidad = direccion * rapidez;
        posicion += velocidad * dt;
        posicion.y = Mathf.Clamp(posicion.y, alturaBase - 0.5f, alturaBase + 0.5f);

        // Distancia mínima garantizada: si aun así queda demasiado cerca (por ejemplo,
        // porque el jugador camina hacia él), se desliza en horizontal justo hasta el
        // límite, sin saltos. Solo en horizontal para no pelearse con el límite de altura.
        Vector3 separacion = posicion + desfaseCentro - camara.position;
        if (separacion.magnitude < distanciaSegura)
        {
            Vector3 horizontal = new Vector3(separacion.x, 0f, separacion.z);
            float necesaria = Mathf.Sqrt(Mathf.Max(0f, distanciaSegura * distanciaSegura - separacion.y * separacion.y));

            Vector3 salida;
            if (horizontal.sqrMagnitude > 0.000001f)
                salida = horizontal.normalized;
            else
            {
                salida = -camara.forward;
                salida.y = 0f;
                salida = salida.sqrMagnitude > 0.000001f ? salida.normalized : Vector3.forward;
            }

            Vector3 centro = camara.position + salida * necesaria;
            posicion.x = centro.x - desfaseCentro.x;
            posicion.z = centro.z - desfaseCentro.z;
        }

        transform.position = posicion;

        // Mirar hacia el jugador sin inclinarse, girando con suavidad
        Vector3 mirar = camara.position - posicion;
        mirar.y = 0f;
        if (mirar.sqrMagnitude > 0.001f)
        {
            Quaternion objetivo = Quaternion.LookRotation(mirar, Vector3.up) * rotacionBase;
            transform.rotation = Quaternion.Slerp(transform.rotation, objetivo, 1f - Mathf.Exp(-6f * dt));
        }
    }

    /// <summary>
    /// Elige un punto fijo del mundo cerca del jugador y a una distancia de viaje
    /// razonable desde donde está el fantasma. Solo usa la posición del jugador,
    /// nunca hacia dónde mira la cámara.
    /// </summary>
    private void ElegirDestino()
    {
        tiempoTramo = 0f;

        // A veces un acelerón, a veces más tranquilo: así cuesta más predecirlo
        factorTramo = Random.value < 0.3f ? 1.3f : Random.Range(0.75f, 1f);

        // Algunos tramos se cortan antes de llegar: cambios de rumbo inesperados
        tiempoMaximoTramo = Random.value < 0.35f ? Random.Range(1.2f, 2.2f) : 5f;

        Vector3 jugador = camara.position;
        Vector3 actual = transform.position;
        Vector3 candidato = actual;

        for (int intento = 0; intento < 20; intento++)
        {
            float angulo = Random.Range(0f, Mathf.PI * 2f);
            // Destinos siempre fuera de la zona en la que se aparta: así no se pasa el
            // rato esquivando al jugador, solo cuando éste camina hacia él
            float minima = Mathf.Max(distanciaMinima * 0.6f, espacioPersonal + 0.2f);
            float distancia = Random.Range(minima, Mathf.Max(minima + 0.5f, distanciaMaxima));
            candidato = new Vector3(
                jugador.x + Mathf.Cos(angulo) * distancia,
                alturaBase + Random.Range(-0.35f, 0.35f),
                jugador.z + Mathf.Sin(angulo) * distancia);

            // Descartar trayectos que pasen por encima del jugador
            float viaje = Vector3.Distance(candidato, actual);
            if (viaje >= 1f && viaje <= 3f && DistanciaAlTrayecto(actual, candidato, jugador) > espacioPersonal)
                break;
        }

        destino = candidato;
    }

    /// <summary>Calcula la esfera que envuelve todas las partes visibles del modelo.</summary>
    private void MedirModelo()
    {
        Bounds limites = new Bounds(transform.position, Vector3.zero);
        bool hayAlguna = false;

        foreach (Renderer render in GetComponentsInChildren<Renderer>())
        {
            if (!render.enabled)
                continue;

            if (hayAlguna)
                limites.Encapsulate(render.bounds);
            else
                limites = render.bounds;
            hayAlguna = true;
        }

        if (!hayAlguna)
        {
            Collider colisor = GetComponent<Collider>();
            if (colisor != null)
                limites = colisor.bounds;
        }

        desfaseCentro = limites.center - transform.position;

        // Al girar para mirar al jugador, el centro se desplaza un poco en horizontal:
        // se suma ese desplazamiento para cubrir cualquier orientación
        float giro = new Vector2(desfaseCentro.x, desfaseCentro.z).magnitude;
        desfaseCentro.x = 0f;
        desfaseCentro.z = 0f;
        radioModelo = limites.extents.magnitude + giro;
    }

    /// <summary>Distancia horizontal más corta entre el jugador y el tramo recto de a a b.</summary>
    private static float DistanciaAlTrayecto(Vector3 a, Vector3 b, Vector3 jugador)
    {
        Vector2 inicio = new Vector2(a.x, a.z);
        Vector2 fin = new Vector2(b.x, b.z);
        Vector2 punto = new Vector2(jugador.x, jugador.z);
        Vector2 tramo = fin - inicio;
        float t = tramo.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector2.Dot(punto - inicio, tramo) / tramo.sqrMagnitude) : 0f;
        return Vector2.Distance(punto, inicio + tramo * t);
    }
}
