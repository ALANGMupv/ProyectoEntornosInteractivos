using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Mecánica de captura: un rayo invisible sale del centro de la pantalla. Mientras toca a un
/// fantasma jugable (el que tiene MovimientoFantasmaB) se llena un temporizador; si se deja de
/// apuntar, se vacía rápido. Al completarse, el fantasma se encoge y desaparece, vibra el móvil
/// y se lanza el evento de captura para que otros sistemas (contador, puntuación...) lo usen.
/// </summary>
public class CapturaFantasma : MonoBehaviour
{
    [Header("Rayo")]
    [Tooltip("Cámara de la que sale el rayo. Si se deja vacía usa Camera.main (la ARCamera).")]
    [SerializeField] private Camera camaraRA;
    [Tooltip("Distancia máxima a la que se puede capturar, en metros.")]
    [SerializeField] private float distanciaMaxima = 6f;
    [Tooltip("Grosor del rayo en metros. 0 = Raycast exacto; un poco más perdona la puntería en el móvil.")]
    [SerializeField] private float radioTolerancia = 0.03f;
    [Tooltip("En el editor, el rayo sale del ratón en lugar del centro (para probar sin mover el móvil).")]
    [SerializeField] private bool usarRatonEnEditor = true;

    [Header("Temporizador")]
    [Tooltip("Segundos que hay que mantener el punto central sobre el fantasma.")]
    [SerializeField] private float tiempoCaptura = 2.5f;
    [Tooltip("Velocidad de vaciado al dejar de apuntar respecto a la de llenado (1 = igual de rápido).")]
    [SerializeField] private float velocidadVaciado = 0.8f;
    [Tooltip("Si está activo, al dejar de apuntar el progreso vuelve a 0 de golpe.")]
    [SerializeField] private bool reiniciarAlFallar = false;

    [Header("Captura")]
    [SerializeField] private MiraCaptura mira;
    [SerializeField] private float duracionDesaparicion = 0.4f;
    [SerializeField] private bool vibrarAlCapturar = true;
    [Tooltip("Se lanza con el fantasma capturado (para el contador, la puntuación...).")]
    public UnityEvent<GameObject> alCapturar;

    /// <summary>Se lanza con el fantasma capturado (para suscribirse desde código).</summary>
    public event System.Action<GameObject> FantasmaCapturado;

    /// <summary>Progreso de la captura actual, de 0 a 1.</summary>
    public float Progreso { get; private set; }

    /// <summary>Si el punto central está ahora mismo sobre un fantasma jugable.</summary>
    public bool ApuntandoFantasma { get; private set; }

    private MovimientoFantasmaB objetivo;
    private readonly HashSet<MovimientoFantasmaB> capturando = new HashSet<MovimientoFantasmaB>();

    void Awake()
    {
        if (camaraRA == null)
            camaraRA = Camera.main;

        // La mira no sale hasta que haya un fantasma jugable que cazar
        if (mira != null)
            mira.gameObject.SetActive(false);
    }

    void Update()
    {
        if (camaraRA == null)
        {
            camaraRA = Camera.main;
            if (camaraRA == null)
                return;
        }

        // Mostrar la mira solo mientras quede algún fantasma jugable en la escena
        bool hayFantasmas = HayFantasmasJugables();
        if (mira != null && mira.gameObject.activeSelf != hayFantasmas)
            mira.gameObject.SetActive(hayFantasmas);
        if (!hayFantasmas)
        {
            Progreso = 0f;
            objetivo = null;
            ApuntandoFantasma = false;
            return;
        }

        MovimientoFantasmaB apuntado = BuscarFantasma();
        ApuntandoFantasma = apuntado != null;

        if (apuntado != null)
        {
            // Si se cambia de fantasma, la captura empieza de cero
            if (apuntado != objetivo)
            {
                objetivo = apuntado;
                Progreso = 0f;
            }

            Progreso = Mathf.MoveTowards(Progreso, 1f, Time.deltaTime / tiempoCaptura);
            if (Progreso >= 1f)
                Capturar(objetivo);
        }
        else
        {
            Progreso = reiniciarAlFallar
                ? 0f
                : Mathf.MoveTowards(Progreso, 0f, Time.deltaTime * velocidadVaciado / tiempoCaptura);
            if (Progreso <= 0f)
                objetivo = null;
        }

        if (mira != null)
            mira.Actualizar(Progreso, ApuntandoFantasma);
    }

    /// <summary>
    /// Si hay algún fantasma jugable activo. El que se está capturando cuenta hasta que
    /// termina de desaparecer, para que se vea el pulso de la mira.
    /// </summary>
    private static bool HayFantasmasJugables()
    {
        return Object.FindAnyObjectByType<MovimientoFantasmaB>() != null;
    }

    /// <summary>Lanza el rayo y devuelve el fantasma jugable que toca, o null.</summary>
    private MovimientoFantasmaB BuscarFantasma()
    {
        // El fantasma se mueve por transform y Vuforia mueve la cámara: actualizar los colliders
        Physics.SyncTransforms();

        Ray rayo = camaraRA.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
#if UNITY_EDITOR
        if (usarRatonEnEditor && Mouse.current != null)
            rayo = camaraRA.ScreenPointToRay(Mouse.current.position.ReadValue());
#endif

        bool tocado = radioTolerancia > 0f
            ? Physics.SphereCast(rayo, radioTolerancia, out RaycastHit impacto, distanciaMaxima, ~0, QueryTriggerInteraction.Collide)
            : Physics.Raycast(rayo, out impacto, distanciaMaxima, ~0, QueryTriggerInteraction.Collide);
        if (!tocado)
            return null;

        // Solo cuenta el fantasma jugable: el de la cinemática y el resto de objetos se ignoran
        MovimientoFantasmaB fantasma = impacto.collider.GetComponentInParent<MovimientoFantasmaB>();
        if (fantasma == null || capturando.Contains(fantasma))
            return null;
        return fantasma;
    }

    private void Capturar(MovimientoFantasmaB fantasma)
    {
        capturando.Add(fantasma);
        objetivo = null;
        Progreso = 0f;

        if (mira != null)
            mira.Completar();
        if (vibrarAlCapturar)
            Vibrar();

        Debug.Log("[CapturaFantasma] Capturado: " + fantasma.name);
        alCapturar?.Invoke(fantasma.gameObject);
        FantasmaCapturado?.Invoke(fantasma.gameObject);

        StartCoroutine(Desaparecer(fantasma));
    }

    private IEnumerator Desaparecer(MovimientoFantasmaB fantasma)
    {
        // Que deje de moverse y se encoja girando, como si lo absorbiera el aparato
        fantasma.enabled = false;
        foreach (Collider collider in fantasma.GetComponentsInChildren<Collider>())
            collider.enabled = false;

        Transform t = fantasma.transform;
        Vector3 escalaInicial = t.localScale;
        for (float tiempo = 0f; tiempo < duracionDesaparicion; tiempo += Time.deltaTime)
        {
            float k = 1f - tiempo / duracionDesaparicion;
            t.localScale = escalaInicial * (k * k);
            t.Rotate(0f, 720f * Time.deltaTime, 0f, Space.Self);
            yield return null;
        }

        fantasma.gameObject.SetActive(false);
        capturando.Remove(fantasma);
    }

    private static void Vibrar()
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }
}
