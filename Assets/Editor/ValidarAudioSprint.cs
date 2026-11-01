using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Prueba de integración en las escenas reales; restaura ajustes y escena al salir de Play.</summary>
[InitializeOnLoad]
public static class ValidarAudioSprint
{
    const string Clave = "T016.Validando";
    static int paso;
    static double inicio, espera;
    static string informe = "";
    static AudioSprint audio;
    static AvisoEscanear seguimiento;

    static ValidarAudioSprint() => EditorApplication.update += Actualizar;

    [MenuItem("Proyecto/Audio/Validar integración del sprint")]
    public static void Iniciar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        ConfigurarAudioSprint.Preparar();
        SessionState.SetBool("T016.TeniaVolumen", PlayerPrefs.HasKey("volumen"));
        SessionState.SetFloat("T016.Volumen", AjustesAudio.Volumen);
        SessionState.SetBool(Clave, true);
        EditorApplication.isPlaying = true;
    }

    static void Comprobar(bool valor, string prueba)
    {
        if (!valor) throw new InvalidOperationException(prueba);
        informe += "OK: " + prueba + "\n";
    }

    static AudioSource Fuente(string nombre) => audio.transform.Find(nombre).GetComponent<AudioSource>();

    static void Deteccion(bool valor) => typeof(AvisoEscanear).GetMethod("Aplicar", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(seguimiento, new object[] { valor });

    static void Actualizar()
    {
        if (!SessionState.GetBool(Clave, false))
        {
            const string solicitud = "Temp/AudioTrabajo/validar";
            if (File.Exists(solicitud) && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.Delete(solicitud);
                Iniciar();
            }
            return;
        }
        if (!EditorApplication.isPlaying) return;
        try
        {
            double ahora = EditorApplication.timeSinceStartup;
            if (inicio == 0) inicio = ahora;
            if (ahora - inicio > 65) throw new TimeoutException("Tiempo de validación agotado.");
            if (ahora < espera) return;
            switch (paso)
            {
                case 0:
                    SceneManager.LoadScene("MenuPrincipal");
                    espera = ahora + 2; paso++; break;
                case 1:
                    audio = AudioSprint.Instancia;
                    Comprobar(audio != null, "Gestor de audio inicializado automáticamente");
                    Comprobar(UnityEngine.Object.FindObjectsByType<AudioSprint>(FindObjectsSortMode.None).Length == 1, "Un único gestor persistente");
                    var clips = new[] { audio.sonidos.boton, audio.sonidos.targetDetectado, audio.sonidos.reiniciar, audio.sonidos.puertaAbrir, audio.sonidos.puertaCerrar, audio.sonidos.ambienteMenu, audio.sonidos.ambientePreview, audio.sonidos.musicaMenu, audio.sonidos.trueno, audio.sonidos.motor };
                    foreach (var clip in clips) Comprobar(clip != null && clip.frequency == 48000, "Clip importado a 48 kHz: " + (clip == null ? "ausente" : clip.name));
                    Comprobar(Fuente("Ambiente").isPlaying && Fuente("Musica").isPlaying, "Ambiente y música del menú reproduciéndose");
                    foreach (var fuente in audio.GetComponentsInChildren<AudioSource>()) Comprobar(!fuente.ignoreListenerVolume, "Volumen general aplicado a " + fuente.name);
                    var panel = UnityEngine.Object.FindFirstObjectByType<PanelAjustes>(FindObjectsInactive.Include);
                    panel.Abrir(); panel.sliderVolumen.value = 0;
                    Comprobar(AudioListener.volume == 0 && AjustesAudio.Volumen == 0, "Deslizador silencia el audio general");
                    panel.sliderVolumen.value = 43;
                    Comprobar(Mathf.Approximately(AudioListener.volume, .43f), "Deslizador restaura el volumen al 43 %");
                    panel.Cerrar();
                    var menu = UnityEngine.Object.FindFirstObjectByType<MenuPrincipal>();
                    menu.AbrirPreview();
                    Comprobar(Fuente("Puerta").clip == audio.sonidos.puertaAbrir && Fuente("Puerta").isPlaying, "Puerta vinculada a transición real");
                    espera = ahora + 2; paso++; break;
                case 2:
                    var aviso = UnityEngine.Object.FindFirstObjectByType<AvisoTarget>();
                    aviso.Cerrar();
                    espera = ahora + audio.sonidos.retrasoCierrePuerta + .1;
                    paso = 20; break;
                case 20:
                    Comprobar(Fuente("Puerta").clip == audio.sonidos.puertaCerrar && Fuente("Puerta").isPlaying, "Cierre de puerta al regresar");
                    var avisoPreview = UnityEngine.Object.FindFirstObjectByType<AvisoTarget>();
                    avisoPreview.Mostrar(UnityEngine.Object.FindFirstObjectByType<MenuPrincipal>().preview);
                    var aceptar = Array.Find(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None), b => b.onClick.GetPersistentEventCount() > 0 && b.onClick.GetPersistentMethodName(0) == "Aceptar");
                    Comprobar(aceptar != null, "Botón real de aceptar localizado");
                    aceptar.onClick.Invoke();
                    // LoadScene completa la carga al final del fotograma; el clic ya debe haber empezado.
                    Comprobar(Fuente("Interfaz").isPlaying, "Clic de aceptar reproducido antes del cambio de escena");
                    espera = ahora + 2; paso = 3; break;
                case 3:
                    Comprobar(SceneManager.GetActiveScene().name == "Preview", "Navegación real hacia preview");
                    Comprobar(AudioSprint.Instancia == audio, "Gestor de clics conservado al cambiar de escena");
                    Comprobar(!Fuente("Musica").isPlaying && !Fuente("Puerta").isPlaying, "Audio del menú detenido en preview");
                    Comprobar(Mathf.Approximately(AjustesAudio.Volumen, .43f), "Volumen conservado entre escenas");
                    seguimiento = UnityEngine.Object.FindFirstObjectByType<AvisoEscanear>();
                    Comprobar(seguimiento != null, "Seguimiento RA de preview conectado");
                    Deteccion(true);
                    espera = ahora + 1; paso++; break;
                case 4:
                    Comprobar(Fuente("Motor").isPlaying && Fuente("Ambiente").isPlaying, "Target detectado activa ambiente y motor");
                    var coche = UnityEngine.Object.FindFirstObjectByType<ControlFurgoneta>();
                    typeof(ControlFurgoneta).GetField("m_Velocidad", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(coche, coche.velocidadMaxima);
                    audio.SendMessage("Update");
                    Comprobar(Fuente("Motor").pitch > 1 && Fuente("Motor").clip == audio.sonidos.motor, "Mismo clip de ralentí cambia de tono al conducir");
                    coche.Reiniciar();
                    Comprobar(Fuente("Interfaz").isPlaying && Fuente("Motor").pitch == 1, "Reiniciar reproduce efecto y restablece el motor");
                    Deteccion(false);
                    Comprobar(!Fuente("Motor").isPlaying && !Fuente("Ambiente").isPlaying, "Perder target detiene motor y ambiente");
                    Deteccion(true);
                    espera = ahora + 1; paso++; break;
                case 5:
                    Comprobar(Fuente("Motor").isPlaying && Fuente("Ambiente").isPlaying, "Recuperar target restablece sonido");
                    audio.SendMessage("OnApplicationPause", true);
                    Comprobar(!Fuente("Motor").isPlaying && !Fuente("Ambiente").isPlaying, "Pausa de aplicación pausa el audio");
                    audio.SendMessage("OnApplicationPause", false);
                    SceneManager.LoadScene("Trailer");
                    espera = ahora + 2; paso++; break;
                case 6:
                    Comprobar(!Fuente("Motor").isPlaying && !Fuente("Ambiente").isPlaying && !Fuente("Musica").isPlaying, "Tráiler sin sonidos de menú ni preview");
                    var trailer = UnityEngine.Object.FindFirstObjectByType<TrailerRA>();
                    Comprobar(trailer != null && !trailer.audioVideo.ignoreListenerVolume, "Audio del tráiler respeta volumen general");
                    AjustesAudio.Volumen = 0;
                    Comprobar(AudioListener.volume == 0, "Silencio general también en tráiler");
                    trailer.Silenciar();
                    Comprobar(trailer.audioVideo.mute, "Silenciar local del tráiler");
                    trailer.Silenciar();
                    Comprobar(!trailer.audioVideo.mute && AudioListener.volume == 0, "Reactivar tráiler conserva silencio general");
                    AjustesAudio.Volumen = .43f;
                    SceneManager.LoadScene("MenuPrincipal");
                    espera = ahora + 2; paso++; break;
                case 7:
                    Comprobar(UnityEngine.Object.FindObjectsByType<AudioSprint>(FindObjectsSortMode.None).Length == 1, "Regreso al menú sin duplicar gestores");
                    Comprobar(Fuente("Ambiente").isPlaying && Fuente("Musica").isPlaying && !Fuente("Motor").isPlaying, "Mezcla del menú restablecida");
                    Terminar(informe); break;
            }
        }
        catch (Exception ex) { Terminar(informe + "FALLO: " + ex + "\n"); }
    }

    static void Terminar(string resultado)
    {
        File.WriteAllText("Temp/AudioTrabajo/validacion.txt", resultado);
        if (SessionState.GetBool("T016.TeniaVolumen", false)) AjustesAudio.Volumen = SessionState.GetFloat("T016.Volumen", 1);
        else { PlayerPrefs.DeleteKey("volumen"); AudioListener.volume = 1; }
        PlayerPrefs.Save();
        SessionState.SetBool(Clave, false);
        EditorApplication.isPlaying = false;
        paso = 0; inicio = espera = 0; informe = "";
    }
}
