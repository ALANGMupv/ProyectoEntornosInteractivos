using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Flujo de la partida: temporizador global, contador de fantasmas cazados y pantalla
/// final con la puntuación y el récord personal. La partida termina al acabarse el
/// tiempo o al cazar todos los fantasmas de la escena.
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
    [SerializeField] private TMP_Text textoTiempo;
    [SerializeField] private TMP_Text textoContador;
    [Tooltip("Recuadro de escaneo que se muestra mientras no hay ningún fantasma que cazar.")]
    [SerializeField] private GameObject marcoEscaneo;
    [Tooltip("Segundos finales en los que el tiempo se pone en rojo.")]
    [SerializeField] private float segundosAviso = 10f;
    [SerializeField] private Color colorTiempo = new Color(1f, 0.71f, 0.278f);
    [SerializeField] private Color colorAviso = new Color(0.9f, 0.15f, 0.15f);

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

    private int Total => secuencias.Length;

    void Awake()
    {
        Time.timeScale = 1f;
        restante = duracion;
        panelFin.SetActive(false);
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

        // Con el menú abierto se para todo (tiempo, fantasmas y captura)
        Time.timeScale = panelAjustes.activeInHierarchy ? 0f : 1f;

        if (!terminando)
        {
            restante -= Time.deltaTime;
            if (restante <= 0f)
            {
                restante = 0f;
                Terminar();
            }
        }

        marcoEscaneo.SetActive(!terminada && !CapturaFantasma.HayFantasmasJugables());
        ActualizarHUD();
    }

    private void SumarCaptura(GameObject fantasma)
    {
        if (terminada || terminando)
            return;

        capturados++;
        ActualizarHUD();

        if (capturados >= Total)
            StartCoroutine(TerminarTrasCaptura());
    }

    private IEnumerator TerminarTrasCaptura()
    {
        terminando = true;
        yield return new WaitForSeconds(esperaUltimaCaptura);
        Terminar();
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
