using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sonido de la partida: música de fondo que acelera según avanza el juego y efectos de
/// aparición, persecución, captura y tiempo. Los volúmenes de todo se ajustan aquí desde
/// el Inspector; además, todo respeta el volumen general del menú de ajustes.
/// A cada fantasma jugable le añade su voz con sonido por proximidad (AudioFantasma).
/// </summary>
public class AudioPartida : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GestorPartida gestor;
    [SerializeField] private CapturaFantasma captura;
    [SerializeField] private SecuenciaFantasma[] secuencias;

    [Header("Música de fondo")]
    public AudioClip musica;
    [Range(0f, 1f)] public float volumenMusica = 0.075f;
    [Tooltip("Mientras se buscan los targets (unos 100 BPM).")]
    public float pitchBusqueda = 0.88f;
    [Tooltip("Durante la persecución (114 BPM, la velocidad original).")]
    public float pitchPersecucion = 1f;
    [Tooltip("Tras el aviso de poco tiempo (unos 131 BPM).")]
    public float pitchPocoTiempo = 1.15f;
    [Tooltip("Cuánto cambia el pitch por segundo: más bajo, transición más suave.")]
    public float velocidadCambioPitch = 0.08f;
    [Tooltip("La música baja a esta proporción mientras suenan la aparición, el aviso de poco tiempo o el final.")]
    [Range(0f, 1f)] public float atenuacionMusica = 0.4f;

    [Header("Efectos (suenan igual en todas partes)")]
    public AudioClip sonidoAparicion;
    [Range(0f, 1f)] public float volumenAparicion = 0.8f;
    public AudioClip comienzaAtrapar;
    [Range(0f, 1f)] public float volumenComienzaAtrapar = 0.6f;
    public AudioClip mitadCirculo;
    [Range(0f, 1f)] public float volumenMitadCirculo = 0.3f;
    public AudioClip sonidoCaptura;
    [Range(0f, 1f)] public float volumenCaptura = 0.75f;
    public AudioClip avisoPocoTiempo;
    [Range(0f, 1f)] public float volumenAvisoPocoTiempo = 0.8f;
    public AudioClip finalTiempo;
    [Range(0f, 1f)] public float volumenFinalTiempo = 0.4f;

    [Header("Voz del fantasma (suena desde él)")]
    public AudioClip audioFantasma;
    [Tooltip("Este audio es mucho más fuerte que los demás: por eso su volumen es tan bajo.")]
    [Range(0f, 1f)] public float volumenAudioFantasma = 0.07f;
    public AudioClip pedorretaBurlona;
    [Range(0f, 1f)] public float volumenPedorreta = 0.45f;
    public AudioClip risaBurlona;
    [Range(0f, 1f)] public float volumenRisa = 0.15f;
    [Tooltip("Pausa aleatoria (mín, máx) tras cada sonido si el fantasma está fuera de cámara.")]
    public Vector2 pausaFueraDeCamara = new Vector2(2f, 4f);
    [Tooltip("Pausa aleatoria (mín, máx) tras cada sonido si el fantasma se está viendo.")]
    public Vector2 pausaEnCamara = new Vector2(4f, 7f);
    [Tooltip("Probabilidad de que el siguiente sonido sea una pedorreta en vez del audio normal.")]
    [Range(0f, 1f)] public float probabilidadPedorreta = 0.3f;
    [Tooltip("Segundos mínimos entre dos pedorretas del mismo fantasma.")]
    public float separacionPedorretas = 12f;

    [Header("Proximidad")]
    [Tooltip("A esta distancia o menos, la voz del fantasma suena a su volumen completo.")]
    public float distanciaCerca = 1.2f;
    [Tooltip("A esta distancia o más, suena a volumenLejos.")]
    public float distanciaLejos = 5f;
    [Range(0f, 1f)] public float volumenLejos = 0.25f;

    [Header("Risa al escaparse de la mira")]
    [Tooltip("Progreso de captura mínimo (0-1) para que se ría al escaparse: haber estado un rato dentro.")]
    [Range(0f, 1f)] public float progresoMinimoRisa = 0.3f;
    public float separacionRisas = 5f;

    private AudioSource fuenteMusica;
    private AudioSource fuenteEfectos;

    private readonly HashSet<SecuenciaFantasma> aparecidas = new HashSet<SecuenciaFantasma>();
    private readonly HashSet<SecuenciaFantasma> huidas = new HashSet<SecuenciaFantasma>();
    private readonly List<AudioFantasma> voces = new List<AudioFantasma>();

    private bool persecucion;
    private bool pocoTiempo;
    private bool terminada;
    private bool mitadArmada = true;
    private bool apuntandoAntes;
    private float progresoAntes;
    private float ultimaRisa = -100f;
    private float finAtenuacion;

    void Awake()
    {
        fuenteMusica = CrearFuente("Musica", true);
        fuenteEfectos = CrearFuente("Efectos", false);
    }

    void Start()
    {
        if (musica != null)
        {
            fuenteMusica.clip = musica;
            fuenteMusica.pitch = pitchBusqueda;
            fuenteMusica.volume = volumenMusica;
            fuenteMusica.Play();
        }
    }

    void OnEnable()
    {
        if (captura != null)
            captura.FantasmaCapturado += AlCapturar;
    }

    void OnDisable()
    {
        if (captura != null)
            captura.FantasmaCapturado -= AlCapturar;
    }

    private AudioSource CrearFuente(string nombre, bool bucle)
    {
        GameObject objeto = new GameObject(nombre);
        objeto.transform.SetParent(transform, false);
        AudioSource fuente = objeto.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = bucle;
        fuente.spatialBlend = 0f;
        return fuente;
    }

    void Update()
    {
        ActualizarMusica();

        if (terminada)
            return;

        RevisarSecuencias();
        RevisarCaptura();
        RevisarTiempo();
    }

    private void ActualizarMusica()
    {
        if (fuenteMusica.clip == null)
            return;

        // El pitch cambia poco a poco y sin reiniciar la pista
        float objetivo = pocoTiempo ? pitchPocoTiempo : persecucion ? pitchPersecucion : pitchBusqueda;
        fuenteMusica.pitch = Mathf.MoveTowards(fuenteMusica.pitch, objetivo, velocidadCambioPitch * Time.unscaledDeltaTime);

        // Al terminar se apaga; mientras suena algo importante, baja un poco
        float volumen = terminada ? 0f : volumenMusica * (Time.unscaledTime < finAtenuacion ? atenuacionMusica : 1f);
        fuenteMusica.volume = Mathf.MoveTowards(fuenteMusica.volume, volumen, Time.unscaledDeltaTime * volumenMusica * 1.5f);
    }

    /// <summary>Aparición del fantasma al escanear, salida disparada y voz del fantasma jugable.</summary>
    private void RevisarSecuencias()
    {
        foreach (SecuenciaFantasma secuencia in secuencias)
        {
            if (secuencia == null)
                continue;

            if (secuencia.Iniciada && aparecidas.Add(secuencia))
                Efecto(sonidoAparicion, volumenAparicion, true);

            if (secuencia.Huyendo && huidas.Add(secuencia))
            {
                persecucion = true;
                Efecto(comienzaAtrapar, volumenComienzaAtrapar, false);
            }

            Transform fantasma = secuencia.ObjetivoActual;
            if (fantasma != null && fantasma.GetComponent<AudioFantasma>() == null)
            {
                AudioFantasma voz = fantasma.gameObject.AddComponent<AudioFantasma>();
                voz.Configurar(this);
                voces.Add(voz);
            }
        }
    }

    /// <summary>Sonido a mitad del círculo y risa si el fantasma se escapa de la mira.</summary>
    private void RevisarCaptura()
    {
        if (captura == null)
            return;

        float progreso = captura.Progreso;

        if (mitadArmada && progreso >= 0.5f && progresoAntes < 0.5f)
        {
            mitadArmada = false;
            Efecto(mitadCirculo, volumenMitadCirculo, false);
        }
        // Se vuelve a poder oír cuando el círculo se ha vaciado bastante
        if (progreso < 0.25f)
            mitadArmada = true;

        bool apuntando = captura.ApuntandoFantasma;
        if (apuntandoAntes && !apuntando && progreso >= progresoMinimoRisa &&
            Time.time - ultimaRisa >= separacionRisas && captura.Objetivo != null)
        {
            AudioFantasma voz = captura.Objetivo.GetComponent<AudioFantasma>();
            if (voz != null)
            {
                ultimaRisa = Time.time;
                voz.Reirse();
            }
        }

        apuntandoAntes = apuntando;
        progresoAntes = progreso;
    }

    private void RevisarTiempo()
    {
        if (gestor == null)
            return;

        if (!pocoTiempo && gestor.PocoTiempo)
        {
            pocoTiempo = true;
            Efecto(avisoPocoTiempo, volumenAvisoPocoTiempo, true);
        }

        if (gestor.Terminada)
        {
            terminada = true;
            foreach (AudioFantasma voz in voces)
                if (voz != null)
                    voz.Callar();

            // Perder por tiempo; si se han cazado todos ya ha sonado la captura
            if (!gestor.TodosCazados)
            {
                fuenteEfectos.Stop();
                Efecto(finalTiempo, volumenFinalTiempo, true);
            }
        }
    }

    private void AlCapturar(GameObject fantasma)
    {
        AudioFantasma voz = fantasma.GetComponent<AudioFantasma>();
        if (voz != null)
            voz.Callar();

        Efecto(sonidoCaptura, volumenCaptura, false);
        mitadArmada = true;
    }

    private void Efecto(AudioClip clip, float volumen, bool atenuarMusica)
    {
        if (clip == null)
            return;

        fuenteEfectos.PlayOneShot(clip, volumen);
        if (atenuarMusica)
            finAtenuacion = Mathf.Max(finAtenuacion, Time.unscaledTime + clip.length);
    }
}
