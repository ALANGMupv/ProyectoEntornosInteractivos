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

    // TODO (idea del hi-fi): el fondo será la carretera y el bosque, y "estaría guapísimo que fuera animado"
    // (p. ej. un desplazamiento lento o parallax). De momento es blanco porque aún no tenemos la imagen.
    public Image fondo;

    void Start()
    {
        botonJuego.interactable = juegoDesbloqueado;
    }

    public void AbrirPreview() => aviso.Mostrar(preview);
    public void AbrirTrailer() => aviso.Mostrar(trailer);
    public void AbrirInstrucciones() => aviso.Mostrar(instrucciones);
    public void AbrirModelos3D() => aviso.Mostrar(modelos3D);

    public void AbrirJuego()
    {
        if (juegoDesbloqueado)
            aviso.Mostrar(juego);
    }
}
