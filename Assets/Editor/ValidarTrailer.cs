using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Prueba real de VideoPlayer en Play Mode, sin necesitar cámara o imagen impresa.</summary>
[InitializeOnLoad]
public static class ValidarTrailer
{
    const string Clave = "T013.Validando";
    static int paso;
    static double inicio, espera;
    static TrailerRA control;
    static string informe = "";
    static ValidarTrailer() { EditorApplication.update += Actualizar; }
    [MenuItem("Proyecto/Trailer/Validar controles sin cámara")]
    public static void Iniciar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        control = UnityEngine.Object.FindFirstObjectByType<TrailerRA>();
        if (control == null) throw new InvalidOperationException("Abre la escena Trailer para validar.");
        control.probarSinTarget = true;
        SessionState.SetBool(Clave, true);
        EditorApplication.isPlaying = true;
    }
    [MenuItem("Proyecto/Trailer/Validar arranque RA")]
    public static void IniciarRA()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        control = UnityEngine.Object.FindFirstObjectByType<TrailerRA>();
        if (control == null) throw new InvalidOperationException("Abre la escena Trailer para validar.");
        control.probarSinTarget = false;
        SessionState.SetBool("T013.Real", true);
        SessionState.SetBool(Clave, true);
        EditorApplication.isPlaying = true;
    }
    static void Comprobar(bool cumple, string prueba)
    {
        if (!cumple) throw new Exception("FALLO: " + prueba);
        informe += "OK: " + prueba + "\n";
    }
    static void Visibilidad(bool visible)
    {
        typeof(TrailerRA).GetMethod("AplicarVisibilidad", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(control, new object[] { visible });
    }
    static void Actualizar()
    {
        if (!SessionState.GetBool(Clave, false))
        {
            if (!EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode && File.Exists("Temp/TrailerTrabajo/validar"))
            { try { File.Delete("Temp/TrailerTrabajo/validar"); } catch (IOException) { return; } Iniciar(); }
            if (!EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode && File.Exists("Temp/TrailerTrabajo/validarRA"))
            { try { File.Delete("Temp/TrailerTrabajo/validarRA"); } catch (IOException) { return; } IniciarRA(); }
            return;
        }
        if (!EditorApplication.isPlaying) return;
        try
        {
            double ahora = EditorApplication.timeSinceStartup;
            if (inicio == 0) inicio = ahora;
            if (ahora - inicio > 45) throw new TimeoutException("El vídeo no se preparó o no terminó la prueba.");
            if (ahora < espera) return;
            if (control == null) control = UnityEngine.Object.FindFirstObjectByType<TrailerRA>();
            if (SessionState.GetBool("T013.Real", false))
            {
                if (control == null || !control.ListoParaReproducir || !Vuforia.VuforiaApplication.Instance.IsRunning) return;
                var target = typeof(TrailerRA).GetField("target", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(control) as Vuforia.ImageTargetBehaviour;
                if (target == null) return;
                Comprobar(target.TargetName == "Target_Trailer", "Vuforia inicializa y crea el target del tráiler");
                Comprobar(!control.probarSinTarget, "Modo RA real por defecto");
                Comprobar(control.reproductor.isPrepared && !control.reproductor.isPlaying, "Vídeo preparado esperando reproducción");
                Comprobar(control.avisoEscanear.activeSelf && !control.pantallaRA.gameObject.activeSelf, "Sin target visible se muestra el aviso y se oculta el vídeo");
                Terminar(informe);
                return;
            }
            switch (paso)
            {
                case 0:
                    if (control == null || !control.ListoParaReproducir) return;
                    Comprobar(control.reproductor.clip.name == "trailer_pick_a_boo", "Clip original asignado");
                    Comprobar(!control.reproductor.isPlaying, "Sin reproducción automática");
                    Comprobar(control.reproductor.frame >= 0, "Primer fotograma visible antes de pulsar Reproducir");
                    ScreenCapture.CaptureScreenshot("Temp/TrailerTrabajo/preview-inicial.png");
                    control.botonReproducir.onClick.Invoke(); espera = ahora + 2; paso++; break;
                case 1:
                    Comprobar(control.reproductor.isPlaying && control.reproductor.time > 0, "Reproducción y avance del tiempo");
                    control.botonReproducir.onClick.Invoke(); espera = ahora + 0.5; paso++; break;
                case 2:
                    Comprobar(!control.reproductor.isPlaying, "Pausa desde el botón");
                    control.botonSilenciar.onClick.Invoke(); Comprobar(control.audioVideo.mute, "Silenciar");
                    control.botonSilenciar.onClick.Invoke(); Comprobar(!control.audioVideo.mute, "Restaurar sonido");
                    control.progreso.value = 0.5f; espera = ahora + 1.5; paso++; break;
                case 3:
                    Comprobar(Math.Abs(control.reproductor.time - control.reproductor.length * 0.5) < 2, "Buscar con slider estando pausado");
                    Comprobar(!control.estado.transform.parent.gameObject.activeSelf, "Aviso no tapa controles durante reproducción");
                    ScreenCapture.CaptureScreenshot("Temp/TrailerTrabajo/preview.png");
                    control.botonReproducir.onClick.Invoke(); espera = ahora + 0.7; paso++; break;
                case 4:
                    Visibilidad(false);
                    Comprobar(!control.reproductor.isPlaying && !control.pantallaRA.gameObject.activeSelf, "Perder target pausa y oculta vídeo");
                    Visibilidad(true); espera = ahora + 0.7; paso++; break;
                case 5:
                    Comprobar(control.reproductor.isPlaying && control.pantallaRA.gameObject.activeSelf, "Recuperar target reanuda reproducción");
                    control.progreso.value = 0.999f; espera = ahora + 1.5; paso++; break;
                case 6:
                    Comprobar(!control.reproductor.isPlaying && control.botonReproducir.GetComponentInChildren<TMPro.TMP_Text>().text == "Repetir", "Fin de vídeo y botón Repetir");
                    control.botonReproducir.onClick.Invoke(); espera = ahora + 1; paso++; break;
                case 7:
                    Comprobar(control.reproductor.isPlaying && control.reproductor.time < 5, "Repetir desde el principio");
                    var cam = control.camaraRA.GetComponent<Camera>();
                    var rect = control.pantallaRA.GetComponent<RectTransform>();
                    control.pantallaRA.renderMode = RenderMode.WorldSpace;
                    rect.SetParent(null, false);
                    rect.sizeDelta = new Vector2(1600, 1080);
                    var aplicar = typeof(TrailerRA).GetMethod("AplicarEscalaDelPanel", BindingFlags.Instance | BindingFlags.NonPublic);
                    aplicar.Invoke(control, null);
                    Vector3 escala = rect.localScale;
                    Comprobar(Mathf.Abs(rect.rect.width * escala.x - control.anchoTargetMetros * control.anchoPanelRespectoTarget) < 0.00001f, "Ancho físico del panel coincide con el configurado");
                    cam.transform.position += new Vector3(0.2f, -0.1f, 0.3f);
                    cam.aspect = 9f / 16;
                    aplicar.Invoke(control, null);
                    Comprobar(rect.localScale == escala, "Mover cámara y cambiar aspecto no cambia la escala del panel");
                    cam.ResetAspect();
                    control.VolverAlMenu(); espera = ahora + 1; paso++; break;
                case 8:
                    Comprobar(SceneManager.GetActiveScene().name == "MenuPrincipal", "Volver al menú principal");
                    Terminar(informe); break;
            }
        }
        catch (Exception e) { Terminar(informe + e); Debug.LogException(e); }
    }
    static void Terminar(string resultado)
    {
        File.WriteAllText(SessionState.GetBool("T013.Real", false) ? "Temp/TrailerTrabajo/validacionRA.txt" : "Temp/TrailerTrabajo/validacion.txt", resultado);
        SessionState.SetBool("T013.Real", false);
        SessionState.SetBool(Clave, false);
        EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () =>
        {
            var c = UnityEngine.Object.FindFirstObjectByType<TrailerRA>();
            if (c != null) c.probarSinTarget = false;
        };
        paso = 0; inicio = espera = 0; informe = "";
    }
}





