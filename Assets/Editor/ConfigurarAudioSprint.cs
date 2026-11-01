using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ConfigurarAudioSprint
{
    static ConfigurarAudioSprint() => EditorApplication.update += RevisarSolicitud;

    static void RevisarSolicitud()
    {
        const string solicitud = "Temp/AudioTrabajo/preparar";
        if (!File.Exists(solicitud) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(solicitud);
        try { Preparar(); File.WriteAllText("Temp/AudioTrabajo/preparado.txt", "OK"); }
        catch (Exception ex) { File.WriteAllText("Temp/AudioTrabajo/preparado.txt", ex.ToString()); Debug.LogException(ex); }
    }

    [MenuItem("Proyecto/Audio/Preparar catálogo del sprint")]
    public static void Preparar()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (var archivo in Directory.GetFiles("Assets/Audio", "*.wav"))
            AssetDatabase.ImportAsset(archivo.Replace('\\', '/'), ImportAssetOptions.ForceUpdate);
        const string ruta = "Assets/Resources/SonidosSprint.asset";
        var sonidos = AssetDatabase.LoadAssetAtPath<SonidosSprint>(ruta);
        if (sonidos == null)
        {
            sonidos = ScriptableObject.CreateInstance<SonidosSprint>();
            AssetDatabase.CreateAsset(sonidos, ruta);
        }
        sonidos.boton = Clip("UI_Boton");
        sonidos.targetDetectado = Clip("UI_TargetDetectado");
        sonidos.reiniciar = Clip("UI_Reiniciar");
        sonidos.puertaAbrir = Clip("Puerta_Abrir");
        sonidos.puertaCerrar = Clip("Puerta_Cerrar");
        sonidos.ambienteMenu = Clip("Ambiente_Menu");
        sonidos.ambientePreview = Clip("Ambiente_Preview");
        sonidos.musicaMenu = Clip("Musica_Menu");
        sonidos.trueno = Clip("Ambiente_Trueno");
        sonidos.motor = Clip("Motor_Furgoneta");
        sonidos.paginaLibro = Clip("Libro_Pagina");
        sonidos.fantasma = Clip("Fantasma_Aparece");
        sonidos.bloqueado = Clip("UI_Bloqueado");
        sonidos.slider = Clip("UI_Slider");
        EditorUtility.SetDirty(sonidos);
        AssetDatabase.SaveAssets();
        Debug.Log("[T016] Catálogo de audio preparado.");
    }

    static AudioClip Clip(string nombre)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + nombre + ".wav");
        if (clip == null) throw new InvalidOperationException("No se ha importado " + nombre);
        return clip;
    }
}
