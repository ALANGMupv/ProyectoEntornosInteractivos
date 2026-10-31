using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using Vuforia;

/// <summary>Reproductor del tráiler sobre su imagen target; el HUD permanece accesible sin seguimiento.</summary>
[DefaultExecutionOrder(-100)]
public class TrailerRA : MonoBehaviour
{
    public VideoPlayer reproductor;
    public AudioSource audioVideo;
    public Texture2D imagenTarget;
    public float anchoTargetMetros = 0.15f;
    [Range(0.3f, 1.6f), Tooltip("Ancho físico del panel respecto al target. Se mantiene fijo al mover la cámara.")]
    public float anchoPanelRespectoTarget = 0.8f;
    public Canvas pantallaRA;
    public GameObject avisoEscanear;
    public TMP_Text estado;
    public TMP_Text tiempo;
    public Button botonReproducir;
    public Button botonSilenciar;
    public Slider progreso;
    public VuforiaBehaviour camaraRA;
    [Tooltip("Solo en el editor: permite probar los controles sin imprimir el target.")]
    public bool probarSinTarget;

    ImageTargetBehaviour target;
    Texture2D copiaTarget;
    bool preparandoPrimeraImagen;
    bool silencioAntesDePreparar;
    bool visible, quiereReproducir, terminado, buscando, errorVideo;
    double instanteBusqueda;
    bool listo => reproductor != null && reproductor.isPrepared && !errorVideo && !preparandoPrimeraImagen;
    public bool ListoParaReproducir => listo;

    void Awake()
    {
#if UNITY_EDITOR
        if (probarSinTarget && camaraRA != null) camaraRA.enabled = false;
#endif
        reproductor.playOnAwake = false;
        reproductor.isLooping = false;
        reproductor.audioOutputMode = VideoAudioOutputMode.AudioSource;
        reproductor.controlledAudioTrackCount = 1;
        reproductor.EnableAudioTrack(0, true);
        reproductor.SetTargetAudioSource(0, audioVideo);
        reproductor.prepareCompleted += Preparado;
        reproductor.loopPointReached += Finalizado;
        reproductor.errorReceived += ErrorVideo;
        reproductor.seekCompleted += BusquedaTerminada;
        botonReproducir.onClick.AddListener(ReproducirPausar);
        botonSilenciar.onClick.AddListener(Silenciar);
        progreso.onValueChanged.AddListener(Buscar);
        AplicarVisibilidad(false);
        ActualizarControles();
    }

    void Start()
    {
        reproductor.Prepare();
#if UNITY_EDITOR
        if (probarSinTarget)
        {
            pantallaRA.renderMode = RenderMode.ScreenSpaceCamera;
            pantallaRA.worldCamera = camaraRA.GetComponent<Camera>();
            pantallaRA.planeDistance = 1;
            var scaler = pantallaRA.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            AplicarVisibilidad(true);
            estado.text = "PRUEBA EN EDITOR · Sin seguimiento RA";
            return;
        }
#endif
        VuforiaApplication.Instance.OnVuforiaStarted += CrearTarget;
        VuforiaApplication.Instance.OnVuforiaInitialized += Inicializado;
        if (VuforiaApplication.Instance.IsRunning) CrearTarget();
    }

    void Inicializado(VuforiaInitError error)
    {
        if (error != VuforiaInitError.NONE)
            estado.text = "No se pudo iniciar la cámara RA. Comprueba Vuforia y los permisos de cámara.";
    }

    void CrearTarget()
    {
        if (target != null) return;
        try
        {
            // Copia legible y <= 2048 px, sin modificar la importación de la imagen original.
            float factor = Mathf.Min(1f, 2048f / Mathf.Max(imagenTarget.width, imagenTarget.height));
            int w = Mathf.Max(1, Mathf.RoundToInt(imagenTarget.width * factor));
            int h = Mathf.Max(1, Mathf.RoundToInt(imagenTarget.height * factor));
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            var anterior = RenderTexture.active;
            try
            {
                Graphics.Blit(imagenTarget, rt);
                RenderTexture.active = rt;
                copiaTarget = new Texture2D(w, h, TextureFormat.RGB24, false);
                copiaTarget.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                copiaTarget.Apply();
            }
            finally { RenderTexture.active = anterior; RenderTexture.ReleaseTemporary(rt); }
            target = camaraRA.ObserverFactory.CreateImageTarget(copiaTarget, anchoTargetMetros, "Target_Trailer");
            if (target == null) throw new InvalidOperationException("No se ha creado el target del tráiler.");
            pantallaRA.transform.SetParent(target.transform, false);
            pantallaRA.transform.localPosition = new Vector3(0, 0.002f, 0);
            pantallaRA.transform.localRotation = Quaternion.Euler(90, 0, 0);
            AplicarEscalaDelPanel();
            target.OnTargetStatusChanged += CambioTarget;
            CambioTarget(target, target.TargetStatus);
        }
        catch (Exception ex)
        {
            estado.text = "No se pudo cargar el target. Vuelve al menú e inténtalo de nuevo.";
            Debug.LogError("[TrailerRA] " + ex);
        }
    }

    // El tamaño pertenece al target, no a la posición de la cámara. Evita cambios de escala al moverla.
    void AplicarEscalaDelPanel()
    {
        var rect = pantallaRA.GetComponent<RectTransform>();
        float escalaPadre = rect.parent != null ? Mathf.Abs(rect.parent.lossyScale.x) : 1f;
        rect.localScale = Vector3.one * (anchoTargetMetros * anchoPanelRespectoTarget / (1600f * Mathf.Max(escalaPadre, 0.0001f)));
    }
    void CambioTarget(ObserverBehaviour _, TargetStatus seguimiento)
    {
        // Exigir que la imagen esté visible evita seguir reproduciendo al apartar la cámara.
        AplicarVisibilidad(seguimiento.Status == Status.TRACKED);
    }

    void AplicarVisibilidad(bool detectado)
    {
        visible = detectado;
        pantallaRA.gameObject.SetActive(detectado);
        avisoEscanear.SetActive(!detectado);
        estado.transform.parent.gameObject.SetActive(!detectado || errorVideo);
        if (!detectado)
        {
            if (reproductor.isPlaying && !preparandoPrimeraImagen) reproductor.Pause();
            estado.text = "Escanea el target del tráiler para continuar";
        }
        else
        {
            estado.text = "Target detectado · Pulsa Reproducir";
            if (quiereReproducir && listo) reproductor.Play();
        }
        ActualizarControles();
    }

    void Preparado(VideoPlayer _)
    {
        // Prepare por sí solo no dibuja el vídeo. Decodificar un fotograma en silencio y pausarlo.
        preparandoPrimeraImagen = true;
        silencioAntesDePreparar = audioVideo.mute;
        audioVideo.mute = true;
        reproductor.sendFrameReadyEvents = true;
        reproductor.frameReady += PrimeraImagen;
        reproductor.Play();
        ActualizarControles();
    }

    void PrimeraImagen(VideoPlayer _, long fotograma)
    {
        reproductor.frameReady -= PrimeraImagen;
        reproductor.sendFrameReadyEvents = false;
        preparandoPrimeraImagen = false;
        audioVideo.mute = silencioAntesDePreparar;
        if (!visible || !quiereReproducir) reproductor.Pause();
        ActualizarControles();
    }
    public void ReproducirPausar()
    {
        if (!visible || !listo) return;
        if (terminado) { reproductor.time = 0; terminado = false; }
        quiereReproducir = !quiereReproducir;
        if (quiereReproducir) reproductor.Play(); else reproductor.Pause();
        ActualizarControles();
    }

    public void Silenciar()
    {
        audioVideo.mute = !audioVideo.mute;
        ActualizarControles();
    }

    void Buscar(float valor)
    {
        if (!listo || !visible || !reproductor.canSetTime || reproductor.length <= 0) return;
        terminado = false;
        buscando = true;
        instanteBusqueda = Time.realtimeSinceStartupAsDouble;
        reproductor.time = Math.Min(valor * reproductor.length, Math.Max(0, reproductor.length - 0.1));
    }

    void BusquedaTerminada(VideoPlayer _) { buscando = false; }

    void Finalizado(VideoPlayer _)
    {
        terminado = true;
        quiereReproducir = false;
        progreso.SetValueWithoutNotify(1);
        ActualizarControles();
    }

    void ErrorVideo(VideoPlayer _, string mensaje)
    {
        errorVideo = true;
        estado.transform.parent.gameObject.SetActive(true);
        quiereReproducir = false;
        reproductor.Pause();
        estado.text = "No se pudo reproducir el vídeo. Vuelve al menú e inténtalo de nuevo.";
        Debug.LogError("[TrailerRA] " + mensaje);
        ActualizarControles();
    }

    void Update()
    {
        if (!listo) return;
        if (buscando && Time.realtimeSinceStartupAsDouble - instanteBusqueda > 2) buscando = false;
        if (!buscando && reproductor.length > 0)
            progreso.SetValueWithoutNotify(terminado ? 1 : (float)(reproductor.time / reproductor.length));
        tiempo.text = FormatoTiempo(terminado ? reproductor.length : reproductor.time) + " / " + FormatoTiempo(reproductor.length);
    }

    static string FormatoTiempo(double segundos)
    {
        int s = Mathf.Max(0, (int)segundos);
        return $"{s / 60:00}:{s % 60:00}";
    }

    void ActualizarControles()
    {
        botonReproducir.interactable = visible && listo;
        progreso.interactable = visible && listo && reproductor.canSetTime;
        botonReproducir.GetComponentInChildren<TMP_Text>().text = terminado ? "Repetir" : quiereReproducir ? "Pausa" : "Reproducir";
        botonSilenciar.GetComponentInChildren<TMP_Text>().text = audioVideo.mute ? "Activar sonido" : "Silenciar";
    }

    void OnApplicationPause(bool pausa)
    {
        if (!pausa) return;
        quiereReproducir = false;
        reproductor.Pause();
        ActualizarControles();
    }

    public void VolverAlMenu()
    {
        quiereReproducir = false;
        reproductor.Stop();
        audioVideo.Stop();
        SceneManager.LoadScene(MenuPrincipal.k_EscenaMenu);
    }

    void OnDestroy()
    {
        VuforiaApplication.Instance.OnVuforiaStarted -= CrearTarget;
        VuforiaApplication.Instance.OnVuforiaInitialized -= Inicializado;
        if (target != null) target.OnTargetStatusChanged -= CambioTarget;
        if (copiaTarget != null) Destroy(copiaTarget);
        if (reproductor == null) return;
        reproductor.prepareCompleted -= Preparado;
        reproductor.loopPointReached -= Finalizado;
        reproductor.errorReceived -= ErrorVideo;
        reproductor.seekCompleted -= BusquedaTerminada;
        reproductor.frameReady -= PrimeraImagen;
    }
}




