using UnityEngine;

/// <summary>Clips y mezcla de T016. Se carga desde Resources/SonidosSprint.</summary>
[CreateAssetMenu(menuName = "Pick a Boo/Sonidos del sprint")]
public sealed class SonidosSprint : ScriptableObject
{
    public AudioClip boton, targetDetectado, reiniciar, puertaAbrir, puertaCerrar;
    public AudioClip ambienteMenu, ambientePreview, musicaMenu, trueno, motor;
    public AudioClip paginaLibro, fantasma, bloqueado, slider;
    [Range(0, 1)] public float volumenBotones = .5f;
    [Range(0, 1)] public float volumenConfirmacion = .4f;
    [Range(0, 1)] public float volumenPuerta = .5f;
    [Min(0), Tooltip("Segundos de espera antes del sonido de cierre; la animación empieza inmediatamente.")]
    public float retrasoCierrePuerta = .2f;
    [Range(0, 1)] public float volumenAmbienteMenu = .16f;
    [Range(0, 1)] public float volumenAmbientePreview = .2f;
    [Range(0, 1)] public float volumenMusica = .22f;
    [Range(0, 1)] public float volumenTrueno = .25f;
    [Range(0, 1)] public float volumenMotor = .35f;
    [Range(0, 1)] public float volumenPagina = .5f;
    [Range(0, 1)] public float volumenFantasma = .5f;
    [Range(0, 1)] public float volumenBloqueado = .5f;
    [Range(0, 1)] public float volumenSlider = .4f;
}
