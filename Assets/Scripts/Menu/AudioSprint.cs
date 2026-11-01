using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Audio compartido de menús y preview. Los clics sobreviven al cambio de escena.</summary>
public sealed class AudioSprint : MonoBehaviour
{
    public static AudioSprint Instancia { get; private set; }
    public SonidosSprint sonidos;

    AudioSource interfaz, ambiente, musica, puerta, motor, trueno, pagina, slider;
    ControlFurgoneta furgoneta;
    AvisoEscanear seguimiento;
    Book libro;
    bool preview, detectado, pausado;
    float siguienteTrueno;
    float ultimoTarget = -10f;
    float ultimoSlider = -10f;
    Vector3 ultimoPuntero;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarEstatica() => Instancia = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Inicializar()
    {
        if (Instancia != null) return;
        var clips = Resources.Load<SonidosSprint>("SonidosSprint");
        if (clips == null)
        {
            Debug.LogError("[AudioSprint] Falta Resources/SonidosSprint.");
            return;
        }
        var go = new GameObject("Audio del sprint");
        go.AddComponent<AudioSprint>().sonidos = clips;
    }

    void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;
        DontDestroyOnLoad(gameObject);
        interfaz = CrearFuente("Interfaz");
        ambiente = CrearFuente("Ambiente", true);
        musica = CrearFuente("Musica", true);
        puerta = CrearFuente("Puerta");
        motor = CrearFuente("Motor", true);
        trueno = CrearFuente("Trueno");
        pagina = CrearFuente("Pagina", true);
        slider = CrearFuente("Slider");
        SceneManager.sceneLoaded += EscenaCargada;
    }

    AudioSource CrearFuente(string nombre, bool bucle = false)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        var fuente = go.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0;
        fuente.loop = bucle;
        fuente.ignoreListenerVolume = false;
        return fuente;
    }

    void DesconectarPreview()
    {
        if (furgoneta != null) furgoneta.Reiniciado -= ReiniciarVehiculo;
        if (seguimiento != null) seguimiento.DeteccionCambiada -= CambiarDeteccion;
        furgoneta = null;
        seguimiento = null;
        libro = null;
    }

    void EscenaCargada(Scene escena, LoadSceneMode modo)
    {
        // Las escenas auxiliares aditivas no deben interrumpir el audio de la sección.
        if (modo == LoadSceneMode.Additive) return;
        DesconectarPreview();
        ambiente.Stop(); musica.Stop(); puerta.Stop(); motor.Stop(); trueno.Stop(); pagina.Stop();
        motor.pitch = 1;
        preview = escena.name == "Preview";
        detectado = false;

        foreach (var raiz in escena.GetRootGameObjects())
        {
            foreach (var boton in raiz.GetComponentsInChildren<Button>(true))
                boton.onClick.AddListener(EsBloqueado(escena, boton) ? ReproducirBloqueado : ReproducirBoton);
            if (libro == null) libro = raiz.GetComponentInChildren<Book>(true);
            var menu = raiz.GetComponentInChildren<MenuPrincipal>(true);
            if (menu != null && !menu.juegoDesbloqueado) ConectarBloqueado(menu.botonJuego);
            if (!preview) continue;
            if (furgoneta == null) furgoneta = raiz.GetComponentInChildren<ControlFurgoneta>(true);
            if (seguimiento == null) seguimiento = raiz.GetComponentInChildren<AvisoEscanear>(true);
        }

        if (escena.name == MenuPrincipal.k_EscenaMenu)
        {
            ReproducirBucle(ambiente, sonidos.ambienteMenu, sonidos.volumenAmbienteMenu);
            ReproducirBucle(musica, sonidos.musicaMenu, sonidos.volumenMusica);
            siguienteTrueno = Time.unscaledTime + Random.Range(12f, 22f);
        }
        else if (preview)
        {
            ambiente.clip = sonidos.ambientePreview;
            ambiente.volume = sonidos.volumenAmbientePreview;
            motor.clip = sonidos.motor;
            motor.volume = sonidos.volumenMotor;
            if (furgoneta != null) furgoneta.Reiniciado += ReiniciarVehiculo;
            if (seguimiento != null)
            {
                seguimiento.DeteccionCambiada += CambiarDeteccion;
                CambiarDeteccion(seguimiento.Detectado);
            }
            else CambiarDeteccion(true); // También permite una preview sin RA.
        }
    }

    static void ReproducirBucle(AudioSource fuente, AudioClip clip, float volumen)
    {
        fuente.clip = clip;
        fuente.volume = volumen;
        if (clip != null) fuente.Play();
    }

    public void ReproducirBoton() => interfaz.PlayOneShot(sonidos.boton, sonidos.volumenBotones);

    public void ReproducirFantasma() => interfaz.PlayOneShot(sonidos.fantasma, sonidos.volumenFantasma);

    public void ReproducirBloqueado() => interfaz.PlayOneShot(sonidos.bloqueado, sonidos.volumenBloqueado);

    // En Modelos3D los modelos aún sin desbloquear son botones sin acción asignada.
    static bool EsBloqueado(Scene escena, Button boton) =>
        escena.name == "Modelos3D" && boton.onClick.GetPersistentEventCount() == 0;

    // Un botón no interactuable no lanza onClick: el toque se recoge con un EventTrigger.
    void ConectarBloqueado(Button boton)
    {
        var entrada = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        entrada.callback.AddListener(_ => ReproducirBloqueado());
        boton.gameObject.AddComponent<EventTrigger>().triggers.Add(entrada);
    }

    public void ReproducirSlider(float valor)
    {
        // Un tic cada pocos puntos: arrastrar genera un cambio por cada unidad del slider.
        if (Mathf.Abs(valor - ultimoSlider) < 4f) return;
        ultimoSlider = valor;
        slider.pitch = Mathf.Lerp(.8f, 1.4f, valor / 100f);
        slider.PlayOneShot(sonidos.slider, sonidos.volumenSlider);
    }

    public void ReproducirPuerta(bool abrir)
    {
        puerta.Stop();
        puerta.clip = abrir ? sonidos.puertaAbrir : sonidos.puertaCerrar;
        puerta.volume = sonidos.volumenPuerta;
        if (puerta.clip != null)
        {
            if (abrir) puerta.Play();
            else puerta.PlayDelayed(sonidos.retrasoCierrePuerta);
        }
    }

    public void ConfirmarTarget()
    {
        // Algunos observers emiten varios cambios de estado al reconocer la misma imagen.
        if (Time.unscaledTime - ultimoTarget < 1f || pausado) return;
        ultimoTarget = Time.unscaledTime;
        interfaz.PlayOneShot(sonidos.targetDetectado, sonidos.volumenConfirmacion);
    }

    void ReiniciarVehiculo()
    {
        motor.pitch = 1;
        if (detectado && !pausado)
            interfaz.PlayOneShot(sonidos.reiniciar, sonidos.volumenConfirmacion);
    }

    void CambiarDeteccion(bool valor)
    {
        bool nuevaDeteccion = valor && !detectado;
        detectado = valor;
        if (detectado && !pausado)
        {
            if (!ambiente.isPlaying && ambiente.clip != null) ambiente.Play();
            if (furgoneta != null && !motor.isPlaying && motor.clip != null) motor.Play();
            if (nuevaDeteccion) ConfirmarTarget();
        }
        else
        {
            ambiente.Stop(); motor.Stop();
        }
    }

    void Update()
    {
        if (pausado || sonidos == null) return;
        if (libro != null) ActualizarPagina();
        if (preview && detectado && furgoneta != null)
        {
            float velocidad = furgoneta.VelocidadNormalizada;
            motor.pitch = Mathf.MoveTowards(motor.pitch, Mathf.Lerp(1f, 1.3f, velocidad), Time.unscaledDeltaTime * .6f);
            motor.volume = sonidos.volumenMotor * Mathf.Lerp(.7f, 1f, velocidad);
        }
        if (SceneManager.GetActiveScene().name == MenuPrincipal.k_EscenaMenu && Time.unscaledTime >= siguienteTrueno)
        {
            trueno.PlayOneShot(sonidos.trueno, sonidos.volumenTrueno);
            siguienteTrueno = Time.unscaledTime + Mathf.Max(30f, sonidos.trueno.length + 5f) + Random.Range(0f, 15f);
        }
    }

    // El roce acompaña al gesto: suena al coger la hoja y sube con la velocidad del dedo.
    void ActualizarPagina()
    {
        bool arrastrando = libro.PaginaEnArrastre;
        Vector3 puntero = Input.mousePosition;
        float velocidad = (puntero - ultimoPuntero).magnitude / Mathf.Max(Time.unscaledDeltaTime, .001f);
        ultimoPuntero = puntero;

        if (arrastrando && !pagina.isPlaying)
        {
            pagina.clip = sonidos.paginaLibro;
            pagina.volume = sonidos.volumenPagina * .6f;
            pagina.Play();
        }
        float objetivo = arrastrando ? sonidos.volumenPagina * Mathf.Clamp01(velocidad / 1200f) : 0f;
        pagina.volume = Mathf.MoveTowards(pagina.volume, objetivo, Time.unscaledDeltaTime * 3f);
        if (!arrastrando && pagina.volume <= 0f) pagina.Stop();
    }

    void OnApplicationPause(bool pausa)
    {
        pausado = pausa;
        if (pausa)
        {
            PlayerPrefs.Save();
            foreach (var fuente in GetComponentsInChildren<AudioSource>()) fuente.Pause();
        }
        else
        {
            foreach (var fuente in GetComponentsInChildren<AudioSource>()) fuente.UnPause();
            if (preview) CambiarDeteccion(detectado);
        }
    }

    void OnApplicationQuit() => PlayerPrefs.Save();

    void OnDestroy()
    {
        if (Instancia != this) return;
        SceneManager.sceneLoaded -= EscenaCargada;
        DesconectarPreview();
        Instancia = null;
    }
}
