using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Flujo de la partida: temporizador global, contador de fantasmas cazados y pantalla
/// final con la puntuación y el récord personal. El tiempo empieza al salir el primer
/// fantasma jugable, y la partida termina al acabarse o al cazar todos los fantasmas.
/// </summary>
public class GestorPartida : MonoBehaviour
{
    [Header("Partida")]
    [Tooltip("Duración de la partida en segundos.")]
    [SerializeField] private float duracion = 90f;
    [SerializeField] private CapturaFantasma captura;
    [Tooltip("Un fantasma por secuencia: su número es el total que hay que cazar.")]
    [SerializeField] private SecuenciaFantasma[] secuencias;
    [Tooltip("Espera tras cazar el último para que se vea su desaparición.")]
    [SerializeField] private float esperaUltimaCaptura = 1f;

    [Header("HUD")]
    [Tooltip("Tiempo y contador: aparecen tras el mensaje de inicio.")]
    [SerializeField] private GameObject[] hud;
    [SerializeField] private TMP_Text textoTiempo;
    [SerializeField] private TMP_Text textoContador;
    [Tooltip("Recuadro de escaneo que se muestra mientras no hay ningún fantasma que cazar.")]
    [SerializeField] private GameObject marcoEscaneo;
    [Tooltip("Texto que acompaña al recuadro de escaneo.")]
    [SerializeField] private GameObject textoBusqueda;
    [Tooltip("Mensaje que sale al escanear el primer target.")]
    [SerializeField] private MensajeTemporal mensajeInicio;
    [Tooltip("Mensaje tras cazar un fantasma, si quedan más.")]
    [SerializeField] private MensajeTemporal mensajeSiguiente;
    [Tooltip("Segundos finales en los que el tiempo se pone en rojo.")]
    [SerializeField] private float segundosAviso = 10f;
    [SerializeField] private Color colorTiempo = new Color(1f, 0.71f, 0.278f);
    [SerializeField] private Color colorAviso = new Color(0.9f, 0.15f, 0.15f);

    [Header("Animación del contador")]
    [SerializeField] private RectTransform panelContador;
    [Tooltip("\"+1\" que sube hasta el contador al cazar un fantasma.")]
    [SerializeField] private TMP_Text textoMasUno;
    [SerializeField] private float duracionAnimacion = 0.6f;

    [Header("Interfaz")]
    [Tooltip("Mientras está abierto, la partida se pausa.")]
    [SerializeField] private GameObject panelAjustes;
    [Tooltip("Se ocultan al terminar la partida (mira, indicadores, botón de menú...).")]
    [SerializeField] private GameObject[] ocultarAlTerminar;

    [Header("Fin de partida")]
    [SerializeField] private GameObject panelFin;
    [SerializeField] private TMP_Text textoTituloFin;
    [SerializeField] private TMP_Text textoPuntuacion;
    [SerializeField] private TMP_Text textoRecord;

    private const string ClaveRecord = "recordFantasmas";
    private const string ClaveRecordTiempo = "recordTiempo";

    private float restante;
    private int capturados;
    private bool terminada;
    private bool terminando;
    private bool primerTarget;
    private bool enMarcha;
    private Coroutine animacionContador;

    private int Total => secuencias.Length;

    /// <summary>Si el reloj está en los segundos finales (cuando el tiempo se pone en rojo).</summary>
    public bool PocoTiempo => enMarcha && !terminada && restante <= segundosAviso;

    /// <summary>Si la partida ha terminado (pantalla final).</summary>
    public bool Terminada => terminada;

    /// <summary>Si se han cazado todos los fantasmas.</summary>
    public bool TodosCazados => capturados >= Total;

    void Awake()
    {
        Time.timeScale = 1f;
        restante = duracion;
        panelFin.SetActive(false);
        textoMasUno.gameObject.SetActive(false);
        MostrarHUD(false);
        ActualizarHUD();
    }

    void OnEnable()
    {
        captura.FantasmaCapturado += SumarCaptura;
    }

    void OnDisable()
    {
        captura.FantasmaCapturado -= SumarCaptura;
    }

    void OnDestroy()
    {
        // La pausa no debe pasar a otra escena
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (terminada)
            return;

        // Con el menú o el mensaje de inicio abiertos se para todo (tiempo, animaciones, fantasmas y captura)
        Time.timeScale = panelAjustes.activeInHierarchy || mensajeInicio.Visible ? 0f : 1f;

        bool hayFantasmas = CapturaFantasma.HayFantasmasJugables();

        // Primer target escaneado: mensaje de inicio y, tras él, el HUD
        if (!primerTarget && AlgunTargetEscaneado())
        {
            primerTarget = true;
            mensajeInicio.Mostrar();
        }

        // El tiempo empieza al verse la mira por primera vez
        if (!enMarcha && hayFantasmas)
            enMarcha = true;

        if ((primerTarget && !mensajeInicio.Visible) || enMarcha)
            MostrarHUD(true);

        if (enMarcha && !terminando)
        {
            restante -= Time.deltaTime;
            if (restante <= 0f)
            {
                restante = 0f;
                Terminar();
                return;
            }
        }

        // Recuadro y texto de búsqueda mientras no hay fantasma ni cinemática
        bool buscando = !hayFantasmas && !EnCinematica() && !mensajeInicio.Visible && !mensajeSiguiente.Visible;
        marcoEscaneo.SetActive(buscando);
        textoBusqueda.SetActive(buscando);
        ActualizarHUD();
    }

    private void SumarCaptura(GameObject fantasma)
    {
        if (terminada || terminando)
            return;

        capturados++;
        ActualizarHUD();

        if (animacionContador != null)
            StopCoroutine(animacionContador);
        animacionContador = StartCoroutine(AnimarContador());

        if (capturados >= Total)
            StartCoroutine(TerminarTrasCaptura());
        else
            mensajeSiguiente.Mostrar();
    }

    private IEnumerator TerminarTrasCaptura()
    {
        terminando = true;
        yield return new WaitForSeconds(esperaUltimaCaptura);
        Terminar();
    }

    private bool AlgunTargetEscaneado()
    {
        foreach (SecuenciaFantasma secuencia in secuencias)
            if (secuencia.Iniciada)
                return true;
        return false;
    }

    /// <summary>Un target escaneado cuyo fantasma aún no ha salido: se está viendo su cinemática.</summary>
    private bool EnCinematica()
    {
        foreach (SecuenciaFantasma secuencia in secuencias)
            if (secuencia.Iniciada && secuencia.ObjetivoActual == null)
                return true;
        return false;
    }

    private void MostrarHUD(bool mostrar)
    {
        foreach (GameObject elemento in hud)
            if (elemento.activeSelf != mostrar)
                elemento.SetActive(mostrar);
    }

    /// <summary>El contador da un salto y un "+1" sube hasta él (solo en el HUD).</summary>
    private IEnumerator AnimarContador()
    {
        RectTransform masUno = textoMasUno.rectTransform;
        Vector2 inicio = new Vector2(0f, -110f);
        Vector2 fin = new Vector2(0f, -20f);
        textoMasUno.gameObject.SetActive(true);

        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duracionAnimacion)
        {
            float salto = Mathf.Sin(t * Mathf.PI);
            panelContador.localScale = Vector3.one * (1f + 0.3f * salto);
            textoContador.color = Color.Lerp(colorTiempo, Color.white, salto);

            masUno.anchoredPosition = Vector2.Lerp(inicio, fin, Mathf.SmoothStep(0f, 1f, t));
            textoMasUno.alpha = 1f - t * t;
            yield return null;
        }

        panelContador.localScale = Vector3.one;
        textoContador.color = colorTiempo;
        textoMasUno.gameObject.SetActive(false);
        animacionContador = null;
    }

    private void ActualizarHUD()
    {
        textoTiempo.text = FormatoTiempo(restante);
        textoTiempo.color = restante <= segundosAviso ? colorAviso : colorTiempo;
        textoContador.text = capturados + "/" + Total;
    }

    private void Terminar()
    {
        terminada = true;
        Time.timeScale = 0f;

        captura.enabled = false;
        marcoEscaneo.SetActive(false);
        textoBusqueda.SetActive(false);
        mensajeInicio.Ocultar();
        mensajeSiguiente.Ocultar();
        panelAjustes.SetActive(false);
        foreach (GameObject objeto in ocultarAlTerminar)
            if (objeto != null)
                objeto.SetActive(false);

        bool todos = capturados >= Total;
        float tiempoUsado = duracion - restante;
        bool nuevoRecord = GuardarRecord(tiempoUsado);

        textoTituloFin.text = todos ? "¡Todos cazados!" : "¡Se acabó el tiempo!";
        textoPuntuacion.text = "Fantasmas cazados: " + capturados + "/" + Total +
                               (todos ? "\nTiempo: " + FormatoTiempo(tiempoUsado) : "");
        textoRecord.text = nuevoRecord ? "¡Nuevo récord personal!" : "Récord personal: " + TextoRecord();

        panelFin.SetActive(true);
    }

    /// <summary>
    /// Gana quien caza más fantasmas; si se iguala habiéndolos cazado todos, el que tarda menos.
    /// Devuelve true si es un récord nuevo.
    /// </summary>
    private bool GuardarRecord(float tiempoUsado)
    {
        int record = PlayerPrefs.GetInt(ClaveRecord, 0);
        float recordTiempo = PlayerPrefs.GetFloat(ClaveRecordTiempo, float.MaxValue);

        bool mejor = capturados > record ||
                     (capturados == record && capturados >= Total && tiempoUsado < recordTiempo);
        if (!mejor || capturados == 0)
            return false;

        PlayerPrefs.SetInt(ClaveRecord, capturados);
        PlayerPrefs.SetFloat(ClaveRecordTiempo, capturados >= Total ? tiempoUsado : float.MaxValue);
        PlayerPrefs.Save();
        return true;
    }

    private string TextoRecord()
    {
        int record = PlayerPrefs.GetInt(ClaveRecord, 0);
        float recordTiempo = PlayerPrefs.GetFloat(ClaveRecordTiempo, float.MaxValue);
        string texto = record + "/" + Total;
        if (record >= Total && recordTiempo < float.MaxValue)
            texto += " en " + FormatoTiempo(recordTiempo);
        return texto;
    }

    private static string FormatoTiempo(float segundos)
    {
        int total = Mathf.CeilToInt(Mathf.Max(0f, segundos));
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }

    /// <summary>Vuelve a empezar la partida desde cero (botón de reiniciar).</summary>
    public void Reiniciar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>Cierra la aplicación (botón de salir de la pantalla final).</summary>
    public void Salir()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
