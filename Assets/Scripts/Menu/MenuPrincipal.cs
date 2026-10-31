using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menú principal 2D: Preview, Trailer, Instrucciones y Modelos 3D muestran el aviso del target
/// que hay que escanear; Juego queda visible pero bloqueado hasta el sprint 3.b.
/// </summary>
public class MenuPrincipal : MonoBehaviour
{
    public const string k_EscenaMenu = "MenuPrincipal";

    public AvisoTarget aviso;
    [Tooltip("Animación de entrar en la casa al elegir una sección (opcional).")]
    public TransicionPuerta transicion;

    [Header("Accesos por target")]
    public AccesoTarget preview;
    public AccesoTarget trailer;
    public AccesoTarget instrucciones;
    public AccesoTarget modelos3D;

    [Header("Juego (sprint 3.b)")]
    public Button botonJuego;
    [Tooltip("Desbloquear cuando exista el juego completo.")]
    public bool juegoDesbloqueado;
    public AccesoTarget juego;

    // Color liso detrás de todo. La animación (fachada de noche, niebla, viento y rayos) está en FondoAnimado.
    public Image fondo;

    void Start()
    {
        botonJuego.interactable = juegoDesbloqueado;
    }

    public void AbrirPreview() => Abrir(preview);
    public void AbrirTrailer() => Abrir(trailer);
    public void AbrirInstrucciones() => Abrir(instrucciones);
    public void AbrirModelos3D() => Abrir(modelos3D);

    public void AbrirJuego()
    {
        if (juegoDesbloqueado)
            Abrir(juego);
    }

    /// <summary>Con transición, se entra en la casa antes de mostrar el aviso; sin ella, el aviso sale directamente.</summary>
    void Abrir(AccesoTarget acceso)
    {
        if (transicion != null)
            transicion.Entrar(acceso);
        else
            aviso.Mostrar(acceso);
    }
}
