using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;
using Vuforia;
using Image = UnityEngine.UI.Image;

/// <summary>Construcción reproducible de la escena de T013. No modifica las otras escenas.</summary>
[InitializeOnLoad]
public static class PrepararTrailer
{
    const string Solicitud = "Temp/TrailerTrabajo/construir";
    static readonly Color Tinta = new Color(0.2901961f, 0f, 0.0627451f);
    static readonly Color Papel = new Color(0.97f, 0.94f, 0.88f);
    static readonly Color TextoOscuro = new Color(0.11764706f, 0.09803922f, 0.078431375f);
    static TMP_FontAsset fuente;
    static PrepararTrailer() { EditorApplication.update += RevisarSolicitud; }
    static void RevisarSolicitud()
    {
        if (File.Exists("Temp/TrailerTrabajo/estilo") && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            try { File.Delete("Temp/TrailerTrabajo/estilo"); AplicarEstiloCompartido(); File.WriteAllText("Temp/TrailerTrabajo/estilo.txt", "OK " + DateTime.Now); }
            catch (Exception ex) { Debug.LogException(ex); File.WriteAllText("Temp/TrailerTrabajo/estilo-error.txt", ex.ToString()); }
            return;
        }
        if (!File.Exists(Solicitud) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        try { File.Delete(Solicitud); } catch (IOException) { return; }
        try { Construir(); File.WriteAllText("Temp/TrailerTrabajo/construido.txt", "OK " + DateTime.Now); }
        catch (Exception e) { File.WriteAllText("Temp/TrailerTrabajo/error.txt", e.ToString()); Debug.LogException(e); }
    }

    [MenuItem("Proyecto/Trailer/Reconstruir escena")]
    public static void Construir()
    {
        var clip = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Videos/trailer_pick_a_boo.mp4");
        var textura = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Texturas/Targets/target_trailer.jpg");
        var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Videos/RT_Trailer.renderTexture");
        fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Fuentes/Underdog SDF.asset");
        if (fuente == null) fuente = TMP_Settings.defaultFontAsset;
        if (clip == null || textura == null || rt == null) throw new InvalidOperationException("Faltan vídeo, target o RenderTexture.");
        Directory.CreateDirectory("Temp/TrailerTrabajo");
        if (File.Exists("Assets/Escenas/Trailer.unity")) File.Copy("Assets/Escenas/Trailer.unity", "Temp/TrailerTrabajo/Trailer.antesDeConstruir.unity", true);
        var anterior = SceneManagerSetup();
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(escena);
        var raiz = new GameObject("TrailerRA");
        var control = raiz.AddComponent<TrailerRA>();
        control.imagenTarget = textura;
        control.anchoTargetMetros = 0.15f;

        var cam = new GameObject("ARCamera", typeof(Camera), typeof(AudioListener));
        cam.tag = "MainCamera";
        var camera = cam.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.07f, 0.06f, 0.08f);
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100;
        control.camaraRA = cam.AddComponent<VuforiaBehaviour>();
        var eventos = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        var video = new GameObject("ReproductorTrailer", typeof(VideoPlayer), typeof(AudioSource));
        control.reproductor = video.GetComponent<VideoPlayer>();
        control.audioVideo = video.GetComponent<AudioSource>();
        control.audioVideo.playOnAwake = false;
        control.audioVideo.spatialBlend = 0;
        control.reproductor.source = VideoSource.VideoClip;
        control.reproductor.clip = clip;
        control.reproductor.playOnAwake = false;
        control.reproductor.waitForFirstFrame = true;
        control.reproductor.renderMode = VideoRenderMode.RenderTexture;
        control.reproductor.targetTexture = rt;
        control.reproductor.aspectRatio = VideoAspectRatio.FitInside;
        control.reproductor.audioOutputMode = VideoAudioOutputMode.AudioSource;
        control.reproductor.controlledAudioTrackCount = 1;
        control.reproductor.SetTargetAudioSource(0, control.audioVideo);
        float aspecto = clip.height > 0 ? (float)clip.width / clip.height : 16f / 9;
        rt.Release();
        rt.width = 1920;
        rt.height = Mathf.RoundToInt(1920 / aspecto);
        rt.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;
        EditorUtility.SetDirty(rt);

        var mundo = CrearCanvas("CanvasTrailerRA", RenderMode.WorldSpace);
        control.pantallaRA = mundo;
        mundo.worldCamera = camera;
        var mr = mundo.GetComponent<RectTransform>();
        mr.sizeDelta = new Vector2(1600, 1080);
        mr.position = new Vector3(0, 0, 2);
        mr.localScale = Vector3.one * 0.001f;
        Panel("Marco", mundo.transform, Vector2.zero, new Vector2(1600, 1080), Papel);
        var raw = ObjetoUI("PantallaVideo", mundo.transform, new Vector2(0, 50), new Vector2(1440, Mathf.Min(810, 1440 / aspecto)));
        raw.AddComponent<RawImage>().texture = rt;
        raw.GetComponent<RawImage>().raycastTarget = false;
        Texto("Etiqueta", mundo.transform, "TRÁILER · PICK A BOO", new Vector2(0, 505), new Vector2(1400, 50), 32);
        control.botonReproducir = Boton("BotonReproducir", mundo.transform, "Reproducir", new Vector2(-600, -430), new Vector2(250, 90), null);
        control.botonSilenciar = Boton("BotonSilenciar", mundo.transform, "Silenciar", new Vector2(600, -430), new Vector2(270, 90), null);
        var barra = ObjetoUI("ProgresoVideo", mundo.transform, new Vector2(0, -430), new Vector2(860, 80));
        barra.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        var slider = barra.AddComponent<Slider>();
        slider.minValue = 0; slider.maxValue = 1; slider.wholeNumbers = false;
        Panel("Carril", barra.transform, Vector2.zero, new Vector2(820, 12), new Color(0.65f, 0.60f, 0.55f));
        var zonaFill = ObjetoUI("ZonaRelleno", barra.transform, Vector2.zero, new Vector2(820, 12));
        var relleno = Panel("Relleno", zonaFill.transform, Vector2.zero, Vector2.zero, Tinta);
        Estirar(relleno.GetComponent<RectTransform>());
        var zonaHandle = ObjetoUI("ZonaCursor", barra.transform, Vector2.zero, new Vector2(820, 80));
        var handle = Panel("Cursor", zonaHandle.transform, Vector2.zero, new Vector2(30, 44), Tinta);
        slider.fillRect = relleno.GetComponent<RectTransform>();
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.targetGraphic = handle.GetComponent<Image>();
        control.progreso = slider;
        control.tiempo = Texto("Tiempo", mundo.transform, "00:00 / 00:00", new Vector2(0, -500), new Vector2(700, 50), 30);

        var hud = CrearCanvas("CanvasPantalla", RenderMode.ScreenSpaceOverlay);
        hud.sortingOrder = 10;
        var cs = hud.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 0.5f;
        var volver = Boton("BotonVolver", hud.transform, "<", Vector2.zero, new Vector2(90, 90), control.VolverAlMenu);
        var vr = volver.GetComponent<RectTransform>(); vr.anchorMin = vr.anchorMax = new Vector2(0, 1); vr.anchoredPosition = new Vector2(90, -90);
        var aviso = ObjetoUI("AvisoEscanear", hud.transform, Vector2.zero, new Vector2(900, 720));
        control.avisoEscanear = aviso;
        Panel("FondoAviso", aviso.transform, Vector2.zero, new Vector2(900, 720), new Color(Papel.r, Papel.g, Papel.b, 0.96f));
        Texto("Titulo", aviso.transform, "TRÁILER", new Vector2(0, 280), new Vector2(800, 90), 64);
        Texto("Instruccion", aviso.transform, "Apunta la cámara a esta imagen", new Vector2(0, 185), new Vector2(820, 80), 38);
        var thumb = ObjetoUI("ImagenTarget", aviso.transform, new Vector2(0, -15), new Vector2(600, 600f * textura.height / textura.width));
        thumb.AddComponent<RawImage>().texture = textura; thumb.GetComponent<RawImage>().raycastTarget = false;
        Texto("Ayuda", aviso.transform, "Mantén visible el target durante la reproducción", new Vector2(0, -280), new Vector2(830, 90), 30);
        var estadoPanel = Panel("FondoEstado", hud.transform, Vector2.zero, new Vector2(1500, 90), new Color(0.07f, 0.06f, 0.08f, 0.85f));
        var er = estadoPanel.GetComponent<RectTransform>(); er.anchorMin = er.anchorMax = new Vector2(0.5f, 0); er.anchoredPosition = new Vector2(0, 70);
        control.estado = Texto("Estado", estadoPanel.transform, "Escanea el target del tráiler para comenzar", Vector2.zero, new Vector2(1450, 80), 30);
        control.estado.color = Papel;
        // La vista de edición muestra el panel; en Play se oculta hasta detectar el target.

        foreach (var setup in anterior)
        {
            if (!setup.isLoaded) continue;
            var abierta = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(setup.path);
            if (abierta.IsValid() && abierta != escena) EditorSceneManager.CloseScene(abierta, true);
        }
        AplicarEstilo(control);
        if (!EditorSceneManager.SaveScene(escena, "Assets/Escenas/Trailer.unity")) throw new IOException("No se pudo guardar Trailer.");
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = raiz;
        Debug.Log("[T013] Escena Trailer construida y guardada con trailer_pick_a_boo.mp4.");
    }

    [MenuItem("Proyecto/Trailer/Aplicar estilo compartido")]
    public static void AplicarEstiloCompartido()
    {
        var control = UnityEngine.Object.FindFirstObjectByType<TrailerRA>();
        if (control == null || control.gameObject.scene.name != "Trailer") throw new InvalidOperationException("Abre Trailer para aplicar su estilo.");
        AplicarEstilo(control);
        EditorSceneManager.MarkSceneDirty(control.gameObject.scene);
        EditorSceneManager.SaveScene(control.gameObject.scene);
        AssetDatabase.SaveAssets();
    }

    static void AplicarEstilo(TrailerRA control)
    {
        var papel = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Fondos/panel_papel.png");
        var redondeado = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Fondos/boton_redondeado.png");
        var atras = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Iconos/icono_atras.png");
        if (papel == null || redondeado == null || atras == null) throw new InvalidOperationException("Faltan recursos compartidos de interfaz.");
        var mundo = control.pantallaRA.transform;
        var hud = control.avisoEscanear.transform.parent;
        foreach (var texto in mundo.GetComponentsInChildren<TMP_Text>(true)) texto.color = TextoOscuro;
        foreach (var texto in hud.GetComponentsInChildren<TMP_Text>(true)) texto.color = TextoOscuro;
        foreach (var fondo in new[] { mundo.Find("Marco"), control.avisoEscanear.transform.Find("FondoAviso") })
        {
            var imagen = fondo.GetComponent<Image>();
            imagen.sprite = papel; imagen.type = Image.Type.Simple; imagen.color = Color.white;
        }
        var fondoEstado = control.estado.transform.parent.GetComponent<Image>();
        fondoEstado.sprite = null; fondoEstado.color = Color.clear;
        foreach (var boton in new[] { control.botonReproducir, control.botonSilenciar })
        {
            var imagen = boton.GetComponent<Image>();
            imagen.sprite = redondeado; imagen.type = Image.Type.Sliced; imagen.color = Tinta;
            boton.colors = ColoresCompartidos();
            boton.GetComponentInChildren<TMP_Text>(true).color = Color.white;
        }
        control.botonReproducir.GetComponent<RectTransform>().anchoredPosition = new Vector2(-570, -430);
        control.botonSilenciar.GetComponent<RectTransform>().anchoredPosition = new Vector2(570, -430);
        foreach (var boton in new[] { control.botonReproducir, control.botonSilenciar })
        {
            boton.GetComponent<RectTransform>().sizeDelta = new Vector2(340, 110);
            var texto = boton.GetComponentInChildren<TMP_Text>(true);
            texto.rectTransform.sizeDelta = new Vector2(320, 94);
            texto.fontSize = 44; texto.enableAutoSizing = true; texto.fontSizeMin = 28; texto.fontSizeMax = 44;
        }
        control.progreso.GetComponent<RectTransform>().sizeDelta = new Vector2(740, 90);
        foreach (string nombre in new[] { "Carril", "ZonaRelleno", "ZonaCursor" })
        {
            var r = control.progreso.transform.Find(nombre).GetComponent<RectTransform>();
            r.sizeDelta = new Vector2(700, r.sizeDelta.y);
        }
        control.tiempo.fontSize = 40;
        control.tiempo.rectTransform.anchoredPosition = new Vector2(0, -510);
        // Mismo sprite, tamaño, margen y anclaje que Instrucciones y Modelos3D.
        var volver = hud.Find("BotonVolver").GetComponent<Button>();
        var imagenVolver = volver.GetComponent<Image>();
        imagenVolver.sprite = atras; imagenVolver.type = Image.Type.Simple;
        imagenVolver.preserveAspect = true; imagenVolver.color = Color.white;
        volver.colors = ColorBlock.defaultColorBlock;
        foreach (var texto in volver.GetComponentsInChildren<TMP_Text>(true)) texto.gameObject.SetActive(false);
        var rect = volver.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(30, -30); rect.sizeDelta = new Vector2(150, 150);
        var titulo = control.avisoEscanear.transform.Find("Titulo").GetComponent<TMP_Text>();
        titulo.text = "Tráiler"; titulo.fontSize = 80; titulo.color = Tinta;
        var etiqueta = mundo.Find("Etiqueta").GetComponent<TMP_Text>();
        etiqueta.text = "Tráiler"; etiqueta.fontSize = 64; etiqueta.color = Tinta;
        control.avisoEscanear.transform.Find("Instruccion").GetComponent<TMP_Text>().text = "Escanee el target correspondiente";
        control.camaraRA.GetComponent<Camera>().backgroundColor = Color.white;
        var barra = control.progreso.transform;
        barra.Find("Carril").GetComponent<Image>().color = new Color(Tinta.r, Tinta.g, Tinta.b, 0.35f);
        control.progreso.fillRect.GetComponent<Image>().color = Tinta;
        var cursor = control.progreso.handleRect.GetComponent<Image>();
        cursor.sprite = redondeado; cursor.type = Image.Type.Sliced; cursor.color = Tinta;
        control.progreso.colors = ColoresCompartidos();
        EditorUtility.SetDirty(control);
    }

    static ColorBlock ColoresCompartidos()
    {
        var colores = ColorBlock.defaultColorBlock;
        colores.highlightedColor = new Color(0.92f, 0.92f, 0.92f);
        colores.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        return colores;
    }
    static SceneSetup[] SceneManagerSetup() => EditorSceneManager.GetSceneManagerSetup();
    static Canvas CrearCanvas(string nombre, RenderMode modo)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var c = go.GetComponent<Canvas>(); c.renderMode = modo; return c;
    }
    static GameObject ObjetoUI(string nombre, Transform padre, Vector2 posicion, Vector2 tamano)
    {
        var go = new GameObject(nombre, typeof(RectTransform)); go.layer = 5;
        var rect = go.GetComponent<RectTransform>(); rect.SetParent(padre, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion; rect.sizeDelta = tamano; return go;
    }
    static GameObject Panel(string nombre, Transform padre, Vector2 pos, Vector2 size, Color color)
    {
        var go = ObjetoUI(nombre, padre, pos, size); var im = go.AddComponent<Image>(); im.color = color; im.raycastTarget = false; return go;
    }
    static TMP_Text Texto(string nombre, Transform padre, string texto, Vector2 pos, Vector2 size, float puntos)
    {
        var go = ObjetoUI(nombre, padre, pos, size); var t = go.AddComponent<TextMeshProUGUI>(); t.font = fuente;
        t.text = texto; t.fontSize = puntos; t.color = Tinta; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false; return t;
    }
    static Button Boton(string nombre, Transform padre, string texto, Vector2 pos, Vector2 size, UnityAction accion)
    {
        var go = ObjetoUI(nombre, padre, pos, size); var im = go.AddComponent<Image>(); im.color = Tinta;
        var b = go.AddComponent<Button>(); b.targetGraphic = im;
        var colors = b.colors; colors.highlightedColor = new Color(1.3f, 1.2f, 1.2f); colors.pressedColor = new Color(0.7f, 0.7f, 0.7f); b.colors = colors;
        var textoBoton = Texto("Texto", go.transform, texto, Vector2.zero, size - new Vector2(12, 8), 30); textoBoton.color = Papel;
        if (accion != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick, accion);
        return b;
    }
    static void Estirar(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
}







