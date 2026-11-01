using UnityEngine;
using Vuforia;

/// <summary>
/// Muestra el aviso "Escanee el target correspondiente" mientras la cámara no ve el target
/// y oculta los controles (joystick, reset) hasta que lo vuelve a ver.
/// </summary>
public class AvisoEscanear : MonoBehaviour
{
    public ObserverBehaviour target;
    public GameObject aviso;
    [Tooltip("Objetos que solo se muestran con el target detectado.")]
    public GameObject[] controles;
    [Tooltip("Si está activo, el seguimiento extendido (la cámara ya no ve la imagen pero Vuforia sigue la posición) cuenta como detectado.")]
    public bool contarSeguimientoExtendido = true;

    public bool Detectado { get; private set; }
    public event System.Action<bool> DeteccionCambiada;

    void Start()
    {
        Aplicar(false);
        if (target != null)
            target.OnTargetStatusChanged += AlCambiarEstado;
    }

    void OnDestroy()
    {
        if (target != null)
            target.OnTargetStatusChanged -= AlCambiarEstado;
    }

    void AlCambiarEstado(ObserverBehaviour _, TargetStatus estado)
    {
        bool detectado = estado.Status == Status.TRACKED ||
                         (contarSeguimientoExtendido && estado.Status == Status.EXTENDED_TRACKED);
        Aplicar(detectado);
    }

    void Aplicar(bool detectado)
    {
        bool cambio = Detectado != detectado;
        Detectado = detectado;
        if (aviso != null)
            aviso.SetActive(!detectado);
        foreach (var control in controles)
            if (control != null)
                control.SetActive(detectado);
        if (cambio) DeteccionCambiada?.Invoke(detectado);
    }
}
